using UnityEngine;
using System.Collections.Generic;
using EzySlice; // Bắt buộc phải có thư viện này

public class Cutting : MonoBehaviour
{
    [Header("--- CẤU HÌNH MẶC ĐỊNH ---")]
    public LayerMask layerToCut;
    [Tooltip("Màu mặc định nếu vật thể không có script Sliceable")]
    public Material defaultCrossSectionMaterial;
    public float explosionForce = 100f;

    // --- SỬA ĐỔI CHÍNH Ở ĐÂY ---
    // Hàm nhận vào:
    // 1. activePlanes: Danh sách các lưỡi rừu/dao
    // 2. objectCut: Đối tượng cụ thể cần bị cắt (bạn truyền từ bên ngoài vào)
    public void PerformSlice(List<Transform> activePlanes, Transform objectCut)
    {
        Debug.Log(activePlanes.Count);
        // 1. Kiểm tra dữ liệu đầu vào
        if (activePlanes == null || activePlanes.Count == 0 || objectCut == null) return;

        // 2. Kiểm tra xem đối tượng có script Sliceable và cho phép cắt không
        Sliceable sliceData = objectCut.GetComponent<Sliceable>();
        if (sliceData != null && !sliceData.canBeCut) return;

        // 3. Chọn vật liệu lõi (Internal Material)
        Material matToUse = defaultCrossSectionMaterial;
        if (sliceData != null && sliceData.internalMaterial != null)
        {
            matToUse = sliceData.internalMaterial;
        }

        // 4. Thực hiện quy trình cắt ngay lập tức cho đối tượng này
        // Không cần vòng lặp OverlapBox hay HashSet nữa
        ProcessMultiSlice(objectCut.gameObject, activePlanes, matToUse);
    }

    // --- QUY TRÌNH XỬ LÝ ĐA ĐIỂM (TUẦN TỰ) ---
    private void ProcessMultiSlice(GameObject startingTarget, List<Transform> planes, Material mat)
    {
        List<GameObject> targetsToSlice = new List<GameObject>();
        targetsToSlice.Add(startingTarget);

        foreach (Transform plane in planes)
        {
            List<GameObject> nextBatchTargets = new List<GameObject>();

            foreach (GameObject target in targetsToSlice)
            {
                // Cắt target bằng plane hiện tại
                bool sliced = SliceSingleTarget(target, plane, nextBatchTargets, mat);

                if (!sliced)
                {
                    nextBatchTargets.Add(target);
                }
            }
            targetsToSlice = nextBatchTargets;
        }
    }

    // --- LOGIC CỐT LÕI: CẮT 1 VẬT BẰNG 1 DAO ---
    private bool SliceSingleTarget(GameObject target, Transform plane, List<GameObject> results, Material mat)
    {
        // 1. LƯU TRỮ THÔNG TIN CŨ
        Transform originalParent = target.transform.parent;
        Vector3 originalPos = target.transform.position;
        Quaternion originalRot = target.transform.rotation;

        Vector3 originalLocalScale = target.transform.localScale;
        Vector3 originalWorldScale = target.transform.lossyScale;

        // --- TÍNH TOÁN VỊ TRÍ CẮT CHUẨN XÁC ---
        // Thay vì dùng tâm cán rừu (plane.position), ta tìm điểm trên bề mặt object gần rừu nhất
        // Điều này giúp vết cắt luôn nằm đúng chỗ va chạm
        Vector3 cutPosition = plane.position;
        Collider targetCol = target.GetComponent<Collider>();
        if (targetCol != null)
        {
            cutPosition = targetCol.ClosestPoint(plane.position);
        }
        else
        {
            cutPosition = target.transform.position;
        }

        // 2. THỰC HIỆN CẮT (EzySlice)
        // Dùng cutPosition vừa tính được làm tâm cắt
        SlicedHull hull = target.Slice(cutPosition, plane.forward, mat);

        if (hull != null)
        {
            GameObject upperHull = hull.CreateUpperHull(target, mat);
            GameObject lowerHull = hull.CreateLowerHull(target, mat);

            // Setup vật lý cơ bản
            SetupHull(upperHull, originalPos, originalRot, originalWorldScale);
            SetupHull(lowerHull, originalPos, originalRot, originalWorldScale);

            // 3. XÁC ĐỊNH MẢNH GỐC VÀ MẢNH RƠI
            GameObject rootPart, fallPart;
            DecideRootAndFall(target, plane, upperHull, lowerHull, out rootPart, out fallPart);

            // 4. XỬ LÝ MẢNH GỐC (Dính lại cha)
            if (originalParent != null)
            {
                rootPart.name = target.name;
                rootPart.transform.SetParent(originalParent, true);
                rootPart.transform.localScale = originalLocalScale; // Trả lại Scale Local
            }
            else
            {
                rootPart.name = target.name;
                rootPart.transform.localScale = originalWorldScale;
            }
            CopySliceableConfig(target, rootPart);

            // 5. XỬ LÝ MẢNH RƠI (Tự do)
            fallPart.name = target.name + "_Broken";
            fallPart.transform.parent = null;
            fallPart.transform.localScale = originalWorldScale;
            CopySliceableConfig(target, fallPart);

            // 6. CHUYỂN CON CÁI (Reparent Children)
            // Truyền cutPosition (vị trí cắt thực tế) để tính toán con cái chuẩn xác
            ReparentChildren(target, upperHull, lowerHull, plane.forward, cutPosition);

            // 7. Hoàn tất
            results.Add(rootPart);
            results.Add(fallPart);

            Destroy(target);
            return true;
        }

        return false;
    }

