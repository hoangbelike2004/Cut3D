using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Enemy : MonoBehaviour
{
    public enum EnemyType { Melee, Ranged }
    public enum BodyPartType { None, Pelvis, Spine, Head, UpperArm, LowerArm, UpperLeg, LowerLeg, Foot }

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

    [Header("LOẠI KẺ ĐỊCH")]
    public EnemyType enemyType = EnemyType.Melee;

    [Header("Cài Đặt Tấn Công Tầm Xa")]
    public float attackRange = 10f;
    public float fireRate = 2f;
    public float recoilForce = 20f; // [THÊM MỚI] Lực giật đầu khi bắn
    private float nextFireTime;

    [Header("Trạng thái")]
    public bool isMoving = true;

    // [THÊM MỚI] Biến bật/tắt xoay người
    public bool enableRotation = true;

    [Header("DANH SÁCH BỘ PHẬN")]
    public List<BodyPart> activeBodyParts = new List<BodyPart>();

    [Header("Thông số Animation & Di Chuyển")]
    public float walkSpeed = 10f;
    public float maxSpeed = 5f;
    public float moveForce = 100f;
    public float stopDistance = 1.0f;

    // Animation settings
    public float hipSwingAngle = 45f;
    public float kneeBendAngle = 40f;
    public float armSwingAngle = 45f;
    public float elbowBendAngle = 30f;

    private Rigidbody mainBody;
    private Transform target;
    private Transform hip;
    private int hp = 100;

    public Transform objectCamFolow;
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
                if (part.type == BodyPartType.Pelvis)
                {
                    hip = part.obj.transform;
                    mainBody = part.obj.GetComponent<Rigidbody>();
                }
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
        if (target == null || hip == null || isDie) return;

        // 1. TÍNH KHOẢNG CÁCH
        float distance = Vector3.Distance(new Vector3(hip.position.x, 0, hip.position.z),
                                          new Vector3(target.position.x, 0, target.position.z));

        // 2. XOAY NGƯỜI (CÓ ĐIỀU KIỆN enableRotation)
        // ---------------------------------------------------------------------
        if (enableRotation)
        {
            Vector3 dir = (target.position - hip.position).normalized;
            dir.y = 0;
            bool hasCore = activeBodyParts.Any(p => p.type == BodyPartType.Pelvis || p.type == BodyPartType.Spine);

            if (dir != Vector3.zero && mainBody != null && hasCore)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                mainBody.MoveRotation(Quaternion.Slerp(mainBody.rotation, lookRot, 5f * Time.fixedDeltaTime));
            }
        }
        // ---------------------------------------------------------------------

        // 3. XỬ LÝ TRẠNG THÁI (ĐỨNG YÊN HAY DI CHUYỂN)
        if (enemyType == EnemyType.Melee)
        {
            if (distance < stopDistance)
            {
                isMoving = false;
                ResetPose();
                return;
            }
        }
        else // Ranged
        {
            if (distance < attackRange)
            {
                isMoving = false;
                ResetPose();
                ShootBehavior();
            }
            else
            {
                isMoving = true;
            }
        }

        if (!isMoving) return;

        // 4. ANIMATION DI CHUYỂN
        ResetDragForMovement();

        Vector3 moveDir = (target.position - hip.position).normalized;
        moveDir.y = 0;

        float cycle = Mathf.Sin(Time.time * walkSpeed);
        bool hasUpperLeg = activeBodyParts.Any(p => p.type == BodyPartType.UpperLeg);
        bool currentSpeedIsHigh = mainBody != null && mainBody.linearVelocity.magnitude > maxSpeed;

        foreach (var part in activeBodyParts)
        {
            if (part == null || part.obj == null || !part.obj.activeInHierarchy || part.joint == null) continue;

            float phase = part.invertPhase ? -1f : 1f;

            switch (part.type)
            {
                case BodyPartType.UpperLeg:
                    part.joint.targetRotation = Quaternion.Euler(cycle * hipSwingAngle * phase, 0, 0);
                    if (part.rb != null && !currentSpeedIsHigh)
                        part.rb.AddForce(moveDir * moveForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
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
                            part.rb.AddForce(moveDir * moveForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
                    }
                    break;
                case BodyPartType.Foot:
                    part.joint.targetRotation = Quaternion.Euler(cycle * 10f * phase, 0, 0); break;
                case BodyPartType.UpperArm:
                    part.joint.targetRotation = Quaternion.Euler(-cycle * armSwingAngle * phase, 0, 0); break;
                case BodyPartType.LowerArm:
                    part.joint.targetRotation = Quaternion.Euler(-elbowBendAngle, 0, 0); break;
                default:
                    part.joint.targetRotation = Quaternion.identity; break;
            }
        }
    }

    void ShootBehavior()
    {
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            if (objectCamFolow != null)
            {
                Bullet bullet = SimplePool.Spawn<Bullet>(PoolType.Bullet, objectCamFolow.position, objectCamFolow.rotation);
                bullet.Initialize(objectCamFolow.forward);

                // --- [THÊM MỚI] TẠO LỰC GIẬT (RECOIL) ---
                ApplyRecoil();
            }
        }
    }

    // [THÊM MỚI] Hàm xử lý lực giật
    void ApplyRecoil()
    {
        // 1. Tìm phần Đầu hoặc Ngực
        var headPart = activeBodyParts.FirstOrDefault(p => p.type == BodyPartType.Head);
        var spinePart = activeBodyParts.FirstOrDefault(p => p.type == BodyPartType.Spine);

        // Ưu tiên giật đầu, nếu không có đầu thì giật ngực
        BodyPart partToRecoil = headPart != null ? headPart : spinePart;

        if (partToRecoil != null && partToRecoil.rb != null && objectCamFolow != null)
        {
            // Lực giật ngược hướng bắn (ngược hướng forward của objectCamFolow)
            // ForceMode.Impulse tạo một lực tức thời mạnh
            partToRecoil.rb.AddForce(objectCamFolow.forward * recoilForce, ForceMode.Impulse);
        }
    }

    private void ResetPose()
    {
        foreach (var part in activeBodyParts)
        {
            if (part.joint != null) part.joint.targetRotation = Quaternion.identity;
            if (part.rb != null)
            {
                part.rb.linearDamping = 10f;
                part.rb.angularDamping = 10f;
            }
        }
    }

    private void ResetDragForMovement()
    {
        foreach (var part in activeBodyParts)
        {
            if (part.rb != null)
            {
                part.rb.linearDamping = 0f;
                part.rb.angularDamping = 0.05f;
            }
        }
    }

    public void RemovePart(GameObject lostObject)
    {
        List<BodyPart> partsToRemove = new List<BodyPart>();
        foreach (var part in activeBodyParts)
        {
            if (part.obj != null && (part.obj == lostObject || part.obj.transform.IsChildOf(lostObject.transform)))
                partsToRemove.Add(part);
        }
        foreach (var part in partsToRemove)
        {
            // Nếu mất xương sống hoặc hông -> Tắt di chuyển và Tắt xoay luôn
            if (part.type == BodyPartType.Spine || part.type == BodyPartType.Pelvis)
            {
                isMoving = false;
                enableRotation = false; // [TỰ ĐỘNG TẮT XOAY KHI MẤT GỐC]
            }
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
        enableRotation = false; // [TỰ ĐỘNG TẮT XOAY KHI CHẾT]

        if (level != null) level.RemoveEnemy(this);
        MakeBodyLimp();
        BreakAllJoints();
    }

    private void MakeBodyLimp()
    {
        foreach (var j in GetComponentsInChildren<ConfigurableJoint>())
        {
            if (j == null) continue;
            var drive = j.angularXDrive; drive.positionSpring = 0; j.angularXDrive = drive;
            drive = j.angularYZDrive; drive.positionSpring = 0; j.angularYZDrive = drive;
        }
    }

    private void BreakAllJoints()
    {
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            if (rb.GetComponent<Joint>()) Destroy(rb.GetComponent<Joint>());
            rb.transform.SetParent(level != null ? level.transform : null, true);
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.interpolation = RigidbodyInterpolation.None;
            Destroy(rb.gameObject, 5f);
        }
        foreach (Rigidbody rb in rbs) rb.AddExplosionForce(breakForce, transform.position, breakRadius);
        Destroy(gameObject, 0.1f);
    }

    public void SetTarget(Transform target) => this.target = target;
    public void SetMoving() => isMoving = true;
}