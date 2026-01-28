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

    [Header("--- VISUAL ---")]
    [Range(0f, 1f)] public float deadPartDarkenFactor = 0.2f;

    private Transform parent => transform.root;

    void OnEnable() { Observer.OnCuttingMultipObject += PerformSlice; }
    void OnDisable() { Observer.OnCuttingMultipObject -= PerformSlice; }

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

        Enemy originalEnemy = target.GetComponentInParent<Enemy>();
        GameObject headObject = FindHeadObject(target, originalEnemy);

        Transform originalParent = target.transform.parent;
        Vector3 originalPos = target.transform.position;
        Quaternion originalRot = target.transform.rotation;
        Vector3 originalWorldScale = target.transform.lossyScale;

        Rigidbody originalRb = target.GetComponent<Rigidbody>();
        ConfigurableJoint originalJoint = target.GetComponent<ConfigurableJoint>();

        UnityEngine.Plane slicePlane = new UnityEngine.Plane(plane.forward, plane.position);

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

            if (originalEnemy != null) originalEnemy.RemovePart(target);

            GameObject rootPart, fallPart;

            DecideRootAndFall(target, slicePlane, upperHull, lowerHull, headObject, out rootPart, out fallPart);

            SetupHull(rootPart, originalPos, originalRot, originalWorldScale, false, originalRb);
            SetupHull(fallPart, originalPos, originalRot, originalWorldScale, true, originalRb);

            // =========================================================================
            // [CẬP NHẬT] Xử lý Tag & Script ObjectSliceable
            // =========================================================================
            if (target.CompareTag("Object"))
            {
                rootPart.tag = "Object";
                fallPart.tag = "Object";
                ObjectSliceable originalComp = target.GetComponent<ObjectSliceable>();

                // --- Xử lý cho phần ROOT ---
                ObjectSliceable rootSlice = rootPart.GetComponent<ObjectSliceable>();
                if (rootSlice == null) rootSlice = rootPart.AddComponent<ObjectSliceable>();

                if (originalComp != null)
                {
                    rootSlice.SetOriginOld(originalComp);
                    // [MỚI] Copy biến changeColor sang mảnh mới
                    rootSlice.changeColor = originalComp.changeColor;
                }

                // --- Xử lý cho phần FALL ---
                ObjectSliceable fallSlice = fallPart.GetComponent<ObjectSliceable>();
                if (fallSlice == null) fallSlice = fallPart.AddComponent<ObjectSliceable>();

                if (originalComp != null)
                {
                    // [MỚI] Copy biến changeColor sang mảnh mới
                    fallSlice.changeColor = originalComp.changeColor;
                }
            }
            // =========================================================================

            // Xử lý Hierarchy
            bool isStillAttached = false;
            if (originalParent != null)
            {
                rootPart.name = target.name + "1";
                rootPart.transform.SetParent(originalParent, true);
                rootPart.transform.localScale = target.transform.localScale;
                isStillAttached = true;
            }
            else
            {
                rootPart.name = target.name + "1";
                rootPart.transform.localScale = originalWorldScale;
                isStillAttached = false;
            }

            CopySliceableConfig(target, rootPart, true);

            fallPart.name = target.name + "_Broken";
            fallPart.transform.SetParent(transform.root, true);
            fallPart.transform.localScale = originalWorldScale;
            CopySliceableConfig(target, fallPart, false);

            ReparentChildren(target, upperHull, lowerHull, slicePlane);

            if (target.GetComponent<Enemy>() != null)
            {
                CopyComponent(target.GetComponent<Enemy>(), rootPart);
                CopyComponent(target.GetComponent<Enemy>(), fallPart);
            }

            // =========================================================================
            // [LOGIC SỰ SỐNG]
            // =========================================================================

            bool rootIsDead = true;
            bool fallIsDead = true;

            if (headObject != null)
            {
                if (headObject.transform.IsChildOf(rootPart.transform))
                {
                    rootIsDead = false;
                    fallIsDead = true;
                }
                else if (headObject.transform.IsChildOf(fallPart.transform))
                {
                    fallIsDead = false;
                    rootIsDead = true;
                }
                else
                {
                    if (isStillAttached)
                    {
                        rootIsDead = false;
                        fallIsDead = true;
                    }
                    else
                    {
                        rootIsDead = true;
                        fallIsDead = true;
                    }
                }
            }
            else
            {
                rootIsDead = true;
                fallIsDead = true;
            }

            if (rootIsDead) ProcessDeadPart(rootPart);
            if (fallIsDead) ProcessDeadPart(fallPart);

            // =========================================================================

            bool isRagdoll = originalJoint != null || target.GetComponentInChildren<ConfigurableJoint>() != null || isHead;
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

    // --- CÁC HÀM PHỤ TRỢ ---

    private void ProcessDeadPart(GameObject part)
    {
        if (part == null) return;
        DestroyEnemyScript(part);
        CheckAndDespawnProjectiles(part);
        DarkenDeadPart(part);
        NotifySliceableDead(part);
    }

    // [ĐÃ SỬA] Hàm làm tối hỗ trợ cả Sliceable và ObjectSliceable cho TỪNG Renderer
    private void DarkenDeadPart(GameObject part)
    {
        if (part == null) return;

        Renderer[] renderers = part.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            // 1. Kiểm tra Sliceable (Enemy)
            Sliceable localSlice = r.GetComponentInParent<Sliceable>();
            if (localSlice != null && localSlice.changeColor == false) continue;

            // 2. Kiểm tra ObjectSliceable (Vật thể môi trường)
            ObjectSliceable localObjectSlice = r.GetComponentInParent<ObjectSliceable>();
            if (localObjectSlice != null && localObjectSlice.changeColor == false) continue;

            // --- Nếu không bị cấm thì mới đổi màu ---
            Material[] mats = r.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                Color currentColor = Color.white;
                if (m.HasProperty("_BaseColor")) currentColor = m.GetColor("_BaseColor");
                else if (m.HasProperty("_Color")) currentColor = m.GetColor("_Color");
                else currentColor = m.color;

                Color darkColor = currentColor * deadPartDarkenFactor;

                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", darkColor);
                if (m.HasProperty("_Color")) m.SetColor("_Color", darkColor);
                m.color = darkColor;
            }
            r.materials = mats;
        }
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
            destData.changeColor = sourceData.changeColor; // Copy cho Sliceable

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

    // --- CÁC HÀM KHÁC GIỮ NGUYÊN ---

    private GameObject FindHeadObject(GameObject target, Enemy enemyScript)
    {
        if (enemyScript != null)
        {
            var head = enemyScript.GetHeadObject();
            if (head != null) return head;
        }
        var slices = target.GetComponentsInChildren<Sliceable>(true);
        foreach (var s in slices) { if (s.isHead) return s.gameObject; }
        Transform[] allChildren = target.GetComponentsInChildren<Transform>(true);
        foreach (var t in allChildren) { if (t.name.ToUpper().Contains("HEAD")) return t.gameObject; }
        return null;
    }

    private void DecideRootAndFall(GameObject target, UnityEngine.Plane slicePlane, GameObject upper, GameObject lower, GameObject headObject, out GameObject root, out GameObject fall)
    {
        if (headObject != null)
        {
            bool headOnUpper = slicePlane.GetSide(headObject.transform.position);
            if (headOnUpper) { root = upper; fall = lower; }
            else { root = lower; fall = upper; }
            return;
        }
        Vector3 checkPoint = target.transform.position;
        ConfigurableJoint joint = target.GetComponent<ConfigurableJoint>();
        if (joint != null && joint.connectedBody != null) checkPoint = joint.connectedBody.transform.position;
        else if (target.transform.parent != null) checkPoint = target.transform.parent.position;
        else if (joint != null) checkPoint = target.transform.TransformPoint(joint.anchor);

        bool bodyIsOnUpperSide = slicePlane.GetSide(checkPoint);
        if (bodyIsOnUpperSide) { root = upper; fall = lower; }
        else { root = lower; fall = upper; }
    }

    private void CheckAndDespawnProjectiles(GameObject part)
    {
        if (part == null) return;
        Projectile[] attachedProjectiles = part.GetComponentsInChildren<Projectile>(true);
        foreach (Projectile proj in attachedProjectiles)
        {
            if (proj != null && proj.gameObject != null) { try { proj.DespawnSelf(); } catch { } }
        }
    }

    private void NotifySliceableDead(GameObject part)
    {
        if (part == null) return;
        Sliceable[] sliceables = part.GetComponentsInChildren<Sliceable>(true);
        foreach (var slice in sliceables) { if (slice != null) { try { slice.DeadPart(); } catch { } } }
    }

    private void DestroyEnemyScript(GameObject obj)
    {
        if (obj == null) return;
        Enemy e = obj.GetComponent<Enemy>();
        if (e != null) Destroy(e);
    }

    private void ReparentChildren(GameObject originalTarget, GameObject upperHull, GameObject lowerHull, UnityEngine.Plane slicePlane)
    {
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
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
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

    private void ConnectChildrenToHull(GameObject hull)
    {
        Rigidbody hullRb = hull.GetComponent<Rigidbody>();
        foreach (Transform child in hull.transform)
        {
            ConfigurableJoint childJoint = child.GetComponent<ConfigurableJoint>();
            if (childJoint != null) childJoint.connectedBody = hullRb;
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
}