    // --- HÀM PHỤ TRỢ: SETUP VẬT LÝ ---
    private void SetupHull(GameObject hull, Vector3 pos, Quaternion rot, Vector3 scale)
    {
        hull.transform.position = pos;
        hull.transform.rotation = rot;
        hull.transform.localScale = scale;

        hull.AddComponent<BoxCollider>();

        Rigidbody rb = hull.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.AddExplosionForce(explosionForce, pos, 1f);

        if (layerToCut.value > 0)
        {
            // Chuyển bitmask layer về index integer
            int layerIndex = 0;
            int layerValue = layerToCut.value;
            while (layerValue > 1)
            {
                layerValue >>= 1;
                layerIndex++;
            }

            hull.layer = layerIndex;
            hull.tag = "Enemy";
        }
    }

    // --- HÀM PHỤ TRỢ: QUYẾT ĐỊNH GỐC/RƠI ---
    private void DecideRootAndFall(GameObject target, Transform plane, GameObject upper, GameObject lower, out GameObject root, out GameObject fall)
    {
        Vector3 pivotDirection = target.transform.position - plane.position;
        float pivotSide = Vector3.Dot(pivotDirection, plane.up);

        if (pivotSide >= 0)
        {
            root = lower;
            fall = upper;
        }
        else
        {
            root = upper;
            fall = lower;
        }
    }

    // --- [SỬA ĐỔI] ReparentChildren dùng Plane toán học tại điểm cắt ---
    private void ReparentChildren(GameObject originalTarget, GameObject upperHull, GameObject lowerHull, Vector3 planeNormal, Vector3 planePos)
    {
        // Tạo mặt phẳng toán học tại đúng vị trí va chạm
        UnityEngine.Plane slicePlane = new UnityEngine.Plane(planeNormal, planePos);

        for (int i = originalTarget.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = originalTarget.transform.GetChild(i);

            if (child.name.Contains("_Broken")) continue;

            Vector3 checkPos = child.position;
            Renderer childRend = child.GetComponent<Renderer>();
            if (childRend != null)
            {
                checkPos = childRend.bounds.center;
            }

            if (slicePlane.GetSide(checkPos))
            {
                child.SetParent(upperHull.transform, true);
            }
            else
            {
                child.SetParent(lowerHull.transform, true);
            }
        }
    }

    private void CopySliceableConfig(GameObject source, GameObject dest)
    {
        Sliceable sourceData = source.GetComponent<Sliceable>();
        if (sourceData != null)
        {
            Sliceable destData = dest.AddComponent<Sliceable>();
            destData.internalMaterial = sourceData.internalMaterial;
            destData.canBeCut = sourceData.canBeCut;
            destData.currentHitCountMax = 0;
        }
    }
    void OnEnable()
    {
        Observer.OnCuttingMultipObject += PerformSlice; // Cần sửa delegate này
    }
    void OnDisable()
    {
        Observer.OnCuttingMultipObject -= PerformSlice;
    }
}