using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Enemy : MonoBehaviour
{
    // --- 1. ĐỊNH NGHĨA CHI TIẾT ---
    public enum BodyPartType
    {
        None, Pelvis, Spine, Head, UpperArm, LowerArm, UpperLeg, LowerLeg, Foot
    }

    [System.Serializable]
    public class BodyPart
    {
        public string name;
        public BodyPartType type;
        public GameObject obj;
        public bool invertPhase;

        [HideInInspector] public Rigidbody rb;
        [HideInInspector] public ConfigurableJoint joint;
    }

    [Header("Trạng thái")]
    public bool isMoving = true;

    [Header("DANH SÁCH BỘ PHẬN")]
    public List<BodyPart> activeBodyParts = new List<BodyPart>();

    [Header("Thông số Animation & Di Chuyển")]
    public float walkSpeed = 10f;
    public float maxSpeed = 5f;
    public float moveForce = 100f;

    public float hipSwingAngle = 45f;
    public float kneeBendAngle = 40f;
    public float armSwingAngle = 45f;
    public float elbowBendAngle = 30f;

    [Header("Cài đặt chung")]
    public Rigidbody mainBody;
    public Transform target;
    public Transform hip;
    public int hp;
    public float stopDistance = 1.0f;
    public Transform objectCamFolow;

    [Header("Hiệu ứng chết")]
    public float breakForce = 300f;
    public float breakRadius = 2f;

    private bool isDie;
    private Level level;

    void Awake()
    {
        if (transform.root.GetComponent<Level>() != null)
        {
            level = transform.root.GetComponent<Level>();
            level.AddEnemy(this);
        }
        AutoSetupBodyParts();
    }

    void AutoSetupBodyParts()
    {
        foreach (var part in activeBodyParts)
        {
            if (part.obj != null)
            {
                part.rb = part.obj.GetComponent<Rigidbody>();
                part.joint = part.obj.GetComponent<ConfigurableJoint>();
            }
        }
    }

    public GameObject GetHeadObject()
    {
        var headPart = activeBodyParts.FirstOrDefault(p => p.type == BodyPartType.Head);
        return headPart != null ? headPart.obj : null;
    }

    void FixedUpdate()
    {
        if (!isMoving || target == null || hip == null || isDie) return;

        float distance = Vector3.Distance(new Vector3(hip.position.x, 0, hip.position.z),
                                          new Vector3(target.position.x, 0, target.position.z));

        if (distance < stopDistance)
        {
            isMoving = false;
            ResetPose();
            if (GameController.Instance != null) GameController.Instance.ReplayGame();
            return;
        }

        Vector3 dir = (target.position - hip.position).normalized;
        dir.y = 0;

        bool hasCore = activeBodyParts.Any(p => p.type == BodyPartType.Pelvis || p.type == BodyPartType.Spine);
        if (dir != Vector3.zero && mainBody != null && hasCore)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            mainBody.MoveRotation(Quaternion.Slerp(mainBody.rotation, lookRot, 5f * Time.fixedDeltaTime));
        }

        float cycle = Mathf.Sin(Time.time * walkSpeed);
        bool hasUpperLeg = activeBodyParts.Any(p => p.type == BodyPartType.UpperLeg);

        // Logic Max Speed
        bool currentSpeedIsHigh = false;
        if (mainBody != null && mainBody.linearVelocity.magnitude > maxSpeed)
        {
            currentSpeedIsHigh = true;
        }

        foreach (var part in activeBodyParts)
        {
            if (part == null || part.obj == null || !part.obj.activeInHierarchy || part.joint == null) continue;

            float phase = part.invertPhase ? -1f : 1f;

            switch (part.type)
            {
                case BodyPartType.UpperLeg:
                    part.joint.targetRotation = Quaternion.Euler(cycle * hipSwingAngle * phase, 0, 0);
                    if (part.rb != null && !currentSpeedIsHigh)
                        part.rb.AddForce(dir * moveForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
                    break;

                case BodyPartType.LowerLeg:
                    if (hasUpperLeg)
                    {
                        float kneeBend = (Mathf.Sin(Time.time * walkSpeed + (part.invertPhase ? Mathf.PI : 0)) + 1) * 0.5f * kneeBendAngle;
                        part.joint.targetRotation = Quaternion.Euler(kneeBend, 0, 0);
                    }
                    else
                    {
                        part.joint.targetRotation = Quaternion.Euler(cycle * hipSwingAngle * phase, 0, 0);
                        if (part.rb != null && !currentSpeedIsHigh)
                            part.rb.AddForce(dir * moveForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
                    }
                    break;

                case BodyPartType.Foot:
                    part.joint.targetRotation = Quaternion.Euler(cycle * 10f * phase, 0, 0);
                    break;

                case BodyPartType.UpperArm:
                    part.joint.targetRotation = Quaternion.Euler(-cycle * armSwingAngle * phase, 0, 0);
                    break;

                case BodyPartType.LowerArm:
                    part.joint.targetRotation = Quaternion.Euler(-elbowBendAngle, 0, 0);
                    break;

                default:
                    part.joint.targetRotation = Quaternion.identity;
                    break;
            }
        }
    }

    // --- [ĐÃ SỬA] HÀM REMOVE PART CÓ CHỈNH LỰC SPRING ---
    public void RemovePart(GameObject lostObject)
    {
        List<BodyPart> partsToRemove = new List<BodyPart>();

        // 1. Tìm bộ phận bị cắt và các con cháu của nó
        foreach (var part in activeBodyParts)
        {
            if (part.obj != null)
            {
                if (part.obj == lostObject || part.obj.transform.IsChildOf(lostObject.transform))
                {
                    partsToRemove.Add(part);
                }
            }
        }

        // 2. Xử lý xóa và chỉnh lực
        foreach (var part in partsToRemove)
        {
            // [LOGIC MỚI] Nếu là Thân (Spine) hoặc Hông (Pelvis)
            if (part.type == BodyPartType.Spine || part.type == BodyPartType.Pelvis)
            {
                if (part.joint != null)
                {
                    // Chỉnh lực Angular X Spring về 180
                    var driveX = part.joint.angularXDrive;
                    driveX.positionSpring = 0f;
                    part.joint.angularXDrive = driveX;

                    // Chỉnh lực Angular YZ Spring về 180
                    var driveYZ = part.joint.angularYZDrive;
                    driveYZ.positionSpring = 0f;
                    part.joint.angularYZDrive = driveYZ;
                }

                // [Tùy chọn] Nếu mất thân thì có thể cho ngừng di chuyển luôn
                isMoving = false;
            }

            // Xóa khỏi danh sách điều khiển Animation
            activeBodyParts.Remove(part);
        }
    }

    public void Hit(int dame)
    {
        if (isDie) return;
        hp -= dame;
        if (hp <= 0) Die();
    }

    public void Die()
    {
        if (isDie) return;
        isDie = true;
        isMoving = false;

        if (level != null) level.RemoveEnemy(this);

        BreakAllJoints();
    }

    private void BreakAllJoints()
    {
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            Joint j = rb.GetComponent<Joint>();
            if (j != null) Destroy(j);

            if (level != null) rb.transform.SetParent(level.transform, true);
            else rb.transform.SetParent(null);

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.maxDepenetrationVelocity = 0.5f;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            Destroy(rb.gameObject, 5f);
        }

        foreach (Rigidbody rb in rbs)
        {
            rb.AddExplosionForce(breakForce, transform.position, breakRadius);
        }

        Destroy(gameObject, 0.1f);
    }

    private void ResetPose()
    {
        foreach (var part in activeBodyParts)
        {
            if (part.joint != null) part.joint.targetRotation = Quaternion.identity;
            if (part.rb != null)
            {
                part.rb.linearVelocity = Vector3.zero;
                part.rb.angularVelocity = Vector3.zero;
            }
        }
        if (mainBody != null)
        {
            mainBody.linearVelocity = Vector3.zero;
            mainBody.angularVelocity = Vector3.zero;
        }
    }

    public void SetTarget(Transform target) => this.target = target;
    public void SetMoving() => isMoving = true;
}