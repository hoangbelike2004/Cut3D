using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class Projectile : GameUnit
{
    [Header("Physics Settings")]
    public float impactForce = 10f; // Lực đẩy khi va chạm
    [Header("Movement Mode")]
    public bool flyStraight = false;

    [Header("Settings")]
    public float flySpeed = 20f;
    public float rotateDuration = 0.2f;
    public float lifeTime = 5f;

    [Header("Bounce Settings")]
    public float bounceSpeedMultiplier = 1f;
    public LayerMask wallLayer;

    // --- TRẠNG THÁI ---
    public bool isStick = false;
    public Vector3 rotation;

    private bool hasHit = false;
    private bool isMoving = false;

    private Rigidbody rb;
    private Collider col;
    private Tween spinTween;
    private Tween moveTween;

    private Vector3 currentFlightDirection;
    private Vector3 lastPosition;

    private float currentSpeed;

    // List theo dõi Enemy đã chém (người)
    private List<Sliceable> sliceables = new List<Sliceable>();

    // [MỚI] List theo dõi Object đã chém (vật)
    private List<ObjectSliceable> hitObjects = new List<ObjectSliceable>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    void Update()
    {
        if (isMoving && !hasHit)
        {
            Vector3 displacement = transform.position - lastPosition;
            if (displacement.sqrMagnitude > 0.0001f)
            {
                currentFlightDirection = displacement.normalized;
            }
            lastPosition = transform.position;
        }
    }

    public void InitializeArcThrow(Vector3 targetPosition, float speedMultiplier, float angleX)
    {
        // 1. Reset List
        sliceables.Clear();
        hitObjects.Clear(); // [MỚI] Reset list vật thể

        isStick = false;
        hasHit = false;
        isMoving = true;
        lastPosition = transform.position;

        col.enabled = true;
        col.isTrigger = true;

        // Reset Physics
        rb.isKinematic = false; // Tắt kinematic để bật vật lý (Unity 6 cần bật cái này để set velocity nếu muốn)
        // Nhưng ở đây ta dùng Tween nên ta set lại kinematic = true ngay sau đó hoặc giữ nguyên logic cũ của bạn
        rb.isKinematic = true;
        rb.useGravity = false;

        // Unity 6 dùng linearVelocity, Unity cũ dùng velocity. 
        // Vì đang dùng Tween nên set về 0 cho chắc.
        // rb.linearVelocity = Vector3.zero; 

        transform.rotation = Quaternion.identity;
        transform.localRotation = Quaternion.Euler(rotation + Vector3.right * angleX);

        Vector3 startPos = transform.position;
        currentSpeed = flySpeed * Mathf.Max(speedMultiplier, 0.5f);

        if (moveTween != null) moveTween.Kill();

        // 2. DI CHUYỂN (Logic cũ giữ nguyên)
        if (flyStraight)
        {
            Vector3 dir = (targetPosition - startPos).normalized;
            currentFlightDirection = dir;
            Vector3 overshootPoint = targetPosition + dir * 20f;
            float duration = Vector3.Distance(startPos, overshootPoint) / currentSpeed;

            moveTween = transform.DOMove(overshootPoint, duration).SetEase(Ease.Linear);
        }
        else
        {
            Vector3 midPoint = (startPos + targetPosition) / 2;
            float distance = Vector3.Distance(startPos, targetPosition);
            Vector3 curveOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(0.2f, 1f), 0).normalized * (distance * 0.3f);
            Vector3 controlPoint = midPoint + curveOffset;
            Vector3 endDir = (targetPosition - controlPoint).normalized;
            Vector3 overshootPoint = targetPosition + endDir * 20f;

            Vector3[] path = new Vector3[] { controlPoint, targetPosition, overshootPoint };
            float pathLen = Vector3.Distance(startPos, controlPoint) +
                            Vector3.Distance(controlPoint, targetPosition) +
                            Vector3.Distance(targetPosition, overshootPoint);

            moveTween = transform.DOPath(path, pathLen / currentSpeed, PathType.CatmullRom).SetEase(Ease.Linear);
        }

        StartSpinning();
        CancelInvoke(nameof(DespawnSelf));
        Invoke(nameof(DespawnSelf), lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // --- CASE 1: XỬ LÝ ENEMY (NGƯỜI) ---
        if (other.CompareTag("Enemy"))
        {
            ApplyForceToTarget(other); // Đẩy lùi

            Sliceable sliceable = other.GetComponent<Sliceable>();
            if (sliceable != null && !sliceables.Contains(sliceable.GetParentOld))
            {
                sliceables.Add(sliceable.GetParentOld);
                sliceable.AddProjectiles(this);
            }
            return;
        }

        // --- CASE 2: [MỚI] XỬ LÝ OBJECT (VẬT) ---
        // Bạn nhớ gán Tag "Object" hoặc "Prop" cho các đồ vật nhé
        if (other.CompareTag("Object"))
        {
            ApplyForceToTarget(other); // Đẩy lùi đồ vật

            ObjectSliceable objSlice = other.GetComponent<ObjectSliceable>();
            // Kiểm tra xem đã chém vào vật gốc này chưa (để tránh chém nhiều lần vào các mảnh vỡ của cùng 1 vật trong 1 frame)
            if (objSlice != null && !hitObjects.Contains(objSlice.GetOriginOld))
            {
                hitObjects.Add(objSlice.GetOriginOld);
                objSlice.AddProjectiles(this);
            }
            return;
        }

        // --- CASE 3: XỬ LÝ TƯỜNG (PHẢN XẠ) ---
        if (other.CompareTag("Wall"))
        {
            HandleWallBounce();
        }
    }

    // --- Tách hàm Physics cho gọn ---
    void ApplyForceToTarget(Collider targetCol)
    {
        Rigidbody targetRb = targetCol.GetComponent<Rigidbody>();
        if (targetRb == null) targetRb = targetCol.GetComponentInParent<Rigidbody>();

        if (targetRb != null)
        {
            // Đẩy theo hướng đạn bay
            targetRb.AddForce(currentFlightDirection.normalized * impactForce, ForceMode.Impulse);
        }
    }

    // --- Tách hàm Nảy tường cho gọn ---
    void HandleWallBounce()
    {
        if (moveTween != null) moveTween.Kill();

        Vector3 reflectDir = Vector3.up;
        RaycastHit hit;

        if (Physics.Raycast(transform.position - currentFlightDirection * 1f, currentFlightDirection, out hit, 2f, wallLayer))
        {
            reflectDir = Vector3.Reflect(currentFlightDirection, hit.normal);
        }
        else
        {
            reflectDir = Vector3.Reflect(currentFlightDirection, Vector3.up);
        }

        currentFlightDirection = reflectDir.normalized;
        currentSpeed *= bounceSpeedMultiplier;

        float farDistance = 50f;
        Vector3 infiniteTarget = transform.position + currentFlightDirection * farDistance;
        float duration = farDistance / currentSpeed;

        moveTween = transform.DOMove(infiniteTarget, duration).SetEase(Ease.Linear);
    }

    void DespawnSelf()
    {
        isMoving = false;
        CancelInvoke(nameof(DespawnSelf));
        if (moveTween != null) moveTween.Kill();
        StopSpinning();
        SimplePool.Despawn(this);
    }

    void StartSpinning()
    {
        StopSpinning();
        spinTween = transform
            .DOLocalRotate(new Vector3(0, 0, -360), rotateDuration, RotateMode.FastBeyond360)
            .SetRelative(true)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental);
    }

    void StopSpinning()
    {
        if (spinTween != null)
        {
            spinTween.Kill();
            spinTween = null;
        }
    }

    public void StickProjectile(Transform parent)
    {
        hasHit = true;
        isMoving = false;
        CancelInvoke(nameof(DespawnSelf));
        if (moveTween != null) moveTween.Kill();
        StopSpinning();

        rb.isKinematic = true;
        col.isTrigger = true;
        isStick = true;
        transform.SetParent(parent, true);
    }

    void OnDisable()
    {
        isMoving = false;
        CancelInvoke(nameof(DespawnSelf));
        if (moveTween != null) moveTween.Kill();
        StopSpinning();
    }
}