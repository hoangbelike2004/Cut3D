using UnityEngine;
using System.Collections.Generic;
using EzySlice;
using System.Reflection;

public class Cutting : MonoBehaviour
{
    [Header("--- CẤU HÌNH ---")]
    public LayerMask layerToCut;
    public Material defaultCrossSectionMaterial;
    public float explosionForce = 100f;

    // --- HÀM GỌI TỪ BÊN NGOÀI ---
    public void PerformSlice(List<Transform> activePlanes, Transform objectCut)
    {
        if (activePlanes == null || activePlanes.Count == 0 || objectCut == null) return;
        Sliceable sliceData = objectCut.GetComponent<Sliceable>();
        if (sliceData != null && !sliceData.canBeCut) return;
        Material matToUse = (sliceData != null && sliceData.internalMaterial != null) ? sliceData.internalMaterial : defaultCrossSectionMaterial;
        ProcessMultiSlice(objectCut.gameObject, activePlanes, matToUse);
    }

    private void ProcessMultiSlice(GameObject startingTarget, List<Transform> planes, Material mat)
    {
        List<GameObject> targetsToSlice = new List<GameObject> { startingTarget };
        foreach (Transform plane in planes)
        {
            List<GameObject> nextBatchTargets = new List<GameObject>();
            foreach (GameObject target in targetsToSlice)
            {
                if (!SliceSingleTarget(target, plane, nextBatchTargets, mat))
                    nextBatchTargets.Add(target);
            }
            targetsToSlice = nextBatchTargets;
        }
    }

    private bool SliceSingleTarget(GameObject target, Transform plane, List<GameObject> results, Material mat)
    {
        Transform originalParent = target.transform.parent;
        Vector3 originalPos = target.transform.position;
        Quaternion originalRot = target.transform.rotation;
        Vector3 originalLocalScale = target.transform.localScale;
        Vector3 originalWorldScale = target.transform.lossyScale;

        Rigidbody originalRb = target.GetComponent<Rigidbody>();
        ConfigurableJoint originalJoint = target.GetComponent<ConfigurableJoint>();
        bool isRagdoll = originalJoint != null || target.GetComponentInChildren<ConfigurableJoint>() != null;

        Vector3 cutPosition = plane.position;
        Collider targetCol = target.GetComponent<Collider>();
        if (targetCol != null) cutPosition = targetCol.ClosestPoint(plane.position);
        else cutPosition = target.transform.position;

        SlicedHull hull = target.Slice(cutPosition, plane.forward, mat);

        if (hull != null)
        {
            GameObject upperHull = hull.CreateUpperHull(target, mat);
            GameObject lowerHull = hull.CreateLowerHull(target, mat);

            GameObject rootPart, fallPart;
            DecideRootAndFall(target, plane, upperHull, lowerHull, out rootPart, out fallPart);

            // Setup Vật lý (Dùng cú pháp Unity 6)
            SetupHull(rootPart, originalPos, originalRot, originalWorldScale, false, originalRb);
            SetupHull(fallPart, originalPos, originalRot, originalWorldScale, true, originalRb);

            // Reparent RootPart
            if (originalParent != null)
            {
                rootPart.name = target.name;
                rootPart.transform.SetParent(originalParent, true);
                rootPart.transform.localScale = originalLocalScale;
            }
            else
            {
                rootPart.name = target.name;
                rootPart.transform.localScale = originalWorldScale;
            }
            CopySliceableConfig(target, rootPart);

            // Setup FallPart
            fallPart.name = target.name + "_Broken";
            fallPart.transform.parent = null;
            fallPart.transform.localScale = originalWorldScale;
            CopySliceableConfig(target, fallPart);

            ReparentChildren(target, upperHull, lowerHull, plane.forward, cutPosition);

            // XỬ LÝ RAGDOLL
            if (isRagdoll)
            {
                CopyAllComponents(target, rootPart);

                if (originalParent != null && originalJoint != null)
                {
                    ConnectRootToParent(rootPart, originalParent, originalJoint);
                }

                ConnectChildrenToHull(fallPart);
                ConnectChildrenToHull(rootPart);
            }

            results.Add(rootPart);
            results.Add(fallPart);
            Destroy(target);
            return true;
        }
        return false;
    }

