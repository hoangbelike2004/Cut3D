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
            axe.transform.localScale = Vector3.one * scale;

            // 1. Tính Vector hướng
            Vector2 direction = startMousePos - endMousePos;

            // 2. Tính góc (Dùng Atan2 để phân biệt trái phải)
            // Mathf.Atan2(x, y) trả về góc so với trục dọc (trục Y)
            // *Lưu ý: Unity dùng (x, y) cho góc so với trục Y (hướng Bắc), 
            // khác với toán học thuần túy (y, x) là so với trục X.
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            // --- KẾT QUẢ BẠN SẼ NHẬN ĐƯỢC ---
            // Kéo thẳng lên: 0 độ
            // Kéo sang phải (90 độ): ~90 độ
            // Kéo sang trái (-90 độ): ~-90 độ
            // Kéo chéo phải-xuống: ~135 độ
            // Kéo chéo trái-xuống: ~-135 độ

            // 3. Đảo dấu (Nếu bạn thấy nó bị ngược chiều xoay)
            float finalAngle = -angle;
            // Gọi hàm
            axe.InitializeArcThrow(targetPosition, powerRatio, finalAngle);
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