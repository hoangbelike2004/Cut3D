using UnityEngine;

public class GimbalLockTest : MonoBehaviour
{
    [Header("Đối tượng test")]
    public Transform eulerObject;      // Kéo Cube "Bad" vào đây
    public Transform quaternionObject; // Kéo Cube "Good" vào đây

    [Header("Cài đặt")]
    public float rotateSpeed = 50f;

    // Hướng dẫn hiển thị trên màn hình
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 200),
            "CÁCH TEST GIMBAL LOCK:\n" +
            "1. Nhấn mũi tên LÊN để ngóc đầu lên 90 độ.\n" +
            "2. Khi đã nhìn thẳng lên trời, giữ mũi tên TRÁI/PHẢI.\n" +
            "   -> Quan sát sự khác biệt!\n\n" +
            "Euler X: " + eulerObject.eulerAngles.x.ToString("F1") + "\n" +
            "Quat X: " + quaternionObject.eulerAngles.x.ToString("F1"));
    }

    void Update()
    {
        // Lấy input từ bàn phím (Mũi tên)
        float xInput = Input.GetAxis("Vertical") * rotateSpeed * Time.deltaTime;   // Lên / Xuống (Pitch)
        float yInput = Input.GetAxis("Horizontal") * rotateSpeed * Time.deltaTime; // Trái / Phải (Yaw)

        // --- CÁCH 1: DÙNG EULER (CÁCH SAI/DỄ LỖI) ---
        // Tư duy kiểu: "Lấy góc hiện tại -> Cộng thêm số -> Gán lại"
        // Đây là cách người mới hay làm và gây ra Gimbal Lock
        Vector3 currentEuler = eulerObject.eulerAngles;
        currentEuler.x += xInput; // Cộng góc X
        currentEuler.y += yInput; // Cộng góc Y
        eulerObject.eulerAngles = currentEuler;

        // --- CÁCH 2: DÙNG QUATERNION (CÁCH ĐÚNG) ---
        // Dùng hàm có sẵn của Unity (nó tính bằng Quaternion bên trong)
        // Rotate quanh trục của chính nó (Space.Self)
        quaternionObject.Rotate(Vector3.right * xInput, Space.Self);
        quaternionObject.Rotate(Vector3.up * yInput, Space.Self);
    }
}