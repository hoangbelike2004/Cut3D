using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Enemy : MonoBehaviour
{
    // --- 1. ĐỊNH NGHĨA CHI TIẾT CÁC BỘ PHẬN ---
    public enum BodyPartType
    {
        None,
        Pelvis,     // Hông tổng (Gốc rễ)
        Spine,      // Cột sống (Quan trọng: Mất cái này là người gãy đôi -> Chết)
        Head,       // Đầu (Mất -> Chết)
        UpperArm,   // Bắp tay
        LowerArm,   // Cẳng tay / Khuỷu tay
        UpperLeg,   // Đùi (Động lực chính để đi)
        LowerLeg,   // Cẳng chân / Đầu gối
        Foot        // Bàn chân
    }

    // --- 2. STRUCT LƯU TRỮ ---
    [System.Serializable]
    public class BodyPart
    {
        public string name;             // Tên (đặt cho dễ nhớ)
        public BodyPartType type;       // Loại
        public GameObject obj;          // KÉO OBJECT VÀO ĐÂY
        public bool invertPhase;        // Đảo nhịp (Trái/Phải)

        // Ẩn trong Inspector cho gọn, code sẽ tự tìm
        [HideInInspector] public Rigidbody rb;
        [HideInInspector] public ConfigurableJoint joint;
    }

    [Header("Trạng thái")]
    public bool isMoving = true;

    [Header("DANH SÁCH BỘ PHẬN")]
    // Kéo GameObject vào đây, chọn Type và InvertPhase.
    public List<BodyPart> activeBodyParts = new List<BodyPart>();

    [Header("Thông số Animation")]
    public float walkSpeed = 10f;
    public float hipSwingAngle = 45f; // Góc đá đùi
    public float kneeBendAngle = 40f; // Góc gập gối
    public float armSwingAngle = 45f; // Góc vung tay
    public float elbowBendAngle = 30f;// Góc gập khuỷu tay
    public float moveForce = 100f;    // Lực đẩy

    [Header("Cài đặt chung")]
    public Rigidbody mainBody;        // Thường là Pelvis
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

        // Tự động tìm Rigidbody và Joint
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

    void FixedUpdate()
    {
        // 1. KIỂM TRA ĐIỀU KIỆN
        if (!isMoving || target == null || hip == null || isDie) return;

        // 2. KHOẢNG CÁCH
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

        // 3. XOAY THÂN
        bool hasCore = activeBodyParts.Any(p => p.type == BodyPartType.Pelvis || p.type == BodyPartType.Spine);
        if (dir != Vector3.zero && mainBody != null && hasCore)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            mainBody.MoveRotation(Quaternion.Slerp(mainBody.rotation, lookRot, 5f * Time.fixedDeltaTime));
        }

        // 4. ANIMATION VÀ DI CHUYỂN
        float cycle = Mathf.Sin(Time.time * walkSpeed);

        // [QUAN TRỌNG] Kiểm tra xem nhân vật này có Đùi (UpperLeg) không?
        bool hasUpperLeg = activeBodyParts.Any(p => p.type == BodyPartType.UpperLeg);

        foreach (var part in activeBodyParts)
        {
            if (part.obj == null || !part.obj.activeInHierarchy || part.joint == null) continue;

            float phase = part.invertPhase ? -1f : 1f;

            switch (part.type)
            {
                case BodyPartType.UpperLeg: // Đùi - Luôn đẩy đi
                    part.joint.targetRotation = Quaternion.Euler(cycle * hipSwingAngle * phase, 0, 0);
                    if (part.rb != null)
                        part.rb.AddForce(dir * moveForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
                    break;

                case BodyPartType.LowerLeg: // Cẳng chân

                    if (hasUpperLeg)
                    {
                        // TRƯỜNG HỢP 1: Có đùi -> Cẳng chân chỉ gập gối (Logic cũ)
                        float kneeBend = (Mathf.Sin(Time.time * walkSpeed + (part.invertPhase ? Mathf.PI : 0)) + 1) * 0.5f * kneeBendAngle;
                        part.joint.targetRotation = Quaternion.Euler(kneeBend, 0, 0);
                    }
                    else
                    {
                        // TRƯỜNG HỢP 2: Không có đùi (Nhân vật lùn) -> Cẳng chân phải làm nhiệm vụ của đùi
                        // A. Animation: Phải đá trước sau (Swing) thay vì gập (Bend)
                        part.joint.targetRotation = Quaternion.Euler(cycle * hipSwingAngle * phase, 0, 0);

                        // B. Di chuyển: Phải sinh lực đẩy (AddForce)
                        if (part.rb != null)
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

    // --- XỬ LÝ KHI BỊ CẮT (Quan Trọng) ---
    public void RemovePart(GameObject lostObject)
    {
        var partToRemove = activeBodyParts.FirstOrDefault(x => x.obj == lostObject);
        if (partToRemove != null)
        {
            activeBodyParts.Remove(partToRemove);

            // 1. MẤT ĐẦU -> CHẾT
            if (partToRemove.type == BodyPartType.Head)
            {
                Die();
            }
            // 2. MẤT CỘT SỐNG (BỊ CẮT ĐÔI NGƯỜI) -> CHẾT
            // Khi gọi Die(), các khớp chân sẽ bị phá hủy -> Thân dưới tự động ngã ra (Ragdoll)
            else if (partToRemove.type == BodyPartType.Spine)
            {
                Die();
            }
            // 3. MẤT HÔNG TỔNG -> TÊ LIỆT
            else if (partToRemove.type == BodyPartType.Pelvis)
            {
                isMoving = false;
            }
        }
    }

    public void Hit(int dame)
    {
        if (isDie) return;
        hp -= dame;
        if (hp <= 0) Die();
    }

    private void Die()
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
            // Phá hủy khớp để các bộ phận rơi tự do
            Joint j = rb.GetComponent<Joint>();
            if (j != null) Destroy(j);

            if (level != null) rb.transform.SetParent(level.transform, true);
            else rb.transform.SetParent(null);

            // Reset vật lý
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
        Destroy(gameObject);
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
    // Thêm hàm này vào class Enemy
    public GameObject GetHeadObject()
    {
        // Tìm trong list xem cái nào là Head
        var headPart = activeBodyParts.FirstOrDefault(p => p.type == BodyPartType.Head);
        return headPart != null ? headPart.obj : null;
    }
    public void SetTarget(Transform target) => this.target = target;
    public void SetMoving() => isMoving = true;
}