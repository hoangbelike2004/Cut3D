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
    // public List<Transform> activePlanes;

    // // --- HÀM PUBLIC: GỌI TỪ BÊN NGOÀI ĐỂ CẮT ---
    // // SkillController hoặc MouseSlicer sẽ gọi hàm này và truyền danh sách dao vào
    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Space))
    //     {
    //         PerformSlice(activePlanes);
    //     }
    // }
    public void PerformSlice(List<Transform> activePlanes)
    {
        if (activePlanes == null || activePlanes.Count == 0) return;
        // Dùng plane đầu tiên làm mốc để quét va chạm
        Transform mainPlane = activePlanes[0];

        // Tạo vùng quét (Box) quanh lưỡi dao
        // Bạn có thể chỉnh size (2f, 2f, 2f) to nhỏ tùy vũ khí
        Collider[] hits = Physics.OverlapBox(mainPlane.position, new Vector3(2f, 2f, 2f), mainPlane.rotation, layerToCut);

        if (hits.Length == 0) return;

        foreach (Collider hit in hits)
        {
            GameObject target = hit.gameObject;

            // Kiểm tra xem vật này có cho phép cắt và có màu ruột riêng không
            Sliceable sliceData = target.GetComponent<Sliceable>();
            Material matToUse = defaultCrossSectionMaterial;

            if (sliceData != null)
            {
                if (!sliceData.canBeCut) continue; // Bỏ qua nếu không cho cắt
                if (sliceData.internalMaterial != null) matToUse = sliceData.internalMaterial;
            }

            // Bắt đầu quy trình cắt đa điểm
            ProcessMultiSlice(target, activePlanes, matToUse);
        }
    }

    // --- QUY TRÌNH XỬ LÝ ĐA ĐIỂM (TUẦN TỰ) ---
    private void ProcessMultiSlice(GameObject startingTarget, List<Transform> planes, Material mat)
    {
        // Danh sách các vật thể cần cắt (Ban đầu chỉ có 1)
        List<GameObject> targetsToSlice = new List<GameObject>();
        targetsToSlice.Add(startingTarget);

        // Duyệt qua từng lưỡi dao
        foreach (Transform plane in planes)
        {
            List<GameObject> nextBatchTargets = new List<GameObject>();

            foreach (GameObject target in targetsToSlice)
            {
                // Cắt target bằng plane hiện tại
                // Nếu cắt thành công, hàm SliceSingleTarget sẽ thêm 2 mảnh mới vào nextBatchTargets
                bool sliced = SliceSingleTarget(target, plane, nextBatchTargets, mat);

                // Nếu dao trượt (không cắt được), giữ lại vật đó cho vòng sau
                if (!sliced)
                {
                    nextBatchTargets.Add(target);
                }
            }
            // Cập nhật danh sách mục tiêu cho dao tiếp theo
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

        // Lưu 2 loại scale để fix lỗi phình to
        Vector3 originalLocalScale = target.transform.localScale; // Dùng cho mảnh dính lại
        Vector3 originalWorldScale = target.transform.lossyScale; // Dùng cho mảnh rơi ra

        // 2. THỰC HIỆN CẮT (EzySlice)
        SlicedHull hull = target.Slice(plane.position, plane.forward, mat);

        if (hull != null)
        {
            GameObject upperHull = hull.CreateUpperHull(target, mat);
            GameObject lowerHull = hull.CreateLowerHull(target, mat);

            // Setup vật lý cơ bản (Tạm dùng World Scale để tính toán Collider chuẩn)
            SetupHull(upperHull, originalPos, originalRot, originalWorldScale);
            SetupHull(lowerHull, originalPos, originalRot, originalWorldScale);

            // 3. XÁC ĐỊNH MẢNH GỐC (DÍNH) VÀ MẢNH RƠI (PIVOT RULE)
            GameObject rootPart, fallPart;
            DecideRootAndFall(target, plane, upperHull, lowerHull, out rootPart, out fallPart);

            // 4. XỬ LÝ MẢNH GỐC (Dính lại cha)
            if (originalParent != null)
            {
                rootPart.name = target.name;
                rootPart.transform.SetParent(originalParent, true); // Giữ nguyên vị trí thế giới

                // [QUAN TRỌNG] Trả lại Scale nội bộ (Local) để nó tương thích với cha
                rootPart.transform.localScale = originalLocalScale;
            }
            else
            {
                // Nếu không có cha, nó hoạt động như mảnh rơi
                rootPart.name = target.name;
                rootPart.transform.localScale = originalWorldScale;
            }

            // Copy lại component Sliceable sang mảnh gốc để có thể cắt tiếp
            CopySliceableConfig(target, rootPart);

            // 5. XỬ LÝ MẢNH RƠI (Tự do)
            fallPart.name = target.name + "_Broken";
            fallPart.transform.parent = null;

            // [QUAN TRỌNG] Gán Scale toàn cầu (World) vì nó không còn cha
            fallPart.transform.localScale = originalWorldScale;

            CopySliceableConfig(target, fallPart);

            // 6. CHUYỂN CON CÁI (Reparent Children)
            ReparentChildren(target, rootPart, fallPart, plane);

            // 7. Thêm vào kết quả trả về
            results.Add(rootPart);
            results.Add(fallPart);

            // Xóa vật cũ
            Destroy(target);
            return true; // Báo thành công
        }

        return false; // Báo thất bại
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

        // Set lại Layer để có thể bị cắt tiếp ở vòng sau
        if (layerToCut.value > 0)
        {
            int layerIndex = (int)Mathf.Log(layerToCut.value, 2);
            hull.layer = layerIndex;
        }
    }

    // --- HÀM PHỤ TRỢ: QUYẾT ĐỊNH GỐC/RƠI (PIVOT) ---
    private void DecideRootAndFall(GameObject target, Transform plane, GameObject upper, GameObject lower, out GameObject root, out GameObject fall)
    {
        // Tính xem Tâm (Pivot) của vật cũ nằm bên nào dao
        Vector3 pivotDirection = target.transform.position - plane.position;
        float pivotSide = Vector3.Dot(pivotDirection, plane.up);

        // [LOGIC ĐẢO NGƯỢC THEO YÊU CẦU CỦA BẠN]
        // Nếu Pivot nằm bên Dương -> Gán Lower làm Gốc (Ngược lại logic thông thường)
        // Bạn có thể đổi chỗ if/else nếu thấy nó bị sai với model của bạn
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

    // --- HÀM PHỤ TRỢ: CHUYỂN CON ---
    private void ReparentChildren(GameObject oldParent, GameObject rootPart, GameObject fallPart, Transform plane)
    {
        List<Transform> children = new List<Transform>();
        foreach (Transform child in oldParent.transform) children.Add(child);

        foreach (Transform child in children)
        {
            // Kiểm tra vị trí của Con so với Plane
            Vector3 childDir = child.position - plane.position;
            float childSide = Vector3.Dot(childDir, plane.up);

            // Kiểm tra vị trí của Mảnh Gốc so với Plane
            float rootSide = Vector3.Dot(rootPart.transform.position - plane.position, plane.up);

            // So sánh phe: Nếu cùng dấu (cùng âm hoặc cùng dương) -> Về cùng đội
            bool isChildSameSideAsRoot = (childSide >= 0) == (rootSide >= 0);

            if (isChildSameSideAsRoot)
            {
                child.SetParent(rootPart.transform, true);
            }
            else
            {
                child.SetParent(fallPart.transform, true);
            }
        }
    }

    // --- HÀM PHỤ TRỢ: COPY DỮ LIỆU SLICEABLE ---
    private void CopySliceableConfig(GameObject source, GameObject dest)
    {
        Sliceable sourceData = source.GetComponent<Sliceable>();
        if (sourceData != null)
        {
            Sliceable destData = dest.AddComponent<Sliceable>();
            destData.internalMaterial = sourceData.internalMaterial;
            destData.canBeCut = sourceData.canBeCut;
        }
    }

    void OnEnable()
    {
        Observer.OnCuttingMultipObject += PerformSlice;
    }

    void OnDisable()
    {
        Observer.OnCuttingMultipObject += PerformSlice;
    }
}