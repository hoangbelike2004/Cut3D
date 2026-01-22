using System.Collections.Generic;
using UnityEngine;

public class CuttingZone : MonoBehaviour
{
    [Header("Cài đặt")]
    [Tooltip("Vùng cắt này có giết chết Enemy ngay lập tức không?")]
    public bool killInstant = true;

    [Tooltip("Transform định nghĩa mặt phẳng cắt. Nếu để null sẽ dùng chính Transform của object này.")]
    public Transform cutPlaneDefinition;

    void OnCollisionEnter(Collision collision)
    {
        // 1. Kiểm tra xem đối tượng va chạm có phải là Sliceable không
        Sliceable sliceable = collision.collider.GetComponent<Sliceable>();

        // Nếu không tìm thấy trên đối tượng va chạm, thử tìm ở cha
        if (sliceable == null)
        {
            sliceable = collision.collider.GetComponentInParent<Sliceable>();
        }

        // [SỬA Ở ĐÂY] Thêm điều kiện && sliceable.isHip
        if (sliceable != null && sliceable.canBeCut && sliceable.isHip)
        {
            PerformZoneCut(sliceable);
        }
    }

    private void PerformZoneCut(Sliceable target)
    {
        // 2. Xác định mặt phẳng cắt
        Transform planeTransform = cutPlaneDefinition != null ? cutPlaneDefinition : transform;

        List<Transform> cutterTransforms = new List<Transform>();
        cutterTransforms.Add(planeTransform);

        // 3. Xử lý sát thương cho Enemy (Parent)
        if (target.GetParent != null)
        {
            if (killInstant)
            {
                target.GetParent.Hit(9999);
            }
            else
            {
                target.GetParent.Hit(target.damage);
            }
        }

        // 4. Kích hoạt sự kiện cắt
        Observer.OnCuttingMultipObject?.Invoke(cutterTransforms, target.transform);
    }
}