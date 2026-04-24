using UnityEngine;
using System;
using System.IO;

[RequireComponent(typeof(Rigidbody))]
public class ChompController : MonoBehaviour {
    [Header("References")]
    [SerializeField] private Transform bodyVisual;       // The mesh to flip/color
    [SerializeField] private LayerMask wallLayer;

    [Header("Tuning")]
    [SerializeField] private float raycastDistance = 0.55f;
    [SerializeField] private float turnBufferTime = 0.2f; // Queue a turn just before a corner

    private Rigidbody rb;
    private Vector3 currentDir = Vector3.zero;
    private Vector3 queuedDir = Vector3.zero;
    private float queuedDirTimer = 0f;
    private bool inverted = false;
    private int fixedTick = 0;
    private Vector3 lastFixedPosition;
    private int stuckTickCount = 0;
    private float nextCollisionLogTime = 0f;

    // Public for GhostController-AI-takeover during SWITCH mode
    public Vector3 CurrentDirection => currentDir;

    void Awake(){
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        if (rb.isKinematic){
            rb.isKinematic = false;
        }
        lastFixedPosition = transform.position;
    }

    void OnEnable(){
        GameEvents.OnControlsInvertedChanged += OnInvertChanged;
    }

    void OnDisable(){
        GameEvents.OnControlsInvertedChanged -= OnInvertChanged;
    }

    private void OnInvertChanged(bool isInverted){
        inverted = isInverted;
    }

    void Update(){
        ReadInput();
        TryApplyQueuedDirection();
    }

    void FixedUpdate(){
        fixedTick++;
        if (currentDir == Vector3.zero) return;
        bool blocked = IsBlocked(currentDir);
        if (blocked){rb.linearVelocity = Vector3.zero;
            return;
        }

        float speed = GameManager.Instance != null
            ? (GameManager.Instance.IsInSwitchMode
                ? GameManager.Instance.Config.ghostFormMoveSpeed
                : GameManager.Instance.Config.chompMoveSpeed)
            : 5f;

        rb.linearVelocity = currentDir * speed;
        float moved = Vector3.Distance(transform.position, lastFixedPosition);
        if (rb.linearVelocity.sqrMagnitude > 0.01f && moved < 0.0005f){
            stuckTickCount++;
        }
        else{
            stuckTickCount = 0;
        }

        if (stuckTickCount >= 10){
            bool forwardBlocked = IsBlocked(currentDir);
            int wallOverlaps = GetWallOverlapCount();
        }

        lastFixedPosition = transform.position;
        if (fixedTick % 20 == 0){
            
        }
        FaceDirection(currentDir);
    }

    private void ReadInput(){
        Vector3 input = Vector3.zero;
        // Only one axis at a time (grid-movement feel)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(h) > 0.1f)      input = new Vector3(Mathf.Sign(h), 0, 0);
        else if (Mathf.Abs(v) > 0.1f) input = new Vector3(0, 0, Mathf.Sign(v));

        if (input == Vector3.zero) return;

        if (inverted) input = -input;
        if (input == currentDir) return;

        queuedDir = input;
        queuedDirTimer = turnBufferTime;
    }

    private void TryApplyQueuedDirection(){
        if (queuedDirTimer <= 0f) return;
        queuedDirTimer -= Time.deltaTime;
        if (queuedDir == Vector3.zero) return;

        // Immediately accept reverse
        if (queuedDir == -currentDir){
            currentDir = queuedDir;
            queuedDir = Vector3.zero;
            queuedDirTimer = 0f;
            return;
        }

        if (!IsBlocked(queuedDir)){
            currentDir = queuedDir;
            Vector3 beforeSnap = transform.position;
            queuedDir = Vector3.zero;
            queuedDirTimer = 0f;
        }
        else if (queuedDir != Vector3.zero){ }
    }

    private bool IsBlocked(Vector3 dir){
        return Physics.Raycast(transform.position, dir, raycastDistance, wallLayer);
    }

    private int GetWallOverlapCount(){
        Collider[] overlaps = Physics.OverlapSphere(transform.position, 0.25f, wallLayer);
        return overlaps != null ? overlaps.Length : 0;
    }

    private void FaceDirection(Vector3 dir){
        if (bodyVisual == null) return;
        bodyVisual.rotation = Quaternion.Slerp(
            bodyVisual.rotation,
            Quaternion.LookRotation(dir, Vector3.up),
            15f * Time.fixedDeltaTime);
    }

    void OnTriggerEnter(Collider other){
        // Collision with a ghost is handled by GhostController (it knows the current mode)
    }

    void OnCollisionStay(Collision collision)
    {
        if (stuckTickCount < 5) return;
        if (Time.time < nextCollisionLogTime) return;
        nextCollisionLogTime = Time.time + 0.2f;

        Collider col = collision.collider;
        if (col == null) return;
    }
}
