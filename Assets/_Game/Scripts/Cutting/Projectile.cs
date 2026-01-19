using UnityEngine;
using DG.Tweening; // <--- NHỚ THÊM DÒNG NÀY

public class Projectile : GameUnit
{
    [Header("Settings")]
    public float flySpeed = 20f;
    [Tooltip("Thời gian để xoay hết 1 vòng (giây). Càng nhỏ xoay càng nhanh.")]
    public float rotateDuration = 0.2f;

    public bool isStick = false;

    public Vector3 rotation;
    private bool hasHit = false;
    private bool isInitialized = false;

    private Rigidbody rb;
    private Collider col;

    // Biến lưu trữ Tween để quản lý (Tắt/Bật)
    private Tween spinTween;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    // --- BỎ HẲN HÀM UPDATE ĐI ---
    // Vì DOTween tự chạy ngầm, không cần Update nữa

    public void InitializeArcThrow(Vector3 targetPosition, float speedMultiplier, float angleX)
    {
        transform.localRotation = Quaternion.Euler(rotation + Vector3.right * angleX);
        isInitialized = true;
        hasHit = false;
        isStick = false;

        // 1. Setup Vật lý
        rb.isKinematic = false;
        rb.useGravity = true;
        col.enabled = true;
        col.isTrigger = false;

        // 2. Tính toán ném (Code cũ giữ nguyên)
        float distance = Vector3.Distance(transform.position, targetPosition);
        float speed = flySpeed * Mathf.Max(speedMultiplier, 0.1f);
        float flightTime = distance / speed;

        Vector3 startPos = transform.position;
        Vector3 gravity = Physics.gravity;
        Vector3 initialVelocity = (targetPosition - startPos - 0.5f * gravity * flightTime * flightTime) / flightTime;

        rb.linearVelocity = initialVelocity;

        // 3. --- KÍCH HOẠT XOAY BẰNG DOTWEEN ---
        StartSpinning();
    }

    void StartSpinning()
    {
        // Trước khi tạo tween mới, hãy chắc chắn kill tween cũ nếu có (để tránh lỗi khi Pool)
        StopSpinning();

        // Giải thích lệnh:
        // .DOLocalRotate: Xoay theo trục Z (-360 là xoay ngược kim đồng hồ)
        // .SetEase(Ease.Linear): Xoay đều, không nhanh dần chậm dần
        // .SetLoops(-1, LoopType.Incremental): Lặp vô tận (-1), kiểu cộng dồn góc xoay
        spinTween = transform
            .DOLocalRotate(new Vector3(0, 0, -360), rotateDuration, RotateMode.FastBeyond360)
            .SetRelative(true)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental);
    }

    void StopSpinning()
    {
        // Hủy tween ngay lập tức
        if (spinTween != null)
        {
            spinTween.Kill();
            spinTween = null;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (hasHit) return;

        if (collision.collider.CompareTag("Enemy"))
        {
            Sliceable sliceable = collision.collider.GetComponent<Sliceable>();
            if (sliceable != null)
            {
                hasHit = true;

                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;

                // --- DỪNG XOAY KHI TRÚNG ---
                StopSpinning();

                sliceable.AddProjectiles(this);
            }
        }
    }

    public void StickProjectile(Transform parent)
    {
        // Đảm bảo dừng xoay lần nữa cho chắc
        StopSpinning();

        rb.isKinematic = true;
        col.isTrigger = true;
        isStick = true;
        transform.SetParent(parent, true);
    }

    // Nếu dùng Pool, khi object bị Disable (thu hồi) cũng phải tắt Tween
    void OnDisable()
    {
        StopSpinning();
    }
}