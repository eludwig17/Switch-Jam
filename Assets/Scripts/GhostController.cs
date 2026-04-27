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

    [Header("Grid setup")]
    [SerializeField] private Vector3 gridOrigin = new Vector3(-12f, 0f, -12f);
    [SerializeField] private int gridWidth = 25;
    [SerializeField] private int gridHeight = 25;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private float wallCheckRadius = 0.35f;

    [Header("Tuning")]
    [SerializeField] private float pathUpdateInterval = 0.25f;
    [SerializeField] private float waypointReachedDistance = 0.3f;
    [SerializeField] private float playerDetectionRange = 8f;
    [SerializeField] private float roamWaypointRadius = 5f;
    [SerializeField] private float stuckCheckInterval = 1.5f;
    [SerializeField] private float stuckMoveThreshold = 0.05f;
    [SerializeField] private float nudgeForce = 0.5f;

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
    private Vector3 _lastCheckedPosition;
    private float _stuckTimer;

    private static bool[,] _walkable;
    private static int _width;
    private static int _height;
    private static float _cellSize;
    private static Vector3 _origin;
    private static bool _gridReady = false;

    private static readonly Vector2Int[] CardinalCells = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };
    private static readonly Vector3[] Cardinals = {
        Vector3.forward, Vector3.back, Vector3.left, Vector3.right
    };

    void Awake(){
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        if (player == null){
            var pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) player = pObj.transform;
        }
        if (!_gridReady) Grid();
        PickNewRoamTarget();
        _lastCheckedPosition = transform.position;
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
        if (GameManager.Instance.CurrentState == GameManager.GameState.MainMenu || GameManager.Instance.CurrentState == GameManager.GameState.GameOver) return;
        float dist = player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

        if (_mood == GhostMood.Roam && dist <= playerDetectionRange)
            SetMood(GhostMood.Chase);
        else if (_mood == GhostMood.Chase && dist > playerDetectionRange)
            SetMood(GhostMood.Roam);

        float speed = _mood == GhostMood.Frightened ? GameManager.Instance.Config.ghostFrightenedSpeed : GameManager.Instance.Config.ghostNormalSpeed;
        MoveAlongPath(speed);
        
        _stuckTimer += Time.fixedDeltaTime;
        if (_stuckTimer >= stuckCheckInterval){
            _stuckTimer = 0f;
            if (Vector3.Distance(transform.position, _lastCheckedPosition) < stuckMoveThreshold && _mood != GhostMood.Eaten)
                NudgeOutOfStuck();
            _lastCheckedPosition = transform.position;
        }
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

    private void NudgeOutOfStuck(){
        foreach (var d in Cardinals){
            Vector2Int cell = WorldToGrid(transform.position + d * _cellSize);
            if (InBounds(cell.x, cell.y) && _walkable[cell.x, cell.y]){
                _rb.linearVelocity = d * nudgeForce;
                _currentPath.Clear();
                _pathIndex = 0;
                PickNewRoamTarget();
                return;
            }
        }
        _rb.linearVelocity = Vector3.zero;
        transform.position += Vector3.right * 0.1f;
    }

    private IEnumerator PathUpdateLoop(){
        yield return new WaitForSeconds(Random.Range(0f, pathUpdateInterval));
        while (true){
            yield return new WaitForSeconds(pathUpdateInterval);
            if (GameManager.Instance == null) continue;
            if (GameManager.Instance.CurrentState == GameManager.GameState.MainMenu || GameManager.Instance.CurrentState == GameManager.GameState.GameOver) continue;
            List<Vector3> newPath = FindPath(transform.position, GetGoalPosition());
            if (newPath != null && newPath.Count > 0){
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
        for (int i = 0; i < 15; i++){
            Vector3 candidate = transform.position + new Vector3(
                Random.Range(-roamWaypointRadius, roamWaypointRadius), 0,
                Random.Range(-roamWaypointRadius, roamWaypointRadius));

            if (!_gridReady){ _roamTarget = candidate; return; }
            Vector2Int cell = WorldToGrid(candidate);
            if (InBounds(cell.x, cell.y) && _walkable[cell.x, cell.y]){
                _roamTarget = GridToWorld(cell.x, cell.y);
                return;
            }
        }
        _roamTarget = transform.position + transform.forward * 3f;
    }

    private void Grid(){
        _width = gridWidth;
        _height = gridHeight;
        _cellSize = cellSize;
        _origin = gridOrigin;
        _walkable = new bool[gridWidth, gridHeight];
        float checkY = gridOrigin.y + cellSize * 0.5f;

        for (int x = 0; x < gridWidth; x++){
            for (int z = 0; z < gridHeight; z++){
                Vector3 worldPos = GridToWorld(x, z);
                worldPos.y = checkY;
                _walkable[x, z] = !Physics.CheckSphere(worldPos, wallCheckRadius, wallLayer);
            }
        }
        _gridReady = true;
    }

    private static Vector3 GridToWorld(int x, int z){
        return new Vector3(_origin.x + x * _cellSize + _cellSize * 0.5f, _origin.y, _origin.z + z * _cellSize + _cellSize * 0.5f);
    }

    private static Vector2Int WorldToGrid(Vector3 world){
        int x = Mathf.FloorToInt((world.x - _origin.x) / _cellSize);
        int z = Mathf.FloorToInt((world.z - _origin.z) / _cellSize);
        return new Vector2Int(
            Mathf.Clamp(x, 0, _width - 1),
            Mathf.Clamp(z, 0, _height - 1));
    }

    private static bool InBounds(int x, int z){
        return x >= 0 && x < _width && z >= 0 && z < _height;
    }

    private Vector2Int NearestWalkable(Vector2Int cell){
        for (int r = 1; r < 6; r++){
            for (int dx = -r; dx <= r; dx++){
                for (int dz = -r; dz <= r; dz++){
                    int nx = cell.x + dx, nz = cell.y + dz;
                    if (InBounds(nx, nz) && _walkable[nx, nz])
                        return new Vector2Int(nx, nz);
                }
            }
        }
        return cell;
    }

    private List<Vector3> FindPath(Vector3 startWorld, Vector3 goalWorld){
        if (!_gridReady) return new List<Vector3>{ goalWorld };

        Vector2Int start = WorldToGrid(startWorld);
        Vector2Int goal = WorldToGrid(goalWorld);

        if (!_walkable[start.x, start.y]) start = NearestWalkable(start);
        if (!_walkable[goal.x, goal.y]) goal = NearestWalkable(goal);

        if (start == goal) return new List<Vector3>{ GridToWorld(goal.x, goal.y) };

        var parent = new Dictionary<Vector2Int, Vector2Int>();
        var frontier = new Queue<Vector2Int>();

        frontier.Enqueue(start);
        parent[start] = start;

        bool found = false;
        while (frontier.Count > 0){
            Vector2Int current = frontier.Dequeue();
            if (current == goal){ found = true; break; }

            foreach (var dir in CardinalCells){
                Vector2Int next = current + dir;
                if (!InBounds(next.x, next.y)) continue;
                if (!_walkable[next.x, next.y]) continue;
                if (parent.ContainsKey(next)) continue;

                parent[next] = current;
                frontier.Enqueue(next);
            }
        }

        if (!found) return new List<Vector3>{ goalWorld };

        var path = new List<Vector3>();
        Vector2Int step = goal;
        while (step != start){
            path.Add(GridToWorld(step.x, step.y));
            step = parent[step];
        }
        path.Reverse();
        return path;
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
        _rb.isKinematic = true;
        _canHit = false;

        if (ghostHome != null){
            Vector2 offset = Random.insideUnitCircle * 0.4f;
            transform.position = ghostHome.position + new Vector3(offset.x, 0f, offset.y);
        }

        yield return new WaitForSeconds(GameManager.Instance.Config.ghostRespawnDelay);

        _rb.isKinematic = false;

        var dirs = new List<Vector3>(Cardinals);
        for (int i = dirs.Count - 1; i > 0; i--){
            int j = Random.Range(0, i + 1);
            (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
        }

        Vector3 exitDir = Vector3.forward;
        foreach (var d in dirs){
            Vector2Int exitCell = WorldToGrid(transform.position + d * _cellSize);
            if (InBounds(exitCell.x, exitCell.y) && _walkable[exitCell.x, exitCell.y]){
                exitDir = d;
                break;
            }
        }

        for (int i = 0; i < 12; i++){
            yield return new WaitForFixedUpdate();
            Vector2Int here = WorldToGrid(transform.position);
            if (InBounds(here.x, here.y) && _walkable[here.x, here.y]) break;
            _rb.MovePosition(transform.position + exitDir * (GameManager.Instance.Config.ghostNormalSpeed * Time.fixedDeltaTime));
        }

        _rb.linearVelocity = Vector3.zero;
        _currentPath.Clear();
        _pathIndex = 0;
        _lastCheckedPosition = transform.position;
        _stuckTimer = 0f;
        PickNewRoamTarget();

        var mgr = GameManager.Instance;
        if (mgr != null && (mgr.IsInRaveMode || mgr.IsInSwitchMode))
            SetMood(GhostMood.Frightened);
        else
            SetMood(GhostMood.Roam);

        _canHit = true;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected(){
        if (!_gridReady) return;
        for (int x = 0; x <_width; x++){
            for (int z = 0; z < _height; z++){
                Gizmos.color = _walkable[x, z] ? new Color(0f, 1f, 0f, 0.12f) : new Color(1f, 0f, 0f, 0.12f);
                Vector3 centre = GridToWorld(x, z);
                centre.y = _origin.y + 0.1f;
                Gizmos.DrawCube(centre, Vector3.one * (_cellSize * 0.88f));
            }
        }
    }
#endif
}