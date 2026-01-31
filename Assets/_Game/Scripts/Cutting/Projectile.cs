using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;

public class Projectile : GameUnit
{
    [Header("Physics Settings")]
    public float impactForce = 10f;
    [Header("Movement Mode")]
    public bool flyStraight = false; // Biến này sẽ được set lại khi gọi hàm Initialize

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

    // Coroutine giữ nguyên của bạn
    IEnumerator FlyRoutine()
    {
        while (isMoving && !hasHit)
        {
            Vector3 displacement = transform.position - lastPosition;
            if (displacement.sqrMagnitude > 0.0001f)
            {
                currentFlightDirection = displacement.normalized;
            }

            float spinSpeed = 360f / rotateDuration;
            currentSpinAngleZ -= spinSpeed * Time.deltaTime;

            if (currentFlightDirection != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(currentFlightDirection);
                Quaternion modelCorrection = Quaternion.Euler(rotation + Vector3.right * initialAngleX);
                Quaternion spinRot = Quaternion.Euler(0, 0, currentSpinAngleZ);

                transform.rotation = lookRot * modelCorrection * spinRot;
            }

            lastPosition = transform.position;
            yield return null;
        }
    }

    // --- [HÀM ĐÃ SỬA] THÊM THAM SỐ bool isStraight ---
    public void InitializeArcThrow(Vector3 targetPosition, float speedMultiplier, float angleX, bool isStraight)
    {
        // 1. Cập nhật chế độ bay dựa trên tham số truyền vào
        this.flyStraight = isStraight;

        // Các phần dưới giữ nguyên y hệt code của bạn
        if (moveTween != null) moveTween.Kill();
        CancelInvoke(nameof(DespawnSelf));

        if (flightCoroutine != null) StopCoroutine(flightCoroutine);

        sliceables.Clear();
        hitObjects.Clear();

        isStick = false;
        hasHit = false;
        isMoving = true;
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

        // DI CHUYỂN (DOTween sẽ check biến flyStraight vừa được set ở trên)
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

        flightCoroutine = StartCoroutine(FlyRoutine());

        Invoke(nameof(DespawnSelf), lifeTime);
    }

    // --- CÁC HÀM DƯỚI GIỮ NGUYÊN KHÔNG ĐỤNG VÀO ---

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
                GameController.Instance.Vibrate();
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
        SoundManager.Instance.PlaySound(eAudioName.Audio_Stick);
        hasHit = true;
        isMoving = false;

        CancelInvoke(nameof(DespawnSelf));
        if (moveTween != null) moveTween.Kill();
        if (flightCoroutine != null) StopCoroutine(flightCoroutine);

        rb.isKinematic = true;
        col.isTrigger = true;
        isStick = true;

        targetParent = parent;
        transform.SetParent(targetParent);
    }

    public void DespawnSelf()
    {
        if (gameObject.activeSelf == false) return;
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