    // --- SETUP RIGIDBODY THEO CHUẨN UNITY 6 ---
    private void SetupHull(GameObject hull, Vector3 pos, Quaternion rot, Vector3 scale, bool isAddforce, Rigidbody originalRb)
    {
        hull.transform.position = pos;
        hull.transform.rotation = rot;
        hull.transform.localScale = scale;

        if (hull.GetComponent<Collider>() == null) hull.AddComponent<BoxCollider>();

        Rigidbody rb = hull.GetComponent<Rigidbody>();
        if (rb == null) rb = hull.AddComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // [CẬP NHẬT UNITY 6] Sử dụng linearVelocity và linearDamping
        if (originalRb != null)
        {
            rb.mass = originalRb.mass;
            rb.useGravity = originalRb.useGravity;

            // Unity 6: drag -> linearDamping
            rb.linearDamping = originalRb.linearDamping;
            // Unity 6: angularDrag -> angularDamping
            rb.angularDamping = originalRb.angularDamping;
        }

        if (isAddforce)
        {
            rb.AddExplosionForce(explosionForce, pos, 1f);
        }
        else
        {
            if (originalRb != null)
            {
                // Unity 6: velocity -> linearVelocity
                rb.linearVelocity = originalRb.linearVelocity;
                rb.angularVelocity = originalRb.angularVelocity;
            }
        }

        if (layerToCut.value > 0)
        {
            int layerIndex = 0; int layerValue = layerToCut.value;
            while (layerValue > 1) { layerValue >>= 1; layerIndex++; }
            hull.layer = layerIndex;
            hull.tag = "Enemy";
        }
    }

    // --- NỐI ROOT VÀO CHA CŨ ---
    private void ConnectRootToParent(GameObject rootPart, Transform parent, ConfigurableJoint originalJoint)
    {
        Rigidbody parentRb = parent.GetComponent<Rigidbody>();
        if (parentRb == null) return;

        ConfigurableJoint existing = rootPart.GetComponent<ConfigurableJoint>();
        if (existing != null) DestroyImmediate(existing);

        ConfigurableJoint newJoint = rootPart.AddComponent<ConfigurableJoint>();
        newJoint.connectedBody = parentRb;

        CopyJointProperties(originalJoint, newJoint);
    }

    // --- COPY JOINT (FULL THÔNG SỐ) ---
    private void CopyJointProperties(ConfigurableJoint source, ConfigurableJoint dest)
    {
        dest.autoConfigureConnectedAnchor = false;
        dest.anchor = source.anchor;
        dest.connectedAnchor = source.connectedAnchor;
        dest.axis = source.axis;
        dest.secondaryAxis = source.secondaryAxis;

        dest.xMotion = source.xMotion;
        dest.yMotion = source.yMotion;
        dest.zMotion = source.zMotion;
        dest.angularXMotion = source.angularXMotion;
        dest.angularYMotion = source.angularYMotion;
        dest.angularZMotion = source.angularZMotion;

        dest.linearLimit = source.linearLimit;
        dest.lowAngularXLimit = source.lowAngularXLimit;
        dest.highAngularXLimit = source.highAngularXLimit;
        dest.angularYLimit = source.angularYLimit;
        dest.angularZLimit = source.angularZLimit;

        dest.xDrive = source.xDrive;
        dest.yDrive = source.yDrive;
        dest.zDrive = source.zDrive;
        dest.angularXDrive = source.angularXDrive;
        dest.angularYZDrive = source.angularYZDrive;
        dest.slerpDrive = source.slerpDrive;

        // Projection giúp chống tuột khớp trong Unity 6
        dest.projectionMode = JointProjectionMode.PositionAndRotation;
        dest.projectionDistance = 0.01f;
        dest.projectionAngle = 1f;

        dest.configuredInWorldSpace = source.configuredInWorldSpace;
        dest.swapBodies = source.swapBodies;
        dest.enableCollision = source.enableCollision;
        dest.enablePreprocessing = source.enablePreprocessing;
        dest.massScale = source.massScale;
        dest.connectedMassScale = source.connectedMassScale;
    }

