using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GhostController : MonoBehaviour{
    private enum GhostMood { Chase, Frightened, Eaten, Roam }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform ghostHome;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private Renderer[] bodyRenderers;

    [Header("Tuning")]
    [SerializeField] private float raycastDistance = 0.8f;
    [SerializeField] private float pathUpdateInterval = 0.4f;
    [SerializeField] private float waypointReachedDistance = 0.25f;
    [SerializeField] private float playerDetectionRange = 8f;
    [SerializeField] private float roamWaypointRadius = 5f;

    [Header("Colors")]
    [SerializeField] private Color chaseColor = Color.red;
    [SerializeField] private Color scaredColor = new Color(0.2f, 0.3f, 1f);
    [SerializeField] private Color eatenColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    [SerializeField] private Color roamColor = new Color(1f, 0.6f, 0.1f);

    private Rigidbody _rb;
    private GhostMood _mood = GhostMood.Roam;
    private Coroutine _respawnRoutine;

    private List<Vector3> _currentPath = new List<Vector3>();
    private int _pathIndex;
    private Vector3 _roamTarget;
    private bool _canHit = true;

    private static readonly Vector3[] Cardinals = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

    private class Node{
        public readonly Vector3 Pos;
        public readonly Node Parent;
        public readonly float G;
        private readonly float _h;
        public float F => G + _h;

        public Node(Vector3 pos, Node parent, float g, float h){
            Pos = pos;
            Parent = parent;
            G = g;
            _h = h;
        }
    }

    void Awake(){
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.constraints = (RigidbodyConstraints)80;
        
        if (player == null){
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }
        PickNewRoamTarget();
    }

    void OnEnable(){
        GameEvents.OnEnterNormalMode += HandleNormalMode;
        GameEvents.OnEnterRaveMode += HandleRaveMode;
        GameEvents.OnEnterSwitchMode += HandleSwitchMode;
    }

    void OnDisable(){
        GameEvents.OnEnterNormalMode -= HandleNormalMode;
        GameEvents.OnEnterRaveMode -= HandleRaveMode;
        GameEvents.OnEnterSwitchMode -= HandleSwitchMode;
    }

    void Start(){
        StartCoroutine(PathUpdateLoop());
    }

    private void HandleNormalMode(){ if (_mood != GhostMood.Eaten) SetMood(GhostMood.Roam); }
    private void HandleRaveMode(){ if (_mood != GhostMood.Eaten) SetMood(GhostMood.Frightened); }
    private void HandleSwitchMode(){ if (_mood != GhostMood.Eaten) SetMood(GhostMood.Frightened); }

    void FixedUpdate(){
        if (GameManager.Instance == null) return;
        float dist = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

        if (_mood == GhostMood.Roam && dist <= playerDetectionRange)
            SetMood(GhostMood.Chase);
        else if (_mood == GhostMood.Chase && dist > playerDetectionRange)
            SetMood(GhostMood.Roam);

        float speed = _mood == GhostMood.Frightened ? GameManager.Instance.Config.ghostFrightenedSpeed : GameManager.Instance.Config.ghostNormalSpeed;
        MoveAlongPath(speed);
    }

    private void MoveAlongPath(float speed){
        if (_currentPath == null || _pathIndex >= _currentPath.Count){
            _rb.linearVelocity = Vector3.zero;
            if (_mood == GhostMood.Roam) PickNewRoamTarget();
            return;
        }

        Vector3 target = _currentPath[_pathIndex];
        target.y = transform.position.y;
        Vector3 dir = target - transform.position;

        if (dir.magnitude <= waypointReachedDistance){
            _pathIndex++;
            return;
        }
        _rb.linearVelocity = dir.normalized * speed;
    }
    
    private IEnumerator PathUpdateLoop(){
        while (true){
            yield return new WaitForSeconds(pathUpdateInterval);
            if (GameManager.Instance == null) continue;

            List<Vector3> newPath = FindPath(transform.position, GetGoalPosition());
            if (newPath is { Count: > 0 }){
                _currentPath = newPath;
                _pathIndex = 0;
            }
        }
    }

    private Vector3 GetGoalPosition(){
        return _mood switch{
            GhostMood.Chase => player != null ? player.position : _roamTarget,
            GhostMood.Frightened => GetFleeTarget(),
            GhostMood.Eaten => ghostHome != null ? ghostHome.position : transform.position, _ => _roamTarget
        };
    }

    private Vector3 GetFleeTarget(){
        if (player == null) return _roamTarget;

        Vector3 best = transform.position;
        float bestScore = float.NegativeInfinity;

        foreach (var d in Cardinals){
            Vector3 candidate = transform.position + d * 4f;
            float score = Vector3.Distance(candidate, player.position) + Random.Range(0f, 2f);
            if (score > bestScore){
                bestScore = score;
                best = candidate;
            }
        }
        return best;
    }

    private void PickNewRoamTarget(){
        for (int i = 0; i < 10; i++){
            Vector3 candidate = transform.position + new Vector3(
                Random.Range(-roamWaypointRadius, roamWaypointRadius), 0,
                Random.Range(-roamWaypointRadius, roamWaypointRadius));

            if (!Physics.CheckSphere(candidate, 0.3f, wallLayer)){
                _roamTarget = candidate;
                return;
            }
        }
        _roamTarget = transform.position + transform.forward * 3f;
    }

    private List<Vector3> FindPath(Vector3 startWorld, Vector3 goalWorld){
        float step = raycastDistance * 1.2f;
        Vector3 start = SnapToStep(startWorld, step);
        Vector3 goal = SnapToStep(goalWorld, step);

        var open = new List<Node>();
        var closed = new HashSet<string>();
        open.Add(new Node(start, null, 0, Heuristic(start, goal)));

        int iterations = 0;
        while (open.Count > 0 && iterations < 200){
            iterations++;

            Node current = open[0];
            for (int i = 1; i < open.Count; i++)
                if (open[i].F < current.F) current = open[i];

            open.Remove(current);

            string key = NodeKey(current.Pos, step);
            if (!closed.Add(key)) continue;

            if (Vector3.Distance(current.Pos, goal) <= step * 1.5f)
                return BuildPath(current);

            foreach (var d in Cardinals){
                Vector3 neighbourPos = current.Pos + d * step;
                if (closed.Contains(NodeKey(neighbourPos, step))) continue;
                if (Physics.Raycast(current.Pos, d, step, wallLayer)) continue;

                open.Add(new Node(neighbourPos, current, current.G + step, Heuristic(neighbourPos, goal)));
            }
        }
        return new List<Vector3> { goalWorld };
    }

    private List<Vector3> BuildPath(Node endNode){
        var path = new List<Vector3>();
        Node n = endNode;
        while (n != null){
            path.Add(n.Pos);
            n = n.Parent;
        }
        path.Reverse();
        return path;
    }

    private float Heuristic(Vector3 a, Vector3 b){
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }

    private Vector3 SnapToStep(Vector3 pos, float step){
        return new Vector3(
            Mathf.Round(pos.x / step) * step,
            pos.y,
            Mathf.Round(pos.z / step) * step);
    }

    private string NodeKey(Vector3 pos, float step){
        return $"{Mathf.RoundToInt(pos.x / step)},{Mathf.RoundToInt(pos.z / step)}";
    }

    private void SetMood(GhostMood newMood){
        _mood = newMood;
        ApplyMoodColor();
        if (newMood == GhostMood.Roam) PickNewRoamTarget();
    }

    private void ApplyMoodColor(){
        Color c = _mood switch{
            GhostMood.Chase => chaseColor,
            GhostMood.Frightened => scaredColor,
            GhostMood.Eaten => eatenColor, _ => roamColor
        };
        foreach (var r in bodyRenderers)
            if (r != null && r.material != null) r.material.color = c;
    }

    void OnCollisionEnter(Collision c){ HandlePlayerContact(c.collider); }
    void OnTriggerEnter(Collider c){ HandlePlayerContact(c); }

    private void HandlePlayerContact(Collider other){
        if (!other.CompareTag("Player")) return;
        if (_mood == GhostMood.Eaten) return;
        if (!_canHit) return;

        var mgr = GameManager.Instance;
        if (mgr == null) return;

        if (mgr.IsInRaveMode || mgr.IsInSwitchMode){
            _canHit = false;
            GameEvents.GhostEaten();
            SetMood(GhostMood.Eaten);
            if (_respawnRoutine != null) StopCoroutine(_respawnRoutine);
            _respawnRoutine = StartCoroutine(RespawnRoutine());
        }
        else{
            _canHit = false;
            GameEvents.PlayerCaught();
            StartCoroutine(ResetHitCooldown());
        }
    }

    private IEnumerator ResetHitCooldown(){
        yield return new WaitForSeconds(1.5f);
        _canHit = true;
    }

    private IEnumerator RespawnRoutine(){
        _rb.linearVelocity = Vector3.zero;
        if (ghostHome != null) transform.position = ghostHome.position;
        _canHit = false;

        yield return new WaitForSeconds(GameManager.Instance.Config.ghostRespawnDelay);

        for (int i = 0; i < 20; i++){
            yield return new WaitForFixedUpdate();
            _rb.linearVelocity = Vector3.forward * GameManager.Instance.Config.ghostNormalSpeed;
            if (!Physics.CheckSphere(transform.position, 0.5f, wallLayer)) break;
        }

        _rb.linearVelocity = Vector3.zero;
        _currentPath.Clear();
        _pathIndex = 0;
        PickNewRoamTarget();

        var mgr = GameManager.Instance;
        if (mgr != null && (mgr.IsInRaveMode || mgr.IsInSwitchMode))
            SetMood(GhostMood.Frightened);
        else
            SetMood(GhostMood.Roam);

        _canHit = true;
    }
}