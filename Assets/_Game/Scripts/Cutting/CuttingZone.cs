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

    private int cutCountMax = 3, cutCount = 0;

    void Start()
    {
        // Tạo một object rỗng làm con của Zone để dùng làm "lưỡi dao ảo"
        midPointSlicer = new GameObject("Internal_MidPointSlicer");
        midPointSlicer.transform.SetParent(transform);
    }

    void OnCollisionEnter(Collision collision)
    {
        // 1. Cố gắng tìm script Enemy từ vật thể bị va chạm
        // GetComponentInParent sẽ tìm từ vật thể đó ngược lên các cha của nó
        if (cutCount >= cutCountMax) return;
        Enemy enemy = collision.collider.GetComponentInParent<Enemy>();

        if (enemy != null)
        {
            // 2. Nếu trúng kẻ địch, xin nó cái Hông (Pelvis)
            GameObject hipObj = enemy.GetHipObject();

            if (hipObj != null)
            {
                // 3. Lấy component Sliceable từ cái Hông đó
                Sliceable bodySliceable = hipObj.GetComponent<Sliceable>();

                // 4. Kiểm tra và thực hiện cắt vào phần thân
                if (bodySliceable != null && bodySliceable.canBeCut)
                {
                    // Dù va chạm vào tay/chân, ta vẫn truyền cái Hông vào để xử lý
                    PerformZoneCut(bodySliceable);
                }
            }
        }
        else
        {
            // (Tùy chọn) Xử lý logic cũ nếu bắn trúng đồ vật không phải Enemy
            Sliceable objSliceable = collision.collider.GetComponent<Sliceable>();
            if (objSliceable != null && objSliceable.canBeCut)
            {
                PerformZoneCut(objSliceable);
            }
        }
    }

    private void PerformZoneCut(Sliceable target)
    {
        cutCount++;
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