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

    private Transform parent => transform.root;

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
        Sliceable sliceData = target.GetComponent<Sliceable>();
        bool isHead = sliceData != null && sliceData.isHead;

        // Lưu lại thông tin Enemy cũ trước khi cắt
        Enemy originalEnemy = target.GetComponentInParent<Enemy>();
        GameObject headObject = (originalEnemy != null) ? originalEnemy.GetHeadObject() : null;

        Transform originalParent = target.transform.parent;
        Vector3 originalPos = target.transform.position;
        Quaternion originalRot = target.transform.rotation;
        Vector3 originalLocalScale = target.transform.localScale;

        // [QUAN TRỌNG] Lấy kích thước toàn cục (World Scale) để dùng khi đưa ra ngoài
        Vector3 originalWorldScale = target.transform.lossyScale;

        Rigidbody originalRb = target.GetComponent<Rigidbody>();
        ConfigurableJoint originalJoint = target.GetComponent<ConfigurableJoint>();

        bool isRagdoll = originalJoint != null || target.GetComponentInChildren<ConfigurableJoint>() != null || isHead;

        Vector3 cutPosition = plane.position;
        Collider targetCol = target.GetComponent<Collider>();
        if (targetCol != null) cutPosition = targetCol.ClosestPoint(plane.position);
        else cutPosition = target.transform.position;

        // --- CẮT ---
        SlicedHull hull = target.Slice(cutPosition, plane.forward, mat);

        if (hull != null)
        {
            GameObject upperHull = hull.CreateUpperHull(target, mat);
            GameObject lowerHull = hull.CreateLowerHull(target, mat);

            // Báo cáo mất bộ phận
            if (originalEnemy != null) originalEnemy.RemovePart(target);

            GameObject rootPart, fallPart;
            DecideRootAndFall(target, plane, upperHull, lowerHull, out rootPart, out fallPart);

            SetupHull(rootPart, originalPos, originalRot, originalWorldScale, false, originalRb);
            SetupHull(fallPart, originalPos, originalRot, originalWorldScale, true, originalRb);

            // --- 1. XỬ LÝ PHẦN GỐC (ROOT PART - GIỮ NGUYÊN) ---
            if (originalParent != null)
            {
                rootPart.name = target.name + "1";
                rootPart.transform.SetParent(originalParent, true);
                rootPart.transform.localScale = originalLocalScale; // Vẫn ở trong cha cũ nên dùng LocalScale
            }
            else
            {
                rootPart.name = target.name + "1";
                rootPart.transform.localScale = originalWorldScale;
            }
            CopySliceableConfig(target, rootPart, true);

            // --- 2. XỬ LÝ PHẦN RƠI RA (FALL PART - ĐÃ SỬA LỖI BÉ) ---
            fallPart.name = target.name + "_Broken";

            // Đưa thẳng ra Root (hoặc Level) như ý bạn muốn
            fallPart.transform.SetParent(transform.root, true);

            // [FIX LỖI BÉ] Dùng WorldScale vì khi ra ngoài nó không còn hưởng Scale của Enemy nữa
            fallPart.transform.localScale = originalWorldScale;

            CopySliceableConfig(target, fallPart, false);

            // --- DI CHUYỂN CON CÁI ---
            ReparentChildren(target, upperHull, lowerHull, plane.forward, cutPosition);

            // [LOGIC TRAO SỰ SỐNG CHO ĐẦU]
            if (target.GetComponent<Enemy>() != null)
            {
                CopyComponent(target.GetComponent<Enemy>(), rootPart);
                CopyComponent(target.GetComponent<Enemy>(), fallPart);
            }

            if (headObject != null)
            {
                bool headInRoot = headObject.transform.IsChildOf(rootPart.transform);
                bool headInFall = headObject.transform.IsChildOf(fallPart.transform);

                if (headInRoot) DestroyEnemyScript(fallPart);
                else if (headInFall) DestroyEnemyScript(rootPart);
                else DestroyEnemyScript(fallPart);
            }
            else
            {
                DestroyEnemyScript(fallPart);
            }

            if (isRagdoll)
            {
                CopyAllComponents(target, rootPart);
                if (originalJoint != null) ReconnectJoint(rootPart, originalJoint);
                ConnectChildrenToHull(fallPart);
                ConnectChildrenToHull(rootPart);
            }

            results.Add(rootPart);
            results.Add(fallPart);
            target.gameObject.SetActive(false);
            return true;
        }
        return false;
    }

    // Hàm phụ trợ xóa script Enemy
    private void DestroyEnemyScript(GameObject obj)
    {
        Enemy e = obj.GetComponent<Enemy>();
        if (e != null) Destroy(e);
    }

    // --- CÁC HÀM PHỤ TRỢ KHÁC ---
    private void DecideRootAndFall(GameObject target, Transform plane, GameObject upper, GameObject lower, out GameObject root, out GameObject fall)
    {
        Vector3 checkPoint = target.transform.position;
        ConfigurableJoint joint = target.GetComponent<ConfigurableJoint>();

        if (joint != null && joint.connectedBody != null) checkPoint = joint.connectedBody.transform.position;
        else if (target.transform.parent != null) checkPoint = target.transform.parent.position;
        else if (joint != null) checkPoint = target.transform.TransformPoint(joint.anchor);

        UnityEngine.Plane slicePlane = new UnityEngine.Plane(plane.forward, plane.position);
        bool bodyIsOnUpperSide = slicePlane.GetSide(checkPoint);

        if (bodyIsOnUpperSide) { root = upper; fall = lower; }
        else { root = lower; fall = upper; }
    }

    private void ReconnectJoint(GameObject rootPart, ConfigurableJoint originalJoint)
    {
        Rigidbody connectedBody = originalJoint.connectedBody;
        ConfigurableJoint existing = rootPart.GetComponent<ConfigurableJoint>();
        if (existing != null) DestroyImmediate(existing);

        ConfigurableJoint newJoint = rootPart.AddComponent<ConfigurableJoint>();
        newJoint.connectedBody = connectedBody;
        CopyJointProperties(originalJoint, newJoint);
    }

    private void SetupHull(GameObject hull, Vector3 pos, Quaternion rot, Vector3 scale, bool isAddforce, Rigidbody originalRb)
    {
        hull.transform.position = pos;
        hull.transform.rotation = rot;
        hull.transform.localScale = scale;

        if (hull.GetComponent<Collider>() == null) hull.AddComponent<BoxCollider>();

        Rigidbody rb = hull.GetComponent<Rigidbody>();
        if (rb == null) rb = hull.AddComponent<Rigidbody>();

        // --- [ĐÃ SỬA] CÀI ĐẶT RIGIDBODY ---
        rb.interpolation = RigidbodyInterpolation.Interpolate; // Làm mượt chuyển động
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Tránh xuyên tường khi bay nhanh
        // ----------------------------------

        if (originalRb != null)
        {
            rb.mass = originalRb.mass;
            rb.useGravity = originalRb.useGravity;
            rb.linearDamping = originalRb.linearDamping;
            rb.angularDamping = originalRb.angularDamping;
        }

        if (isAddforce) rb.AddExplosionForce(explosionForce, pos, 1f);
        else if (originalRb != null)
        {
            rb.linearVelocity = originalRb.linearVelocity;
            rb.angularVelocity = originalRb.angularVelocity;
        }

        if (layerToCut.value > 0)
        {
            int layerIndex = 0; int layerValue = layerToCut.value;
            while (layerValue > 1) { layerValue >>= 1; layerIndex++; }
            hull.layer = layerIndex;
            hull.tag = "Enemy";
        }
    }

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
                type == typeof(Rigidbody) || type == typeof(ConfigurableJoint) ||
                type == typeof(Sliceable) ||
                type == typeof(Enemy))
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

    private void CopySliceableConfig(GameObject source, GameObject dest, bool isRoot)
    {
        Sliceable sourceData = source.GetComponent<Sliceable>();
        if (sourceData != null)
        {
            Sliceable destData = dest.AddComponent<Sliceable>();
            destData.internalMaterial = sourceData.internalMaterial;
            destData.canBeCut = sourceData.canBeCut;
            destData.isHead = sourceData.isHead;
            destData.SetParentOld(sourceData.GetParentOld);

            if (isRoot && sourceData.isHead)
            {
                if (sourceData.GetParent != null && sourceData.GetParent.objectCamFolow != null)
                {
                    Transform tf = sourceData.GetParent.objectCamFolow.transform;
                    tf.SetParent(dest.transform);
                    tf.localPosition = Vector3.zero;
                }
            }
        }
    }

    void OnEnable() { Observer.OnCuttingMultipObject += PerformSlice; }
    void OnDisable() { Observer.OnCuttingMultipObject -= PerformSlice; }
}