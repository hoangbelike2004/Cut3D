using UnityEngine;

public class Bullet : GameUnit
{
    [Header("Settings")]
    public float minForce = 4f;
    public float maxForce = 7f;
    public float lifeTime = 5f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // [ĐÃ SỬA] Nhận vào hướng bắn (Vector3) thay vì vị trí mục tiêu
    public void Initialize(Vector3 shootDirection)
    {
        // 1. Reset vật lý
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 2. Random lực bắn
        float randomForce = Random.Range(minForce, maxForce);

        // 3. Bắn thẳng theo hướng được chỉ định
        // ForceMode.Impulse tạo lực đẩy tức thì
        rb.AddForce(shootDirection * randomForce, ForceMode.Impulse);

        // 4. Hẹn giờ tự hủy
        CancelInvoke(nameof(DespawnSelf));
        Invoke(nameof(DespawnSelf), lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Trừ máu Player...
            DespawnSelf();
        }
        else
        {
            DespawnSelf();
        }
    }

    void DespawnSelf()
    {
        CancelInvoke(nameof(DespawnSelf));
        SimplePool.Despawn(this);
    }
}