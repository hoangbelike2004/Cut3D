using UnityEngine;
using DG.Tweening;

public class Projectile : GameUnit
{
    [Header("Movement Mode")]
    public bool flyStraight = false;

    [Header("Settings")]
    public float flySpeed = 20f;
    public float rotateDuration = 0.2f;
    public float lifeTime = 5f;

    [Header("Bounce Settings")]
    // [SỬA] Đổi tên cho rõ nghĩa hơn, đây là tốc độ tăng thêm khi nảy
    public float bounceSpeedMultiplier = 1f;
    public LayerMask wallLayer;

    // --- TRẠNG THÁI ---
    public bool isStick = false;
    public Vector3 rotation;

    private bool hasHit = false; // Chỉ true khi trúng Enemy hoặc Stick, KHÔNG true khi trúng tường
    private bool isMoving = false;

    private Rigidbody rb;
    private Collider col;
    private Tween spinTween;
    private Tween moveTween;

    private Vector3 currentFlightDirection;
    private Vector3 lastPosition;

    // Biến lưu tốc độ hiện tại (để khi nảy có thể giữ hoặc tăng tốc độ)
    private float currentSpeed;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    void Update()
    {
        if (isMoving && !hasHit)
        {
            // Tính hướng bay thực tế (quan trọng để tính góc phản xạ đúng)
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
        // 1. Reset
        isStick = false;
        hasHit = false;
        isMoving = true;
        lastPosition = transform.position;

        col.enabled = true;
        col.isTrigger = true;

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

        transform.rotation = Quaternion.identity;
        transform.localRotation = Quaternion.Euler(rotation + Vector3.right * angleX);

        Vector3 startPos = transform.position;

        // Lưu tốc độ hiện tại
        currentSpeed = flySpeed * Mathf.Max(speedMultiplier, 0.5f);

        if (moveTween != null) moveTween.Kill();

        // 2. DI CHUYỂN BAN ĐẦU (Đến target)
        // Chúng ta bắn nó đi một đoạn xa hơn target (overshoot) để đảm bảo nó bay qua đích nếu không trúng gì
        if (flyStraight)
        {
            Vector3 dir = (targetPosition - startPos).normalized;
            currentFlightDirection = dir;
            Vector3 overshootPoint = targetPosition + dir * 20f; // Bay xa quá đích

            float duration = Vector3.Distance(startPos, overshootPoint) / currentSpeed;

            moveTween = transform.DOMove(overshootPoint, duration)
                .SetEase(Ease.Linear);
        }
        else
        {
            // Logic bay cong giữ nguyên
            Vector3 midPoint = (startPos + targetPosition) / 2;
            float distance = Vector3.Distance(startPos, targetPosition);
            Vector3 curveOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(0.2f, 1f), 0).normalized * (distance * 0.3f);
            Vector3 controlPoint = midPoint + curveOffset;
            Vector3 endDir = (targetPosition - controlPoint).normalized;
            Vector3 overshootPoint = targetPosition + endDir * 20f; // Bay tiếp sau khi qua đỉnh

            Vector3[] path = new Vector3[] { controlPoint, targetPosition, overshootPoint };

            // Tính tổng độ dài đường cong
            float pathLen = Vector3.Distance(startPos, controlPoint) +
                            Vector3.Distance(controlPoint, targetPosition) +
                            Vector3.Distance(targetPosition, overshootPoint);

            moveTween = transform.DOPath(path, pathLen / currentSpeed, PathType.CatmullRom)
                .SetEase(Ease.Linear);
        }

        StartSpinning();

        // --- QUẢN LÝ THỜI GIAN SỐNG ---
        // Chỉ Despawn khi hết giờ, bất kể nó đang bay đi đâu
        CancelInvoke(nameof(DespawnSelf));
        Invoke(nameof(DespawnSelf), lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        // Nếu đã trúng mục tiêu "chốt" (như Enemy) thì thôi
        if (hasHit) return;

        // --- XỬ LÝ ENEMY (Điểm dừng) ---
        if (other.CompareTag("Enemy"))
        {
            Sliceable sliceable = other.GetComponent<Sliceable>();
            if (sliceable != null)
            {
                hasHit = true; // Đánh dấu đã xong việc
                sliceable.AddProjectiles(this);
            }
            return;
        }

        // --- XỬ LÝ PHẢN XẠ TƯỜNG (Bay tiếp) ---
        if (other.CompareTag("Wall"))
        {
            // [QUAN TRỌNG] KHÔNG set hasHit = true ở đây. 
            // Để nó có thể va chạm với tường tiếp theo.

            // 1. Ngắt chuyển động hiện tại
            if (moveTween != null) moveTween.Kill();

            // 2. Tính toán hướng phản xạ
            Vector3 reflectDir = Vector3.up;
            RaycastHit hit;

            // Bắn Raycast ngược lại 1 chút để lấy pháp tuyến (normal) của tường
            if (Physics.Raycast(transform.position - currentFlightDirection * 1f, currentFlightDirection, out hit, 2f, wallLayer))
            {
                reflectDir = Vector3.Reflect(currentFlightDirection, hit.normal);
            }
            else
            {
                // Fallback nếu raycast trượt (hiếm)
                reflectDir = Vector3.Reflect(currentFlightDirection, Vector3.up);
            }

            // 3. Cập nhật hướng bay mới
            currentFlightDirection = reflectDir.normalized;

            // 4. Tăng tốc độ nếu cần (ricochet thường nhanh hơn)
            currentSpeed *= bounceSpeedMultiplier;

            // 5. TẠO CHUYỂN ĐỘNG "VÔ TẬN" MỚI
            // Thay vì bay đến targetBounce (cố định), ta bay đến một điểm RẤT XA theo hướng phản xạ.
            // Nếu gặp tường khác, OnTriggerEnter lại được gọi và quy trình này lặp lại.
            float farDistance = 50f; // Bay xa 50m (đủ để ra khỏi màn hình hoặc gặp tường khác)
            Vector3 infiniteTarget = transform.position + currentFlightDirection * farDistance;

            // Tính thời gian dựa trên tốc độ hiện tại
            float duration = farDistance / currentSpeed;

            moveTween = transform.DOMove(infiniteTarget, duration)
                .SetEase(Ease.Linear);
            // Không có OnComplete, nó sẽ bay cho đến khi đụng tường khác hoặc hết lifeTime
        }
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