using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("Cài đặt Game")]
    [Tooltip("Khoảng cách kéo chuột (Pixel) để đạt Scale tối đa")]
    public float maxDragDistance = 400f;

    [Tooltip("Khoảng cách kéo tối thiểu. Nếu ngắn hơn mức này sẽ không ném.")]
    public float minDragDistance = 10f;

    [Tooltip("Chế độ ném thẳng hay cong")]
    public bool isStraight = false;
    [Header("Cài đặt Vũ Khí")]
    private float minScale = 0.1f;
    private float maxScale = 1.25f;

    [Header("Cài đặt Hiển thị (Line)")]
    public float distanceFromCamera = 5f;
    public float lineWidth = 0.025f;

    // Biến lưu trạng thái chuột
    private Vector2 startMousePos;
    private Vector2 endMousePos;
    private bool isDragging = false;

    private PoolType weapontype;
    // Component
    private Camera mainCamera;
    private LineRenderer lineRenderer;

    private Level _level;

    void Start()
    {
        weapontype = WeaponManager.Instance.WeaponSellect.Type;
        mainCamera = Camera.main;
        _level = transform.root.GetComponent<Level>();
        _level.SetPlayer(this);

        // Setup LineRenderer
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.enabled = false;
    }

    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (GameController.Instance.State != eGameState.Playing || GameController.Instance.isStop)
            return;

        // 1. BẮT ĐẦU KÉO
        if (Input.GetMouseButtonDown(0))
        {
            startMousePos = Input.mousePosition;
            isDragging = true;

            lineRenderer.enabled = true;
            UpdateLineVisual(startMousePos, startMousePos);
        }

        // 2. ĐANG KÉO
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
            lineRenderer.enabled = false;

            // Kiểm tra lực kéo tối thiểu
            float dragDistance = Vector2.Distance(startMousePos, endMousePos);
            if (dragDistance < minDragDistance)
            {
                return;
            }

            ProcessThrowLogic(dragDistance);
        }
    }

    void UpdateLineVisual(Vector2 startScreenPos, Vector2 endScreenPos)
    {
        Vector3 startWorld = mainCamera.ScreenToWorldPoint(new Vector3(startScreenPos.x, startScreenPos.y, distanceFromCamera));
        Vector3 endWorld = mainCamera.ScreenToWorldPoint(new Vector3(endScreenPos.x, endScreenPos.y, distanceFromCamera));

        lineRenderer.SetPosition(0, startWorld);
        lineRenderer.SetPosition(1, endWorld);
    }

    // --- ĐÂY LÀ HÀM ĐÃ ĐƯỢC CHỈNH SỬA ---
    void ProcessThrowLogic(float dragDistance)
    {
        // 1. TÍNH SCALE
        float powerRatio = Mathf.Clamp(dragDistance / maxDragDistance, 0.1f, 1f);
        float finalScale = Mathf.Lerp(minScale, maxScale, powerRatio);

        // 2. TÌM MỤC TIÊU
        Vector2 screenMidPoint = (startMousePos + endMousePos) / 2;
        Ray ray = mainCamera.ScreenPointToRay(screenMidPoint);
        RaycastHit hit;

        Vector3 targetPos; // Biến lưu vị trí đích cuối cùng

        // Thử Raycast xem có trúng vật thể nào không (ví dụ mặt đất, kẻ địch, tường)
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            // NẾU TRÚNG: Lấy điểm va chạm
            targetPos = hit.point;
        }
        else
        {
            // NẾU KHÔNG TRÚNG (Bắn lên trời/ra ngoài map):
            // Lấy một điểm nằm trên tia Ray, cách Camera 1000 đơn vị về phía trước
            targetPos = ray.GetPoint(1000f);
        }

        // 3. THỰC HIỆN NÉM (Dù trúng hay trượt đều ném)
        ThrowWeapon(targetPos, finalScale, powerRatio);
    }

    void ThrowWeapon(Vector3 targetPosition, float scale, float powerRatio)
    {
        Projectile axe = SimplePool.Spawn<Projectile>(weapontype, transform.position, Quaternion.identity);

        if (axe != null)
        {
            SoundManager.Instance.PlaySound(eAudioName.Audio_Throw);
            axe.transform.localScale = Vector3.one * scale;

            // Tính hướng ném (hướng của đường kẻ trên màn hình)
            Vector2 direction = startMousePos - endMousePos;

            // Tính góc xoay cho vũ khí (để nó quay đúng hướng bay)
            float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            float finalAngle = -angle;

            axe.InitializeArcThrow(targetPosition, powerRatio, finalAngle, isStraight);
        }
    }

    void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("Enemy") || other.collider.CompareTag("Bullet"))
        {
            if (GameController.Instance != null) GameController.Instance.SetState(eGameState.GameOver);
        }
    }

    public void ChangeWeaponType(WeaponData weaponData)
    {
        PoolType type = weaponData.Type;
        if (type != weapontype)
        {
            weapontype = type;
        }
    }

    void OnEnable()
    {
        Observer.OnSellectWeapon += ChangeWeaponType;
    }

    void OnDisable()
    {
        Observer.OnSellectWeapon -= ChangeWeaponType;
    }
}