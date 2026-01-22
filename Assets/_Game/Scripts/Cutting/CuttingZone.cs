using System.Collections.Generic;
using UnityEngine;

public class CuttingZone : MonoBehaviour
{
    [Header("Cài đặt")]
    [Tooltip("Vùng cắt này có giết chết Enemy ngay lập tức không?")]
    public bool killInstant = true;

    [Tooltip("Transform định nghĩa hướng cắt (Rotation). Vị trí sẽ tự động lấy theo đối tượng bị cắt.")]
    public Transform cutPlaneDefinition;

    // Đối tượng ảo dùng để xác định vị trí cắt ngay tâm
    private GameObject midPointSlicer;

    void Start()
    {
        // Tạo một object rỗng làm con của Zone để dùng làm "lưỡi dao ảo"
        midPointSlicer = new GameObject("Internal_MidPointSlicer");
        midPointSlicer.transform.SetParent(transform);
    }

    void OnCollisionEnter(Collision collision)
    {
        // 1. Kiểm tra Sliceable
        Sliceable sliceable = collision.collider.GetComponent<Sliceable>();

        if (sliceable == null)
        {
            sliceable = collision.collider.GetComponentInParent<Sliceable>();
        }

        // Kiểm tra điều kiện: Cắt được VÀ Phải là phần Hông (isHip)
        if (sliceable != null && sliceable.canBeCut && sliceable.isHip)
        {
            PerformZoneCut(sliceable);
        }
    }

    private void PerformZoneCut(Sliceable target)
    {
        // 2. SETUP MẶT PHẲNG CẮT NGAY TÂM

        // Bước A: Đưa lưỡi dao ảo đến đúng vị trí của đối tượng bị cắt (Điểm giữa)
        // Mẹo: Nếu bạn muốn chính xác tâm hình học (visual center) thay vì pivot, 
        // hãy dùng target.GetComponent<Collider>().bounds.center;
        midPointSlicer.transform.position = target.transform.position;

        // Bước B: Xoay lưỡi dao theo hướng mong muốn (từ cutPlaneDefinition hoặc chính CuttingZone)
        Transform rotReference = cutPlaneDefinition != null ? cutPlaneDefinition : transform;
        midPointSlicer.transform.rotation = rotReference.rotation;

        // Bước C: Đóng gói vào List
        List<Transform> cutterTransforms = new List<Transform>();
        cutterTransforms.Add(midPointSlicer.transform);

        // 3. Xử lý sát thương
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

        // 4. Kích hoạt cắt
        // Truyền midPointSlicer (đang nằm ngay tâm đối tượng) vào Observer
        Observer.OnCuttingMultipObject?.Invoke(cutterTransforms, target.transform);
    }
}