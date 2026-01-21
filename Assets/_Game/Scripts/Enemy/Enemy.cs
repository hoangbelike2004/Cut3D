using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Trạng thái")]
    public bool isMoving = true; // <--- BIẾN MỚI: Bật/Tắt để cho phép đi hay không

    [Header("Cài đặt")]
    public Rigidbody leftLeg;
    public Rigidbody rightLeg;
    public Transform target;
    public Transform hip;

    public int hp;

    public float speed = 10f;
    public float stepRate = 0.3f;
    public float stopDistance = 1.0f; // Khoảng cách dừng lại

    private float timer;
    private bool moveLeft = true;

    void FixedUpdate()
    {
        // 1. KIỂM TRA ĐIỀU KIỆN BAN ĐẦU
        // Nếu biến isMoving bị tắt -> Dừng ngay lập tức
        if (!isMoving || target == null || hip == null) return;

        // 2. KIỂM TRA KHOẢNG CÁCH
        // Tính khoảng cách trên mặt phẳng (bỏ qua trục Y)
        float distance = Vector3.Distance(new Vector3(hip.position.x, 0, hip.position.z),
                                          new Vector3(target.position.x, 0, target.position.z));

        // Nếu đã đến nơi (khoảng cách nhỏ hơn mức cho phép) -> Dừng lại
        if (distance < stopDistance)
        {
            isMoving = false;
            return;
        }

        // 3. LOGIC BƯỚC ĐI
        Vector3 dir = (target.position - hip.position).normalized;
        dir.y = 0;

        timer += Time.fixedDeltaTime;

        // Chỉ tác động lực khi đến nhịp (tránh bay lên trời)
        if (timer > stepRate)
        {
            moveLeft = !moveLeft;
            timer = 0;

            Rigidbody activeLeg = moveLeft ? leftLeg : rightLeg;

            // Đá chân tới + Nhấc nhẹ lên
            Vector3 stepForce = (dir + Vector3.up * 0.5f) * speed;

            if (activeLeg != null)// Giật chân đi
                activeLeg.AddForce(stepForce, ForceMode.VelocityChange);
        }
    }

    public void Hit(int dame)
    {
        hp -= dame;
        if (hp < 0)
        {
            Debug.Log("Win");
        }
    }
}