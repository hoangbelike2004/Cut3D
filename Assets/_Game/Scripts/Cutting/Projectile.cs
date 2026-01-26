using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections; // Cần thêm cái này để dùng Coroutine

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

    // --- TRẠNG THÁI ---
    public bool isStick = false;
    public Vector3 rotation;

    private bool hasHit = false;
    private bool isMoving = false;

    private Rigidbody rb;
    private Collider col;
    private Tween moveTween;

    private Vector3 currentFlightDirection;
    private Vector3 lastPosition;
    private float currentSpeed;
    private float currentSpinAngleZ = 0f;
    private float initialAngleX;

    private List<Sliceable> sliceables = new List<Sliceable>();
    private List<ObjectSliceable> hitObjects = new List<ObjectSliceable>();

    private Vector3 savedScale;
    private Transform targetParent;

    // Biến lưu tham chiếu Coroutine để dừng khi cần
    private Coroutine flightCoroutine;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        savedScale = transform.localScale;
    }

    // [ĐÃ SỬA] Xóa Update đi, chỉ để lại check cha chết (nếu cần)
    // Hoặc nếu muốn tối ưu hơn, chuyển check cha chết vào Coroutine riêng
    void Update()
    {
        // Chỉ kiểm tra khi đang stick
        if (isStick && targetParent == null)
        {
            DespawnSelf();
        }
    }

    // [MỚI] Coroutine thay thế cho Update
    IEnumerator FlyRoutine()
    {
        while (isMoving && !hasHit)
        {
            // A. Tính toán hướng bay
            Vector3 displacement = transform.position - lastPosition;
            if (displacement.sqrMagnitude > 0.0001f)
            {
                currentFlightDirection = displacement.normalized;
            }

            // B. Tính góc xoay
            float spinSpeed = 360f / rotateDuration;
            currentSpinAngleZ -= spinSpeed * Time.deltaTime;

            // C. Áp dụng xoay
            if (currentFlightDirection != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(currentFlightDirection);
                Quaternion modelCorrection = Quaternion.Euler(rotation + Vector3.right * initialAngleX);
                Quaternion spinRot = Quaternion.Euler(0, 0, currentSpinAngleZ);

                transform.rotation = lookRot * modelCorrection * spinRot;
            }

            lastPosition = transform.position;

            // Đợi đến frame tiếp theo
            yield return null;
        }
    }

    public void InitializeArcThrow(Vector3 targetPosition, float speedMultiplier, float angleX)
    {
        if (moveTween != null) moveTween.Kill();
        CancelInvoke(nameof(DespawnSelf));

        // Dừng coroutine cũ nếu có
        if (flightCoroutine != null) StopCoroutine(flightCoroutine);

        sliceables.Clear();
        hitObjects.Clear();

        isStick = false;
        hasHit = false;
        isMoving = true; // Bật cờ chạy
        lastPosition = transform.position;
        targetParent = null;

        currentSpinAngleZ = 0f;
        initialAngleX = angleX;

        col.enabled = true;
        col.isTrigger = true;
        rb.isKinematic = true;
        rb.useGravity = false;

        transform.SetParent(null);

        Vector3 startPos = transform.position;
        currentSpeed = flySpeed * Mathf.Max(speedMultiplier, 0.5f);
        Vector3 dir = (targetPosition - startPos).normalized;
        currentFlightDirection = dir;

        // DI CHUYỂN (DOTween vẫn lo việc thay đổi position)
        if (flyStraight)
        {
            Vector3 overshootPoint = targetPosition + dir * 20f;
            float duration = Vector3.Distance(startPos, overshootPoint) / currentSpeed;
            moveTween = transform.DOMove(overshootPoint, duration).SetEase(Ease.Linear);
        }
        else
        {
            Vector3 midPoint = (startPos + targetPosition) / 2;
            Vector3 curveOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(0.2f, 1f), 0).normalized * (Vector3.Distance(startPos, targetPosition) * 0.3f);
            Vector3 controlPoint = midPoint + curveOffset;
            Vector3 endDir = (targetPosition - controlPoint).normalized;
            Vector3 overshootPoint = targetPosition + endDir * 20f;

            Vector3[] path = new Vector3[] { controlPoint, targetPosition, overshootPoint };
            float pathLen = Vector3.Distance(startPos, controlPoint) + Vector3.Distance(controlPoint, targetPosition) + Vector3.Distance(targetPosition, overshootPoint);
            moveTween = transform.DOPath(path, pathLen / currentSpeed, PathType.CatmullRom).SetEase(Ease.Linear);
        }

        // [MỚI] Bắt đầu Coroutine xoay và tính hướng
        flightCoroutine = StartCoroutine(FlyRoutine());

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

        float maxScale = Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
        float backTrackDistance = maxScale * 2f + 2f;
        RaycastHit hit;
        Vector3 reflectDir;

        if (Physics.Raycast(transform.position - currentFlightDirection * backTrackDistance, currentFlightDirection, out hit, backTrackDistance + 5f, wallLayer))
        {
            reflectDir = Vector3.Reflect(currentFlightDirection, hit.normal);
            transform.position = hit.point + hit.normal * (maxScale * 0.5f);
        }
        else
        {
            reflectDir = -currentFlightDirection;
        }

        currentFlightDirection = reflectDir.normalized;
        currentSpeed *= bounceSpeedMultiplier;

        float farDistance = 50f;
        Vector3 infiniteTarget = transform.position + currentFlightDirection * farDistance;
        float duration = farDistance / currentSpeed;

        moveTween = transform.DOMove(infiniteTarget, duration).SetEase(Ease.Linear);
    }

    public void StickProjectile(Transform parent)
    {
        hasHit = true;
        isMoving = false; // [QUAN TRỌNG] Đặt false để vòng lặp while trong Coroutine tự thoát

        CancelInvoke(nameof(DespawnSelf));
        if (moveTween != null) moveTween.Kill();

        // Dừng thủ công cho chắc chắn (dù vòng lặp while cũng sẽ tự dừng)
        if (flightCoroutine != null) StopCoroutine(flightCoroutine);

        rb.isKinematic = true;
        col.isTrigger = true;
        isStick = true;

        targetParent = parent;
        transform.SetParent(targetParent);
    }

    public void DespawnSelf()
    {
        isMoving = false;
        isStick = false;
        hasHit = false;
        targetParent = null;

        CancelInvoke(nameof(DespawnSelf));
        if (moveTween != null) moveTween.Kill();
        if (flightCoroutine != null) StopCoroutine(flightCoroutine);

        transform.SetParent(null);
        SimplePool.Despawn(this);
    }

    void OnEnable()
    {
        Observer.OnDespawnObject += DespawnSelf;
    }

    void OnDisable()
    {
        Observer.OnDespawnObject -= DespawnSelf;
    }
}