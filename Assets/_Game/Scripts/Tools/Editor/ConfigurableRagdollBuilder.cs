using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ConfigurableRagdollBuilder : EditorWindow
{
    // --- Ô KÉO CHA TỔNG ---
    public Transform characterRoot;

    // --- CÁC BIẾN XƯƠNG ---
    public Transform pelvis;

    public Transform leftHips;
    public Transform leftKnee;
    public Transform leftFoot;

    public Transform rightHips;
    public Transform rightKnee;
    public Transform rightFoot;

    public Transform leftArm;
    public Transform leftElbow;

    public Transform rightArm;
    public Transform rightElbow;

    public Transform middleSpine;
    public Transform head;

    public float totalMass = 20f;
    public float strength = 0f;

    [MenuItem("Tools/Configurable Ragdoll Builder")]
    public static void ShowWindow()
    {
        GetWindow<ConfigurableRagdollBuilder>("Configurable Ragdoll");
    }

    void OnGUI()
    {
        GUILayout.Label("1. Cấu hình Chung", EditorStyles.boldLabel);
        characterRoot = (Transform)EditorGUILayout.ObjectField("Character Root (Cha tổng)", characterRoot, typeof(Transform), true);

        GUILayout.Space(10);
        GUILayout.Label("2. Cấu hình Xương", EditorStyles.boldLabel);

        pelvis = (Transform)EditorGUILayout.ObjectField("Pelvis (Hông - Gốc)", pelvis, typeof(Transform), true);

        GUILayout.Space(5);
        GUILayout.Label("Chân Trái", EditorStyles.boldLabel);
        leftHips = (Transform)EditorGUILayout.ObjectField("Left Hips", leftHips, typeof(Transform), true);
        leftKnee = (Transform)EditorGUILayout.ObjectField("Left Knee", leftKnee, typeof(Transform), true);
        leftFoot = (Transform)EditorGUILayout.ObjectField("Left Foot", leftFoot, typeof(Transform), true);

        GUILayout.Space(5);
        GUILayout.Label("Chân Phải", EditorStyles.boldLabel);
        rightHips = (Transform)EditorGUILayout.ObjectField("Right Hips", rightHips, typeof(Transform), true);
        rightKnee = (Transform)EditorGUILayout.ObjectField("Right Knee", rightKnee, typeof(Transform), true);
        rightFoot = (Transform)EditorGUILayout.ObjectField("Right Foot", rightFoot, typeof(Transform), true);

        GUILayout.Space(5);
        GUILayout.Label("Tay Trái", EditorStyles.boldLabel);
        leftArm = (Transform)EditorGUILayout.ObjectField("Left Arm", leftArm, typeof(Transform), true);
        leftElbow = (Transform)EditorGUILayout.ObjectField("Left Elbow", leftElbow, typeof(Transform), true);

        GUILayout.Space(5);
        GUILayout.Label("Tay Phải", EditorStyles.boldLabel);
        rightArm = (Transform)EditorGUILayout.ObjectField("Right Arm", rightArm, typeof(Transform), true);
        rightElbow = (Transform)EditorGUILayout.ObjectField("Right Elbow", rightElbow, typeof(Transform), true);

        GUILayout.Space(5);
        GUILayout.Label("Thân & Đầu", EditorStyles.boldLabel);
        middleSpine = (Transform)EditorGUILayout.ObjectField("Middle Spine", middleSpine, typeof(Transform), true);
        head = (Transform)EditorGUILayout.ObjectField("Head", head, typeof(Transform), true);

        GUILayout.Space(15);
        totalMass = EditorGUILayout.FloatField("Total Mass", totalMass);
        strength = EditorGUILayout.FloatField("Joint Spring (Tham khảo)", strength);

        GUILayout.Space(20);

        if (GUILayout.Button("TẠO RAGDOLL (FULL AUTO)", GUILayout.Height(40)))
        {
            if (CheckConsistency())
            {
                CreateRagdoll();
            }
        }
    }

    bool CheckConsistency()
    {
        if (!characterRoot && pelvis) characterRoot = pelvis.root;

        if (!pelvis || !leftHips || !leftKnee || !leftFoot || !rightHips || !rightKnee || !rightFoot ||
            !leftArm || !leftElbow || !rightArm || !rightElbow || !middleSpine || !head)
        {
            EditorUtility.DisplayDialog("Thiếu thông tin", "Vui lòng điền đủ tất cả các xương.", "OK");
            return false;
        }
        return true;
    }

    void CreateRagdoll()
    {
        Cleanup(pelvis);

        // 1. Setup Xương
        BuildBone(pelvis, null, new Transform[] { leftHips, rightHips, middleSpine }, false);

        BuildBone(leftHips, pelvis, new Transform[] { leftKnee }, true);
        BuildBone(leftKnee, leftHips, new Transform[] { leftFoot }, true);
        BuildBone(leftFoot, leftKnee, null, true, true);

        BuildBone(rightHips, pelvis, new Transform[] { rightKnee }, true);
        BuildBone(rightKnee, rightHips, new Transform[] { rightFoot }, true);
        BuildBone(rightFoot, rightKnee, null, true, true);

        BuildBone(middleSpine, pelvis, new Transform[] { leftArm, rightArm, head }, false);
        BuildBone(head, middleSpine, null, false);

        BuildBone(leftArm, middleSpine, new Transform[] { leftElbow }, true);
        BuildBone(leftElbow, leftArm, null, true);

        BuildBone(rightArm, middleSpine, new Transform[] { rightElbow }, true);
        BuildBone(rightElbow, rightArm, null, true);


        Debug.Log("🎉 Đã tạo Ragdoll thành công! (Middle Spine Configured exactly as image)");
    }

    void BuildBone(Transform bone, Transform parent, Transform[] children, bool isLimb, bool isFoot = false)
    {
        // Tag (Đã cập nhật logic tag ở hàm SetupTag bên dưới)
        SetupTag(bone);

        // Rigidbody
        Rigidbody rb = bone.GetComponent<Rigidbody>();
        if (!rb) rb = bone.gameObject.AddComponent<Rigidbody>();

        if (bone == middleSpine)
        {
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.mass = totalMass / 15f;
        }
        else
        {
            rb.mass = totalMass / 15f;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }

        if (isFoot)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else
        {
            if (bone != middleSpine)
                rb.constraints = RigidbodyConstraints.None;
        }

        // --- COLLIDER LOGIC ---
        if (bone == head)
        {
            // HEAD
            CapsuleCollider existingCap = bone.GetComponent<CapsuleCollider>();
            if (existingCap) DestroyImmediate(existingCap);

            SphereCollider sphere = bone.GetComponent<SphereCollider>();
            if (!sphere) sphere = bone.gameObject.AddComponent<SphereCollider>();
            sphere.center = new Vector3(0, 0.1f, 0);
            sphere.radius = 0.1f;
        }
        else
        {
            // OTHERS: CAPSULE
            SphereCollider existingSphere = bone.GetComponent<SphereCollider>();
            if (existingSphere) DestroyImmediate(existingSphere);

            CapsuleCollider collider = bone.GetComponent<CapsuleCollider>();
            if (!collider) collider = bone.gameObject.AddComponent<CapsuleCollider>();

            if (bone == pelvis)
            {
                // [CASE 0] PELVIS
                collider.center = new Vector3(0f, 0.02f, 0f);
                collider.radius = 0.05f;
                collider.height = 0.2f;
                collider.direction = 1;
            }
            else if (bone == leftFoot || bone == rightFoot)
            {
                // [CASE 1] FOOT
                collider.center = new Vector3(0f, 0.09f, 0f);
                collider.radius = 0.03f;
                collider.height = 0.1f;
                collider.direction = 1;
            }
            else if (bone == leftElbow || bone == rightElbow)
            {
                // [CASE 2] ELBOW
                collider.center = new Vector3(0f, 0.12f, 0f);
                collider.radius = 0.05f;
                collider.height = 0.2f;
                collider.direction = 1;
            }
            else if (bone == middleSpine)
            {
                // [CASE MỚI] MIDDLE SPINE (Theo ảnh cấu hình bạn gửi)
                // Center: X=0, Y=0.07, Z=0
                collider.center = new Vector3(0f, 0.07f, 0f);
                // Radius: 0.075
                collider.radius = 0.075f;
                // Height: 0.35
                collider.height = 0.35f;
                // Direction: 1 (Y-Axis) là chuẩn cho spine đứng
                collider.direction = 1;
            }
            else
            {
                // [CASE 3] AUTO CALC
                CalculateCapsuleLogic(bone, children, collider);
            }
        }

        // Joint
        if (parent != null)
        {
            ConfigurableJoint joint = bone.GetComponent<ConfigurableJoint>();
            if (!joint) joint = bone.gameObject.AddComponent<ConfigurableJoint>();

            joint.connectedBody = parent.GetComponent<Rigidbody>();
            SetupJointSettings(joint, isFoot);
        }
    }

    void SetupTag(Transform bone)
    {
        if (bone == head)
        {
            bone.tag = "Head";
        }
        else if (bone == middleSpine)
        {
            // [CẬP NHẬT] Set tag BodyPart cho Middle Spine
            bone.tag = "BodyPart";
        }
        else
        {
            // Chỉ set Untagged nếu chưa có Tag quan trọng
            if (!bone.CompareTag("Head") && !bone.CompareTag("BodyPart"))
                bone.tag = "Untagged";
        }
    }

    void SetupJointSettings(ConfigurableJoint joint, bool isFoot)
    {
        // 1. Khóa vị trí
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        // 2. KHÓA XOAY
        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        // 3. Projection
        joint.projectionMode = JointProjectionMode.PositionAndRotation;
        joint.projectionDistance = 0.1f;
        joint.projectionAngle = 180f;

        // 4. Limits
        SoftJointLimit limit = new SoftJointLimit();
        limit.limit = 45f;
        joint.lowAngularXLimit = new SoftJointLimit() { limit = -45f };
        joint.highAngularXLimit = limit;
        joint.angularYLimit = limit;
        joint.angularZLimit = limit;

        // 5. SPRING DRIVE = 180
        JointDrive drive = new JointDrive();
        drive.positionSpring = 180f;
        drive.positionDamper = 0f;
        drive.maximumForce = float.MaxValue;

        joint.angularXDrive = drive;
        joint.angularYZDrive = drive;
    }

    void CalculateCapsuleLogic(Transform bone, Transform[] children, CapsuleCollider collider)
    {
        if (children != null && children.Length > 0)
        {
            Transform child = children[0];
            Vector3 direction = child.position - bone.position;
            float length = direction.magnitude;

            collider.height = length;
            collider.center = bone.InverseTransformPoint(bone.position + direction * 0.5f);

            Vector3 localDir = bone.InverseTransformDirection(direction);
            if (Mathf.Abs(localDir.x) > Mathf.Abs(localDir.y) && Mathf.Abs(localDir.x) > Mathf.Abs(localDir.z))
                collider.direction = 0;
            else if (Mathf.Abs(localDir.y) > Mathf.Abs(localDir.x) && Mathf.Abs(localDir.y) > Mathf.Abs(localDir.z))
                collider.direction = 1;
            else
                collider.direction = 2;

            collider.radius = length * 0.2f;
        }
        else
        {
            collider.height = 0.2f;
            collider.radius = 0.05f;
            collider.center = Vector3.zero;
        }
    }

    void Cleanup(Transform root) { }
}