using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class Projectile : GameUnit
{
    [Header("Physics Settings")]
    public float impactForce = 10f;
    [Header("Movement Mode")]
    public bool flyStraight = false;

    [Header("Settings")]
    public float flySpeed = 20f;
    public float rotateDuration = 0.2f;
    public float lifeTime = 5f;

    [Header("Bounce Settings")]
    public float bounceSpeedMultiplier = 1f;
    public LayerMask wallLayer;

    [Header("Hit Adjustments")]
    public Vector3 hitRotationOffset = new Vector3(0, 0, 0); // Chỉnh góc cắm nếu cần

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

    private List<Sliceable> sliceables = new List<Sliceable>();
    private List<ObjectSliceable> hitObjects = new List<ObjectSliceable>();

    // --- CÁC BIẾN QUAN TRỌNG CHO LOGIC BÁM DÍNH (FIX MÉO HÌNH) ---
    private Vector3 savedScale;         // Lưu kích thước chuẩn của Rìu
    private Transform targetParent;     // Đối tượng đang bám vào
    private Vector3 relativePosition;   // Vị trí tương đối
    private Quaternion relativeRotation;// Góc xoay tương đối

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // 1. Lưu lại Scale chuẩn ngay từ đầu (Ví dụ: 0.22, 2.55, 0.11)
        // Để sau này dù có chuyện gì xảy ra cũng reset về được số này
        savedScale = transform.localScale;
    }

    void Update()
    {
        // Logic Bay (Chỉ chạy khi chưa dính)
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

    // [QUAN TRỌNG] Dùng LateUpdate để bám theo sau cùng (tránh rung lắc)
    void LateUpdate()
    {
        if (isStick)
        {
            if (targetParent != null)
            {
                // THUẬT TOÁN: Tự tính toán vị trí bám theo thay vì SetParent

                // 1. Dịch chuyển Rìu đến vị trí mới dựa trên vị trí tương đối đã lưu
                transform.position = targetParent.TransformPoint(relativePosition);

                // 2. Xoay Rìu theo góc xoay của Cha
                transform.rotation = targetParent.rotation * relativeRotation;

                // 3. Scale: Vì Rìu đứng độc lập (không con ai cả) nên Scale luôn chuẩn
            }
            else
            {
                // Nếu vật bị bám đã bị hủy (Enemy chết mất xác), thì Rìu tự hủy
                DespawnSelf();
            }
        }
    }

    public void InitializeArcThrow(Vector3 targetPosition, float speedMultiplier, float angleX)
    {
        sliceables.Clear();
        hitObjects.Clear();

        isStick = false;
        hasHit = false;
        isMoving = true;
        lastPosition = transform.position;
        targetParent = null; // Reset mục tiêu bám

        col.enabled = true;
        col.isTrigger = true;

        rb.isKinematic = true;
        rb.useGravity = false;
        // rb.linearVelocity = Vector3.zero; // Unity 6
        // rb.angularVelocity = Vector3.zero;

        transform.rotation = Quaternion.identity;
        transform.localRotation = Quaternion.Euler(rotation + Vector3.right * angleX);

        // Đảm bảo Rìu nằm ngoài cùng (không con ai cả)
        transform.SetParent(null);

        // Trả lại Scale chuẩn ban đầu
        transform.localScale = savedScale;

        Vector3 startPos = transform.position;
        currentSpeed = flySpeed * Mathf.Max(speedMultiplier, 0.5f);

        if (moveTween != null) moveTween.Kill();

        // Logic di chuyển (giữ nguyên code cũ của bạn)
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
            float pathLen = Vector3.Distance(startPos, controlPoint) + Vector3.Distance(controlPoint, targetPosition) + Vector3.Distance(targetPosition, overshootPoint);
            moveTween = transform.DOPath(path, pathLen / currentSpeed, PathType.CatmullRom).SetEase(Ease.Linear);
        }

        StartSpinning();
        CancelInvoke(nameof(DespawnSelf));
        Invoke(nameof(DespawnSelf), lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        if (other.CompareTag("Enemy"))
        {
            ApplyForceToTarget(other);
            Sliceable sliceable = other.GetComponent<Sliceable>();
            if (sliceable != null && !sliceables.Contains(sliceable.GetParentOld))
            {
                sliceables.Add(sliceable.GetParentOld);
                sliceable.AddProjectiles(this);
            }
            return;
        }

        if (other.CompareTag("Object"))
        {
            ApplyForceToTarget(other);
            ObjectSliceable objSlice = other.GetComponent<ObjectSliceable>();
            if (objSlice != null && !hitObjects.Contains(objSlice.GetOriginOld))
            {
                hitObjects.Add(objSlice.GetOriginOld);
                objSlice.AddProjectiles(this);
            }
            return;
        }

        if (other.CompareTag("Wall"))
        {
            HandleWallBounce();
        }
    }

    void ApplyForceToTarget(Collider targetCol)
    {
        Rigidbody targetRb = targetCol.GetComponent<Rigidbody>();
        if (targetRb == null) targetRb = targetCol.GetComponentInParent<Rigidbody>();
        if (targetRb != null)
        {
            targetRb.AddForce(currentFlightDirection.normalized * impactForce, ForceMode.Impulse);
        }
    }

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

    // --- HÀM NÀY ĐÃ SỬA TRIỆT ĐỂ VẤN ĐỀ MÉO VÀ XOAY ---
    public void StickProjectile(Transform parent)
    {
        hasHit = true;
        isMoving = false;

        CancelInvoke(nameof(DespawnSelf));
        if (moveTween != null) moveTween.Kill();
        StopSpinning(); // Dừng xoay ngay lập tức -> Giữ nguyên góc hiện tại

        // --- [SỬA TẠI ĐÂY] ---
        // Tôi đã Comment đoạn này lại. 
        // Bây giờ Rìu sẽ giữ nguyên góc xoay lúc nó chạm vào kẻ địch, không bị tự đổi hướng nữa.

        /* if (currentFlightDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(currentFlightDirection) * Quaternion.Euler(hitRotationOffset);
        }
        */

        rb.isKinematic = true;
        col.isTrigger = true;
        isStick = true;

        // Thiết lập mục tiêu để LateUpdate chạy theo (Math Follow)
        targetParent = parent;

        // Tính toán vị trí/góc tương đối
        relativePosition = targetParent.InverseTransformPoint(transform.position);

        // Tính góc lệch giữa Rìu và Cha tại thời điểm va chạm
        relativeRotation = Quaternion.Inverse(targetParent.rotation) * transform.rotation;

        // Tách ra ngoài để giữ Scale chuẩn (Fix lỗi méo hình)
        transform.SetParent(null);
        transform.localScale = savedScale;
    }

    void DespawnSelf()
    {
        isMoving = false;
        isStick = false;
        targetParent = null;

        CancelInvoke(nameof(DespawnSelf));
        if (moveTween != null) moveTween.Kill();
        StopSpinning();

        transform.SetParent(null);
        transform.localScale = savedScale;

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

    void OnDisable()
    {
        isMoving = false;
        isStick = false;
        CancelInvoke(nameof(DespawnSelf));
        if (moveTween != null) moveTween.Kill();
        StopSpinning();
    }
}