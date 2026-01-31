using UnityEngine;

public class LegMotor : MonoBehaviour
{
    [Header("Cài Đặt Di Chuyển")]
    public float moveSpeed = 5f;

    [Header("Tham Chiếu")]
    [Tooltip("Kéo Hông (Pelvis) vào đây để biết hướng 'Phía Trước' là đâu")]
    public Transform directionReference;

    private Rigidbody rb;
    private bool isMoving = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        ApplyVelocity();
    }

    public void SetMoving(bool moving) => isMoving = moving;


    void ApplyVelocity()
    {
        if (isMoving && directionReference != null)
        {
            // 1. Lấy hướng Z (Mũi tên xanh) của cái Hông
            Vector3 forwardDir = directionReference.forward;

            // 2. Ép hướng này nằm ngang mặt đất (loại bỏ độ dốc trục Y nếu có)
            forwardDir.y = 0;
            forwardDir.Normalize();

            // 3. Tính toán vận tốc theo hướng Z đó
            Vector3 targetVelocity = forwardDir * moveSpeed;

            // 4. Giữ nguyên vận tốc rơi tự do (Trục Y) của Rigidbody
            // Chỉ thay đổi vận tốc trên mặt phẳng (X và Z)
            rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);
        }
        else if (!isMoving)
        {
            // Dừng lại: Set X và Z về 0, giữ nguyên Y để trọng lực hoạt động
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void OnDrawGizmos()
    {
        // Vẽ tia debug để bạn thấy hướng di chuyển trong Scene
        if (directionReference != null)
        {
            Gizmos.color = Color.blue; // Màu xanh dương tượng trưng cho trục Z
            Gizmos.DrawLine(transform.position, transform.position + directionReference.forward * 2f);
        }
    }
}