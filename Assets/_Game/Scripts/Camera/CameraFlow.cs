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

        for (int i = tfsFlow.Count - 1; i >= 0; i--)
        {
            Transform tf = tfsFlow[i];

            if (tf == null)
            {
                tfsFlow.RemoveAt(i);
                continue;
            }

            Vector3 directionToTarget = tf.position - sourcePosition;
            directionToTarget.z = 0; // Tính khoảng cách trên mặt phẳng 2D

            float dSqrToTarget = directionToTarget.sqrMagnitude;

            if (dSqrToTarget < minDistanceSqr)
            {
                minDistanceSqr = dSqrToTarget;
                nearest = tf;
            }
        }
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
}