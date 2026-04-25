using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GhostController : MonoBehaviour{
    public enum GhostMood { Chase, Frightened, Eaten, Roam }

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

    private Rigidbody rb;
    private GhostMood mood = GhostMood.Roam;
    private Coroutine respawnRoutine;

    private List<Vector3> currentPath = new List<Vector3>();
    private int pathIndex = 0;
    private Vector3 roamTarget;

    private static readonly Vector3[] Cardinals = { Vector3.forward, Vector3.back, Vector3.left, Vector3.right };

    private bool canHit = true;
    
    private class Node{
        public Vector3 pos;
        public Node parent;
        public float g;
        public float h;
        public float f => g + h;

        public Node(Vector3 pos, Node parent, float g, float h){
            this.pos = pos;
            this.parent = parent;
            this.g = g;
            this.h = h;
        }
    }

    void Awake(){
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

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

    private void HandleNormalMode(){ if (mood != GhostMood.Eaten) SetMood(GhostMood.Roam); }
    private void HandleRaveMode(){ if (mood != GhostMood.Eaten) SetMood(GhostMood.Frightened); }
    private void HandleSwitchMode(){ if (mood != GhostMood.Eaten) SetMood(GhostMood.Frightened); }

    void FixedUpdate(){
        if (GameManager.Instance == null) return;

        float dist = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

        if (mood == GhostMood.Roam && dist <= playerDetectionRange)
            SetMood(GhostMood.Chase);
        else if (mood == GhostMood.Chase && dist > playerDetectionRange)
            SetMood(GhostMood.Roam);

        float speed = mood == GhostMood.Frightened
            ? GameManager.Instance.Config.ghostFrightenedSpeed
            : GameManager.Instance.Config.ghostNormalSpeed;
        MoveAlongPath(speed);
    }

    private void MoveAlongPath(float speed){
        if (currentPath == null || pathIndex >= currentPath.Count){
            rb.linearVelocity = Vector3.zero;
            if (mood == GhostMood.Roam) PickNewRoamTarget();
            return;
        }

        Vector3 target = currentPath[pathIndex];
        target.y = transform.position.y;
        Vector3 dir = target - transform.position;

        if (dir.magnitude <= waypointReachedDistance){
            pathIndex++;
            return;
        }

        rb.linearVelocity = dir.normalized * speed;
    }

    private IEnumerator PathUpdateLoop(){
        while (true){
            yield return new WaitForSeconds(pathUpdateInterval);
            if (GameManager.Instance == null) continue;

            List<Vector3> newPath = FindPath(transform.position, GetGoalPosition());
            if (newPath != null && newPath.Count > 0){
                currentPath = newPath;
                pathIndex = 0;
            }
        }
    }

    private Vector3 GetGoalPosition(){
        return mood switch{
            GhostMood.Chase => player != null ? player.position : roamTarget,
            GhostMood.Frightened => GetFleeTarget(),
            GhostMood.Eaten => ghostHome != null ? ghostHome.position : transform.position,
            GhostMood.Roam => roamTarget, _ => roamTarget
        };
    }

    private Vector3 GetFleeTarget(){
        if (player == null) return roamTarget;

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
                Random.Range(-roamWaypointRadius, roamWaypointRadius),
                0,
                Random.Range(-roamWaypointRadius, roamWaypointRadius));

            if (!Physics.CheckSphere(candidate, 0.3f, wallLayer)){
                roamTarget = candidate;
                return;
            }
        }
        roamTarget = transform.position + transform.forward * 3f;
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
                if (open[i].f < current.f) current = open[i];

            open.Remove(current);

            string key = NodeKey(current.pos, step);
            if (closed.Contains(key)) continue;
            closed.Add(key);

            if (Vector3.Distance(current.pos, goal) <= step * 1.5f)
                return BuildPath(current);

            foreach (var d in Cardinals){
                Vector3 neighbourPos = current.pos + d * step;
                if (closed.Contains(NodeKey(neighbourPos, step))) continue;
                if (Physics.Raycast(current.pos, d, step, wallLayer)) continue;

                open.Add(new Node(neighbourPos, current, current.g + step, Heuristic(neighbourPos, goal)));
            }
        }
        return new List<Vector3> { goalWorld };
    }

    private List<Vector3> BuildPath(Node endNode){
        var path = new List<Vector3>();
        Node n = endNode;
        while (n != null){
            path.Add(n.pos);
            n = n.parent;
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
        mood = newMood;
        ApplyMoodColor();
        if (newMood == GhostMood.Roam) PickNewRoamTarget();
    }

    private void ApplyMoodColor(){
        Color c = mood switch{
            GhostMood.Chase => chaseColor,
            GhostMood.Frightened => scaredColor,
            GhostMood.Eaten => eatenColor,
            GhostMood.Roam => roamColor, _ => Color.white
        };
        foreach (var r in bodyRenderers)
            if (r != null && r.material != null) r.material.color = c;
    }

    void OnCollisionEnter(Collision c){ HandlePlayerContact(c.collider); }
    void OnTriggerEnter(Collider c){ HandlePlayerContact(c); }

    private void HandlePlayerContact(Collider other){
        if (!other.CompareTag("Player")) return;
        if (mood == GhostMood.Eaten) return;
        if (!canHit) return;

        var mgr = GameManager.Instance;
        if (mgr == null) return;

        if (mgr.IsInRaveMode || mgr.IsInSwitchMode){
            canHit = false;
            GameEvents.GhostEaten();
            SetMood(GhostMood.Eaten);
            if (respawnRoutine != null) StopCoroutine(respawnRoutine);
            respawnRoutine = StartCoroutine(RespawnRoutine());
        }
        else{
            canHit = false;
            GameEvents.PlayerCaught();
            StartCoroutine(ResetHitCooldown());
        }
    }

    private IEnumerator ResetHitCooldown(){
        yield return new WaitForSeconds(1.5f);
        canHit = true;
    }
    
    private IEnumerator RespawnRoutine(){
        rb.linearVelocity = Vector3.zero;
        if (ghostHome != null) transform.position = ghostHome.position;

        yield return new WaitForSeconds(GameManager.Instance.Config.ghostRespawnDelay);

        var mgr = GameManager.Instance;
        if (mgr != null && (mgr.IsInRaveMode || mgr.IsInSwitchMode))
            SetMood(GhostMood.Frightened);
        else
            SetMood(GhostMood.Roam);
    }
}