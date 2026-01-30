using System.Collections.Generic;
using UnityEngine;

public class CameraFlow : MonoBehaviour
{
    private List<Transform> tfsFlow = new List<Transform>();

    [Header("Cài đặt")]
    public float rotationSpeed = 5f; // Tốc độ xoay của Camera
    Transform currentTarget;

    // Không cần smoothTime hay velocity nữa vì không di chuyển vị trí

    void LateUpdate()
    {

        if (currentTarget != null)
        {
            // 2. Tính toán hướng cần nhìn
            Vector3 direction = currentTarget.position - transform.position;

            // Nếu bạn muốn Camera chỉ xoay ngang (trục Y) mà không ngước lên ngước xuống (giống game Top-down), 
            // hãy bỏ comment dòng dưới:
            // direction.y = 0; 

            // 3. Tạo góc quay mục tiêu (LookRotation)
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);

                // 4. Xoay từ từ (Slerp) đến góc đó
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }

    // --- HÀM TÌM ĐỐI TƯỢNG GẦN NHẤT (Giữ nguyên logic của bạn) ---
    public Transform GetNearestTarget(Vector3 sourcePosition)
    {
        Transform nearest = null;
        float minDistanceSqr = Mathf.Infinity;

        // Duyệt ngược là đúng rồi (để xóa phần tử null an toàn)
        for (int i = tfsFlow.Count - 1; i >= 0; i--)
        {
            if (!tfsFlow[i].gameObject.activeInHierarchy) continue;
            Transform tf = tfsFlow[i];

            // Kiểm tra null
            if (tf == null)
            {
                tfsFlow.RemoveAt(i);
                continue;
            }
            // --- SỬA Ở ĐÂY ---
            // Lấy vector từ nguồn đến đích (bao gồm cả X, Y, Z)
            Vector3 directionToTarget = tf.position - sourcePosition;

            // KHÔNG ĐƯỢC set z = 0 hay y = 0 nếu muốn tính full 3D.
            // directionToTarget.z = 0; <--- Xóa dòng này đi

            // Tính bình phương độ dài vector (X^2 + Y^2 + Z^2)
            // Dùng sqrMagnitude nhanh hơn Vector3.Distance vì không cần căn bậc 2
            float dSqrToTarget = directionToTarget.sqrMagnitude;

            if (dSqrToTarget < minDistanceSqr)
            {
                minDistanceSqr = dSqrToTarget;
                nearest = tf;
            }
        }

        // Debug.Log(nearest); // Comment lại khi build để đỡ lag log
        return nearest;
    }

    // Hàm set vị trí thủ công (dùng khi setup màn chơi)
    public void SetPosCam(Vector3 pos)
    {
        transform.position = pos;
    }

    public void AddEnemyFlow(Transform tf)
    {
        if (!tfsFlow.Contains(tf)) tfsFlow.Add(tf);
        // 1. Tìm thằng gần Camera nhất
        currentTarget = GetNearestTarget(transform.position);
    }

    public void RemovEnemyFlow(Transform tf)
    {
        if (tfsFlow.Contains(tf)) tfsFlow.Remove(tf);
        // 1. Tìm thằng gần Camera nhất
        currentTarget = GetNearestTarget(transform.position);
    }

    public void Clear()
    {
        tfsFlow.Clear();
    }
}