using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Enemy : MonoBehaviour
{
    public enum EnemyType { Melee, Ranged }
    public enum BodyPartType { None, Pelvis, Spine, Head, UpperArm, LowerArm, UpperLeg, LowerLeg, Foot, LeftEye, RightEye }

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

    [Header("--- TRẠNG THÁI NGỦ (MỚI) ---")]
    public bool isSleeping = false;

    [Header("LOẠI KẺ ĐỊCH")]
    public EnemyType enemyType = EnemyType.Melee;

    [Header("Cài Đặt Tấn Công Tầm Xa")]
    public float attackRange = 10f;
    public float fireRate = 2f;
    public float recoilForce = 20f;
    public int minMissShots = 2;
    public int maxMissShots = 5;
    public float inaccuracyAngle = 15f;

    private float nextFireTime;
    private int shotsLeftToMiss;

    [Header("Trạng thái")]
    public bool isMoving = true;
    public bool isMoveWhenAttacked = true;
    public bool enableRotation = true;

    [Header("DANH SÁCH BỘ PHẬN")]
    public List<BodyPart> activeBodyParts = new List<BodyPart>();

    [Header("Thông số Animation & Di Chuyển")]
    [Tooltip("Bật = Di chuyển bằng Velocity (Chắc chắn). Tắt = Di chuyển bằng Force (Quán tính)")]
    public bool useVelocityMovement;

    public float walkSpeed = 3f;
    public float maxSpeed = 5f;
    public float moveForce = 50f;
    public float stopDistance = 1.0f;
    public int HP => hp;

    [Header("Animation Settings")]
    public float hipSwingAngle = 45f;
    public float kneeBendAngle = 40f;

    [Header("Cài Đặt Khua Tay")]
    public float armFlailSpeed = 8f;
    public float armFlailRange = 60f;
    public float elbowShake = 45f;

    [Header("Cài Đặt Mắt Hài Hước")]
    public float eyeSpeed = 1.5f;
    public float eyeAngleLimit = 70f;

    private Rigidbody mainBody;
    private Transform target;
    private Transform hip;
    private int hp = 100;

    public Transform objectCamFolow;
    public float breakForce = 300f;
    public float breakRadius = 2f;
    private bool isDie;
    private Level level;
    private float randomSeed;

    private Dictionary<ConfigurableJoint, float> initialJointSprings = new Dictionary<ConfigurableJoint, float>();

    void Awake()
    {
        if (transform.root.GetComponent<Level>() != null)
        {
            level = transform.root.GetComponent<Level>();
            level.AddEnemy(this);
        }

        AutoSetupBodyParts();
        SaveInitialSprings();
        randomSeed = Random.Range(0f, 100f);

        if (enemyType == EnemyType.Ranged)
        {
            ResetMissCounter();
            nextFireTime = Time.time + fireRate;
        }
        if (isSleeping)
        {
            GoToSleep();
        }
    }

    void AutoSetupBodyParts()
    {
        foreach (var part in activeBodyParts)
        {
            if (part.obj != null)
            {
                if (part.type != BodyPartType.LeftEye && part.type != BodyPartType.RightEye)
                {
                    part.rb = part.obj.GetComponent<Rigidbody>();
                    part.joint = part.obj.GetComponent<ConfigurableJoint>();
                }

                if (part.type == BodyPartType.Pelvis)
                {
                    hip = part.obj.transform;
                    mainBody = part.obj.GetComponent<Rigidbody>();
                }
            }
        }
    }

    void SaveInitialSprings()
    {
        initialJointSprings.Clear();
        foreach (var part in activeBodyParts)
        {
            if (part.joint != null)
            {
                initialJointSprings[part.joint] = part.joint.angularXDrive.positionSpring;
            }
        }
    }

    public void GoToSleep()
    {
        if (isSleeping) return;
        isSleeping = true;
        isMoving = false;

        foreach (var part in activeBodyParts)
        {
            if (part.joint != null)
            {
                var drive = part.joint.angularXDrive;
                drive.positionSpring = 0;
                part.joint.angularXDrive = drive;
                part.joint.angularYZDrive = drive;
            }
        }
    }

    public void WakeUp()
    {
        if (!isSleeping) return;
        isSleeping = false;
        if (isMoveWhenAttacked) isMoving = true;

        foreach (var part in activeBodyParts)
        {
            if (part.joint != null && initialJointSprings.ContainsKey(part.joint))
            {
                float originalSpring = initialJointSprings[part.joint];
                var drive = part.joint.angularXDrive;
                drive.positionSpring = originalSpring;
                part.joint.angularXDrive = drive;
                part.joint.angularYZDrive = drive;
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
        if (isSleeping) return;

        float distance = Vector3.Distance(new Vector3(hip.position.x, 0, hip.position.z),
                                          new Vector3(target.position.x, 0, target.position.z));

        // --- 1. XOAY HÔNG ---
        if (enableRotation)
        {
            Vector3 dir = (target.position - hip.position).normalized;
            dir.y = 0;
            bool hasCore = activeBodyParts.Any(p => p.type == BodyPartType.Pelvis || p.type == BodyPartType.Spine);

            if (dir != Vector3.zero && mainBody != null && hasCore && distance < attackRange * 1.5f)
            {
                Quaternion lookRot = Quaternion.LookRotation(dir);
                mainBody.MoveRotation(Quaternion.Slerp(mainBody.rotation, lookRot, 5f * Time.fixedDeltaTime));
            }
        }

        // --- 2. XỬ LÝ LOGIC ---
        if (enemyType == EnemyType.Melee)
        {
            if (distance < stopDistance)
            {
                isMoving = false;
                ResetLegsOnly();
            }
        }
        else // Ranged
        {
            if (distance < stopDistance)
            {
                isMoving = false;
                ResetLegsOnly();
            }

            if (distance < attackRange) ShootBehavior();
            else nextFireTime = Time.time + fireRate;
        }

        // --- 3. ANIMATION & MOVEMENT ---
        if (isMoving && !useVelocityMovement) ResetDragForMovement();

        Vector3 moveDir = (target.position - hip.position).normalized;
        moveDir.y = 0;

        float cycle = Mathf.Sin(Time.time * walkSpeed);
        bool currentSpeedIsHigh = mainBody != null && mainBody.linearVelocity.magnitude > maxSpeed;

        foreach (var part in activeBodyParts)
        {
            if (part == null || part.obj == null || !part.obj.activeInHierarchy) continue;

            if (part.type == BodyPartType.LeftEye || part.type == BodyPartType.RightEye)
            {
                HandleEyeRotation(part);
                continue;
            }

            if (part.joint == null) continue;

            float phase = part.invertPhase ? -1f : 1f;

            switch (part.type)
            {
                // =================================================================
                // [MỚI] CHỈNH SỬA PHẦN ĐẦU CHỈ XOAY Y
                // =================================================================

                case BodyPartType.Head:
                    if (target != null && part.joint.connectedBody != null)
                    {
                        Vector3 dirToTarget = (target.position - part.obj.transform.position).normalized;

                        // Tính góc nhìn thế giới
                        Quaternion worldLook = Quaternion.LookRotation(dirToTarget, Vector3.up);

                        // Chuyển sang góc Local so với Body
                        Quaternion localLook = Quaternion.Inverse(part.joint.connectedBody.rotation) * worldLook;

                        // [TRỌNG TÂM] Chỉ lấy góc Y (Yaw), ép X và Z về 0
                        Vector3 localEuler = localLook.eulerAngles;
                        Quaternion yOnlyRotation = Quaternion.Euler(0, localEuler.y, 0);

                        // ConfigurableJoint TargetRotation là Inverse của Local Rotation
                        Quaternion targetJointRot = Quaternion.Inverse(yOnlyRotation);

                        part.joint.targetRotation = Quaternion.Slerp(part.joint.targetRotation, targetJointRot, 10f * Time.fixedDeltaTime);
                    }
                    break;

                case BodyPartType.Spine:
                    // Spine vẫn giữ xoay tự do (hoặc bạn có thể áp dụng logic Y-only nếu muốn spine cứng)
                    if (target != null && part.joint.connectedBody != null)
                    {
                        Vector3 dirToTarget = (target.position - part.obj.transform.position).normalized;
                        Quaternion worldLook = Quaternion.LookRotation(dirToTarget, Vector3.up);
                        Quaternion localLook = Quaternion.Inverse(part.joint.connectedBody.rotation) * worldLook;
                        Quaternion targetJointRot = Quaternion.Inverse(localLook);
                        part.joint.targetRotation = Quaternion.Slerp(part.joint.targetRotation, targetJointRot, 10f * Time.fixedDeltaTime);
                    }
                    break;
                // =================================================================

                case BodyPartType.UpperLeg:
                    if (isMoving)
                        part.joint.targetRotation = Quaternion.Euler(cycle * hipSwingAngle * phase, 0, 0);
                    break;

                case BodyPartType.LowerLeg:
                    if (isMoving)
                    {
                        bool hasUpperLeg = activeBodyParts.Any(p => p.type == BodyPartType.UpperLeg);
                        if (hasUpperLeg)
                        {
                            float kneeBend = (Mathf.Sin(Time.time * walkSpeed + (part.invertPhase ? Mathf.PI : 0)) + 1) * 0.5f * kneeBendAngle;
                            part.joint.targetRotation = Quaternion.Euler(kneeBend, 0, 0);
                        }
                        else
                        {
                            part.joint.targetRotation = Quaternion.Euler(cycle * hipSwingAngle * phase, 0, 0);
                        }

                        if (part.rb != null && hip != null)
                        {
                            if (useVelocityMovement)
                            {
                                Vector3 forwardDir = hip.forward;
                                forwardDir.y = 0;
                                forwardDir.Normalize();
                                Vector3 targetVel = forwardDir * maxSpeed;
                                part.rb.linearVelocity = new Vector3(targetVel.x, part.rb.linearVelocity.y, targetVel.z);
                            }
                            else
                            {
                                if (!currentSpeedIsHigh)
                                    part.rb.AddForce(moveDir * moveForce * Time.fixedDeltaTime, ForceMode.VelocityChange);
                            }
                        }
                    }
                    else if (!isMoving && part.rb != null && useVelocityMovement)
                    {
                        part.rb.linearVelocity = new Vector3(0, part.rb.linearVelocity.y, 0);
                    }
                    break;

                case BodyPartType.Foot:
                    if (isMoving)
                        part.joint.targetRotation = Quaternion.Euler(cycle * 10f * phase, 0, 0);
                    break;

                case BodyPartType.UpperArm:
                    float noiseX = (Mathf.PerlinNoise(Time.time * armFlailSpeed * 0.5f, randomSeed + phase) - 0.5f) * 2f;
                    float noiseY = (Mathf.PerlinNoise(Time.time * armFlailSpeed, randomSeed + phase + 100) - 0.5f) * 2f;
                    float noiseZ = (Mathf.PerlinNoise(Time.time * armFlailSpeed * 0.8f, randomSeed + phase + 200) - 0.5f) * 2f;
                    Quaternion chaosRot = Quaternion.Euler(noiseX * armFlailRange, noiseY * armFlailRange / 2, noiseZ * armFlailRange / 2 + (phase * 20));
                    part.joint.targetRotation = chaosRot;
                    if (part.rb != null) { part.rb.linearDamping = 0f; part.rb.angularDamping = 0.05f; }
                    break;

                case BodyPartType.LowerArm:
                    float elbowNoise = Mathf.PerlinNoise(Time.time * armFlailSpeed, randomSeed + phase + 300);
                    float elbowBend = -10f - (elbowNoise * elbowShake);
                    part.joint.targetRotation = Quaternion.Euler(elbowBend, 0, 0);
                    if (part.rb != null) { part.rb.linearDamping = 0f; part.rb.angularDamping = 0.05f; }
                    break;

                default:
                    if (part.type != BodyPartType.Head && part.type != BodyPartType.Spine && part.type != BodyPartType.Pelvis)
                        part.joint.targetRotation = Quaternion.identity;
                    break;
            }
        }
    }

    // ... (Giữ nguyên phần còn lại của code: HandleEyeRotation, ShootBehavior, v.v...) ...

    void HandleEyeRotation(BodyPart part)
    {
        float seedOffset = (part.type == BodyPartType.LeftEye) ? 0f : 500f;
        float noiseX = (Mathf.PerlinNoise(Time.time * eyeSpeed, randomSeed + seedOffset) - 0.5f) * 2f;
        float noiseY = (Mathf.PerlinNoise(Time.time * eyeSpeed, randomSeed + seedOffset + 100) - 0.5f) * 2f;
        float lookX = noiseX * eyeAngleLimit;
        float lookY = noiseY * eyeAngleLimit;
        float rollZ = (part.type == BodyPartType.RightEye) ? (Mathf.Sin(Time.time * 0.5f) * 40f) + (Time.time * 20f) : Mathf.Sin(Time.time * 2f) * 10f;
        part.obj.transform.localRotation = Quaternion.Euler(lookX, lookY, rollZ);
    }

    void ShootBehavior()
    {
        if (Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            if (objectCamFolow != null && target != null)
            {
                Vector3 directionToTarget = (target.position - objectCamFolow.position).normalized;
                Vector3 finalShootDirection = directionToTarget;

                if (shotsLeftToMiss > 0)
                {
                    Vector3 noise = Random.insideUnitSphere * Mathf.Tan(inaccuracyAngle * Mathf.Deg2Rad);
                    finalShootDirection = (directionToTarget + noise).normalized;
                    shotsLeftToMiss--;
                }
                else
                {
                    ResetMissCounter();
                }

                objectCamFolow.rotation = Quaternion.LookRotation(finalShootDirection);
                Bullet bullet = SimplePool.Spawn<Bullet>(PoolType.Bullet, objectCamFolow.position, objectCamFolow.rotation);
                bullet.Initialize(objectCamFolow.forward);
                ApplyRecoil();
            }
        }
    }

    void ResetMissCounter() => shotsLeftToMiss = Random.Range(minMissShots, maxMissShots + 1);

    void ApplyRecoil()
    {
        var headPart = activeBodyParts.FirstOrDefault(p => p.type == BodyPartType.Head);
        var spinePart = activeBodyParts.FirstOrDefault(p => p.type == BodyPartType.Spine);
        BodyPart partToRecoil = headPart != null ? headPart : spinePart;

        if (partToRecoil != null && partToRecoil.rb != null && objectCamFolow != null)
        {
            Debug.Log(partToRecoil.obj.name + " recoil applied.");
            partToRecoil.rb.AddForce(objectCamFolow.forward * recoilForce, ForceMode.Impulse);
        }
    }

    private void ResetLegsOnly()
    {
        foreach (var part in activeBodyParts)
        {
            if (part.type == BodyPartType.UpperArm || part.type == BodyPartType.LowerArm || part.type == BodyPartType.LeftEye || part.type == BodyPartType.RightEye) continue;

            if (part.joint != null) part.joint.targetRotation = Quaternion.identity;

            if (!useVelocityMovement && part.rb != null)
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
            if (part.type == BodyPartType.UpperArm || part.type == BodyPartType.LowerArm || part.type == BodyPartType.LeftEye || part.type == BodyPartType.RightEye) continue;
            if (part.rb != null) { part.rb.linearDamping = 0f; part.rb.angularDamping = 0.05f; }
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
            if (part.type == BodyPartType.Spine || part.type == BodyPartType.Pelvis)
            {
                isMoving = false;
                enableRotation = false;
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
        enableRotation = false;

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
            if (rb.gameObject.GetComponent<Projectile>() == null)
            {
                rb.transform.SetParent(level != null ? level.transform : null, true);
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.interpolation = RigidbodyInterpolation.None;
                Destroy(rb.gameObject, 5f);
            }
        }
        foreach (Rigidbody rb in rbs) rb.AddExplosionForce(breakForce, transform.position, breakRadius);
        Destroy(gameObject, 0.1f);
    }

    public GameObject GetHipObject()
    {
        if (isDie) return null;
        var hipPart = activeBodyParts.FirstOrDefault(p => p.type == BodyPartType.Pelvis);
        return hipPart != null ? hipPart.obj : null;
    }

    public void SetTarget(Transform target) => this.target = target;
    public void SetMoving()
    {
        if (isSleeping)
        {
            WakeUp();
            return;
        }
        isMoving = isMoveWhenAttacked ? true : false;
    }
}