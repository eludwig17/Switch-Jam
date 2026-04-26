using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ChompController : MonoBehaviour{
    [Header("References")] [SerializeField]
    private Transform bodyVisual;

    [SerializeField] private LayerMask wallLayer;

    [Header("Tuning")] [SerializeField] private float raycastDistance = 0.55f;
    [SerializeField] private float turnBufferTime = 0.2f;

    private Rigidbody _rb;
    private Vector3 _currentDir;
    private Vector3 _queuedDir;
    private float _queuedDirTimer;
    private bool _inverted;
    private Vector3 _startPosition;

    public Vector3 CurrentDirection => _currentDir;

    void Awake(){
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        if (_rb.isKinematic) _rb.isKinematic = false;

        _startPosition = transform.position;
    }

    void OnEnable(){
        GameEvents.OnControlsInvertedChanged += OnInvertChanged;
        GameEvents.OnPlayerCaught += HandleCaught;
    }

    void OnDisable(){
        GameEvents.OnControlsInvertedChanged -= OnInvertChanged;
        GameEvents.OnPlayerCaught -= HandleCaught;
    }

    private void OnInvertChanged(bool isInverted){
        _inverted = isInverted;
    }

    private void HandleCaught(){
        transform.position = _startPosition;
        _currentDir = Vector3.zero;
        _queuedDir = Vector3.zero;
        _queuedDirTimer = 0f;
        _rb.linearVelocity = Vector3.zero;
    }

    void Update(){
        ReadInput();
        TryApplyQueuedDirection();
    }

    void FixedUpdate(){
        if (_currentDir == Vector3.zero) return;
        if (IsBlocked(_currentDir)){
            _rb.linearVelocity = Vector3.zero;
            return;
        }

        float speed = GameManager.Instance != null
            ? (GameManager.Instance.IsInSwitchMode
                ? GameManager.Instance.Config.ghostFormMoveSpeed
                : GameManager.Instance.Config.chompMoveSpeed)
            : 5f;

        _rb.linearVelocity = _currentDir * speed;
        FaceDirection(_currentDir);
    }

    private void ReadInput(){
        Vector3 input = Vector3.zero;
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(h) > 0.1f) input = new Vector3(Mathf.Sign(h), 0, 0);
        else if (Mathf.Abs(v) > 0.1f) input = new Vector3(0, 0, Mathf.Sign(v));

        if (input == Vector3.zero) return;
        if (_inverted) input = -input;
        if (input == _currentDir) return;

        _queuedDir = input;
        _queuedDirTimer = turnBufferTime;
    }

    private void TryApplyQueuedDirection(){
        if (_queuedDirTimer <= 0f) return;
        _queuedDirTimer -= Time.deltaTime;
        if (_queuedDir == Vector3.zero) return;

        if (_queuedDir == -_currentDir){
            _currentDir = _queuedDir;
            _queuedDir = Vector3.zero;
            _queuedDirTimer = 0f;
            return;
        }

        if (!IsBlocked(_queuedDir)){
            _currentDir = _queuedDir;
            _queuedDir = Vector3.zero;
            _queuedDirTimer = 0f;
        }
    }

    private bool IsBlocked(Vector3 dir){
        return Physics.Raycast(transform.position, dir, raycastDistance, wallLayer);
    }

    private void FaceDirection(Vector3 dir){
        if (bodyVisual == null) return;
        bodyVisual.rotation = Quaternion.Slerp(
            bodyVisual.rotation,
            Quaternion.LookRotation(dir, Vector3.up),
            15f * Time.fixedDeltaTime);
    }
}