    // --- HELPER FUNCTIONS ---
    private void DecideRootAndFall(GameObject target, Transform plane, GameObject upper, GameObject lower, out GameObject root, out GameObject fall)
    {
        // 1. Xác định "Điểm Neo" (Anchor Point) - Điểm này nằm ở đâu thì đó là phần gốc (Root)
        Vector3 anchorPoint = target.transform.position; // Mặc định là tâm

        ConfigurableJoint joint = target.GetComponent<ConfigurableJoint>();
        if (joint != null)
        {
            // Nếu có Joint, điểm quan trọng nhất là vị trí khớp nối
            // Chuyển đổi Anchor từ Local sang World
            anchorPoint = target.transform.TransformPoint(joint.anchor);
        }
        else if (target.transform.parent != null)
        {
            // Nếu không có Joint nhưng có cha, điểm neo là vị trí của cha
            // (Phần nào nằm cùng phía với cha thì giữ lại)
            anchorPoint = target.transform.parent.position;
        }

        // 2. Tạo mặt phẳng toán học để kiểm tra
        // LƯU Ý QUAN TRỌNG: Trong hàm SliceSingleTarget bạn dùng 'plane.forward' để cắt
        // nên ở đây BẮT BUỘC phải dùng 'plane.forward' để tính toán (không dùng plane.up)
        UnityEngine.Plane slicePlane = new UnityEngine.Plane(plane.forward, plane.position);

        // 3. Kiểm tra xem Điểm Neo nằm ở bên nào (Upper hay Lower)
        // GetSide trả về true nếu điểm nằm ở phía dương của pháp tuyến (Upper Hull)
        bool anchorIsOnUpperSide = slicePlane.GetSide(anchorPoint);

        if (anchorIsOnUpperSide)
        {
            root = upper;
            fall = lower;
        }
        else
        {
            root = lower;
            fall = upper;
        }
    }

    private void ReparentChildren(GameObject originalTarget, GameObject upperHull, GameObject lowerHull, Vector3 planeNormal, Vector3 planePos)
    {
        UnityEngine.Plane slicePlane = new UnityEngine.Plane(planeNormal, planePos);
        for (int i = originalTarget.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = originalTarget.transform.GetChild(i);
            if (child.name.Contains("_Broken")) continue;
            Vector3 checkPos = child.position;
            Renderer childRend = child.GetComponent<Renderer>();
            if (childRend != null) checkPos = childRend.bounds.center;

            if (slicePlane.GetSide(checkPos)) child.SetParent(upperHull.transform, true);
            else child.SetParent(lowerHull.transform, true);
        }
    }

    private void ConnectChildrenToHull(GameObject hull)
    {
        Rigidbody hullRb = hull.GetComponent<Rigidbody>();
        foreach (Transform child in hull.transform)
        {
            ConfigurableJoint childJoint = child.GetComponent<ConfigurableJoint>();
            if (childJoint != null) childJoint.connectedBody = hullRb;
        }
    }

    private void CopyAllComponents(GameObject source, GameObject dest)
    {
        Component[] components = source.GetComponents<Component>();
        foreach (var comp in components)
        {
            System.Type type = comp.GetType();
            if (type == typeof(Transform) || type == typeof(MeshFilter) || type == typeof(MeshRenderer) ||
                type == typeof(Collider) || type == typeof(BoxCollider) || type == typeof(SphereCollider) || type == typeof(CapsuleCollider) ||
                type == typeof(Rigidbody) || type == typeof(ConfigurableJoint))
            {
                continue;
            }
            CopyComponent(comp, dest);
        }
    }

    private Component CopyComponent(Component original, GameObject destination)
    {
        System.Type type = original.GetType();
        Component copy = destination.GetComponent(type);
        if (copy == null) copy = destination.AddComponent(type);
        System.Reflection.FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (System.Reflection.FieldInfo field in fields) field.SetValue(copy, field.GetValue(original));
        return copy;
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

    void OnEnable() { Observer.OnCuttingMultipObject += PerformSlice; }
    void OnDisable() { Observer.OnCuttingMultipObject -= PerformSlice; }
}