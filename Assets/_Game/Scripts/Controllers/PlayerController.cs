using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // [Header("Cài đặt Ném")] 
    // public LayerMask targetLayer; // <-- ĐÃ BỎ CÁI NÀY VÌ BẠN MUỐN LAYER NÀO CŨNG ĐƯỢC

    [Header("Cài đặt Game")]
    [Tooltip("Khoảng cách kéo chuột (Pixel) để đạt Scale tối đa")]
    public float maxDragDistance = 400f;

    [Header("Cài đặt Vũ Khí")]
    public float minScale = 0.5f;
    public float maxScale = 2.5f;

    // Biến lưu trạng thái chuột
    private Vector2 startMousePos;
    private Vector2 endMousePos;
    private bool isDragging = false;

    // Cache Camera để tối ưu
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        // 1. BẮT ĐẦU KÉO
        if (Input.GetMouseButtonDown(0))
        {
            startMousePos = Input.mousePosition;
            isDragging = true;
        }

        // 2. THẢ CHUỘT -> NÉM
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            endMousePos = Input.mousePosition;
            isDragging = false;

            ProcessThrowLogic();
        }
    }

    void ProcessThrowLogic()
    {
        // --- BƯỚC 1: TÍNH TOÁN TỈ LỆ (SCALE) ---
        float dragDistance = Vector2.Distance(startMousePos, endMousePos);

        // Tính tỉ lệ lực dựa trên độ dài kéo (Clamp từ 0.1 đến 1.0)
        float powerRatio = Mathf.Clamp(dragDistance / maxDragDistance, 0.1f, 1f);

        // Tính Scale thực tế dựa trên tỉ lệ này
        float finalScale = Mathf.Lerp(minScale, maxScale, powerRatio);

        // --- BƯỚC 2: TÌM MỤC TIÊU (RAYCAST TỪ ĐIỂM GIỮA) ---
        Vector2 screenMidPoint = (startMousePos + endMousePos) / 2;
        Ray ray = mainCamera.ScreenPointToRay(screenMidPoint);
        RaycastHit hit;

        // --- SỬA Ở ĐÂY: Bỏ tham số layer đi ---
        // Physics.Raycast(ray, out hit, 1000f) => Sẽ bắn trúng mọi thứ (Everything)
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            // Trúng bất cứ cái gì có Collider là ném
            ThrowWeapon(hit.point, finalScale, powerRatio);
        }
        else
        {
            // Raycast bắn ra ngoài hư vô
            Debug.Log("Không trúng gì cả (bắn lên trời?)");
        }
    }

    void ThrowWeapon(Vector3 targetPosition, float scale, float powerRatio)
    {
        // --- BƯỚC 3: SPAWN VÀ NÉM ---
        Projectile axe = SimplePool.Spawn<Projectile>(PoolType.Weapon_Axe, transform.position, Quaternion.identity);

        if (axe != null)
        {
            // 1. Set kích thước
            axe.transform.localScale = Vector3.one * scale;

            // Lấy biến toàn cục startMousePos và endMousePos
            Vector2 direction = endMousePos - startMousePos;

            // --- SỬA LỖI NGƯỢC HƯỚNG ---
            // Thêm dấu trừ (-) phía trước để đảo chiều xoay
            float dragAngle = -Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Mẹo phụ: Nếu rìu mặc định đang đứng thẳng (Up), mà Atan2 tính theo hướng ngang (Right),
            // bạn có thể cần cộng/trừ thêm 90 độ nếu thấy nó bị vuông góc so với đường vẽ.
            // Ví dụ: float dragAngle = -Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90;

            // 2. Kích hoạt ném vòng cung
            axe.InitializeArcThrow(targetPosition, powerRatio, dragAngle);
        }
    }

    // --- DEBUG ---
    private void OnDrawGizmos()
    {
        if (isDragging && mainCamera != null)
        {
            Vector2 currentMousePos = Input.mousePosition;
            Vector2 mid = (startMousePos + currentMousePos) / 2;
            Ray ray = mainCamera.ScreenPointToRay(mid);

            Gizmos.color = Color.red;
            Gizmos.DrawRay(ray.origin, ray.direction * 50f);
        }
    }
}