using UnityEngine;

[RequireComponent(typeof(LineRenderer))] // Tự động thêm LineRenderer nếu chưa có
public class PlayerController : MonoBehaviour
{
    [Header("Cài đặt Game")]
    [Tooltip("Khoảng cách kéo chuột (Pixel) để đạt Scale tối đa")]
    public float maxDragDistance = 400f;

    [Tooltip("Khoảng cách kéo tối thiểu. Nếu ngắn hơn mức này sẽ không ném.")]
    public float minDragDistance = 50f; // <--- MỚI: Ngưỡng kiểm tra

    [Header("Cài đặt Vũ Khí")]
    public float minScale = 0.5f;
    public float maxScale = 2.5f;

    [Header("Cài đặt Hiển thị (Line)")]
    public float distanceFromCamera = 5f; // Khoảng cách vẽ Line trước Camera
    public float lineWidth = 0.05f;

    // Biến lưu trạng thái chuột
    private Vector2 startMousePos;
    private Vector2 endMousePos;
    private bool isDragging = false;

    // Component
    private Camera mainCamera;
    private LineRenderer lineRenderer; // <--- MỚI: Biến LineRenderer

    void Start()
    {
        mainCamera = Camera.main;

        // Setup LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2; // Chỉ cần điểm đầu và cuối
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.enabled = false; // Mặc định tắt

        // Bạn nên gán một Material đơn giản (Unlit/Color) cho LineRenderer trong Inspector để nó hiện rõ màu
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

            // Bật LineRenderer
            lineRenderer.enabled = true;
            UpdateLineVisual(startMousePos, startMousePos); // Ban đầu là 1 điểm
        }

        // 2. ĐANG KÉO (Cập nhật Line liên tục)
        if (Input.GetMouseButton(0) && isDragging)
        {
            Vector2 currentMousePos = Input.mousePosition;
            UpdateLineVisual(startMousePos, currentMousePos);
        }

        // 3. THẢ CHUỘT -> NÉM
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            endMousePos = Input.mousePosition;
            isDragging = false;

            // Tắt LineRenderer ngay khi thả tay
            lineRenderer.enabled = false;

            // --- KIỂM TRA ĐIỀU KIỆN ---
            float dragDistance = Vector2.Distance(startMousePos, endMousePos);

            // Nếu kéo quá ngắn (gần nhau quá) -> Hủy bỏ, không ném
            if (dragDistance < minDragDistance)
            {
                Debug.Log("Lực kéo quá yếu (quá gần), hủy ném!");
                return;
            }

            ProcessThrowLogic(dragDistance); // Truyền khoảng cách đã tính vào luôn cho tối ưu
        }
    }

    // --- HÀM MỚI: CẬP NHẬT VẼ LINE ---
    void UpdateLineVisual(Vector2 startScreenPos, Vector2 endScreenPos)
    {
        // Chuyển đổi từ tọa độ màn hình (Screen) sang thế giới (World)
        // Cần thêm trục Z (distanceFromCamera) để nó hiện ra trước Camera
        Vector3 startWorld = mainCamera.ScreenToWorldPoint(new Vector3(startScreenPos.x, startScreenPos.y, distanceFromCamera));
        Vector3 endWorld = mainCamera.ScreenToWorldPoint(new Vector3(endScreenPos.x, endScreenPos.y, distanceFromCamera));

        lineRenderer.SetPosition(0, startWorld);
        lineRenderer.SetPosition(1, endWorld);
    }

    void ProcessThrowLogic(float dragDistance)
    {
        // --- BƯỚC 1: TÍNH TOÁN TỈ LỆ (SCALE) ---
        // (Đã tính dragDistance ở trên rồi nên dùng luôn)

        // Tính tỉ lệ lực dựa trên độ dài kéo (Clamp từ 0.1 đến 1.0)
        float powerRatio = Mathf.Clamp(dragDistance / maxDragDistance, 0.1f, 1f);

        // Tính Scale thực tế dựa trên tỉ lệ này
        float finalScale = Mathf.Lerp(minScale, maxScale, powerRatio);

        // --- BƯỚC 2: TÌM MỤC TIÊU (RAYCAST TỪ ĐIỂM GIỮA) ---
        Vector2 screenMidPoint = (startMousePos + endMousePos) / 2;
        Ray ray = mainCamera.ScreenPointToRay(screenMidPoint);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            // Trúng bất cứ cái gì có Collider là ném
            ThrowWeapon(hit.point, finalScale, powerRatio);
        }
        else
        {
            Debug.Log("Không trúng gì cả (bắn lên trời?)");
        }
    }

    void ThrowWeapon(Vector3 targetPosition, float scale, float powerRatio)
    {
        // --- BƯỚC 3: SPAWN VÀ NÉM ---
        // Đảm bảo bạn đã có SimplePool và PoolType trong project
        Projectile axe = SimplePool.Spawn<Projectile>(PoolType.Weapon_Axe, transform.position, Quaternion.identity);

        if (axe != null)
        {
            axe.transform.localScale = Vector3.one * scale;

            // 1. Tính Vector hướng
            Vector2 direction = startMousePos - endMousePos;

            // 2. Tính góc
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

            // 3. Đảo dấu
            float finalAngle = -angle;

            // Gọi hàm
            axe.InitializeArcThrow(targetPosition, powerRatio, finalAngle);
        }
    }
}