using System.Collections.Generic;
using UnityEngine;

public class Sliceable : MonoBehaviour
{
    [Header("Cấu hình")]
    public Material internalMaterial;
    public bool canBeCut = true;

    [Header("Trạng thái (Debug)")]
    public int currentHitCountMax = 0;
    public int damage;
    public bool isBroken = false;   // Đánh dấu xem đã bị vỡ lần đầu chưa
    public bool isHead = false;
    public bool isHip = false;

    private int currentHitCount = 0; // Đếm số lần trúng
    private Enemy parent;
    private Sliceable sliceable; // thang ban dau khi chua bi cat

    List<Projectile> projectiles = new List<Projectile>();
    List<Projectile> projectilesOnPeople = new List<Projectile>();

    // [MỚI] Biến lưu tham chiếu đến vật chứa (Container)
    private Transform stuckProjectilesContainer;

    void Start()
    {
        OnInit();
    }

    public void OnInit()
    {
        isBroken = false;
        currentHitCount = 0;
        if (parent == null) parent = GetComponentInParent<Enemy>();

        // Reset lại container nếu cần (phòng trường hợp dùng Pool)
        // Lưu ý: Projectile tự Despawn nên container sẽ rỗng, ta có thể giữ lại hoặc xóa đi tùy logic
    }

    public void AddProjectiles(Projectile projectile)
    {
        if (!projectiles.Contains(projectile))
        {
            if (parent != null) parent.SetMoving();
            projectiles.Add(projectile);
            currentHitCount++;

            // Logic check vỡ
            isBroken = currentHitCount > currentHitCountMax ? true : false;

            if (isBroken)
            {
                // Xử lý khi bị vỡ (Slice)
                List<Transform> tfs = projectiles.ConvertAll(x => x.transform);
                for (int i = 0; i < tfs.Count; i++)
                {
                    if (projectiles[i].isStick)
                    {
                        projectiles[i].DespawnSelf();
                    }
                }
                parent.Hit(projectiles.Count * damage);
                GameController.Instance.DelayGame();
                Observer.OnCuttingMultipObject?.Invoke(tfs, transform);

                projectiles.Clear();

                // [MỚI] Khi vỡ rồi thì xóa luôn cái container đi cho sạch
                if (stuckProjectilesContainer != null)
                {
                    Destroy(stuckProjectilesContainer.gameObject);
                    stuckProjectilesContainer = null;
                }
            }
            else
            {
                // [MỚI] LOGIC TẠO CONTAINER
                if (stuckProjectilesContainer == null)
                {
                    // 1. Tạo GameObject rỗng
                    GameObject container = new GameObject("Stuck_Container");

                    // 2. Set nó làm con của Sliceable này
                    container.transform.SetParent(this.transform.parent);

                    // 3. Reset toạ độ về 0 so với cha
                    container.transform.localPosition = Vector3.zero;
                    container.transform.localRotation = Quaternion.identity;
                    container.transform.localScale = Vector3.one;

                    stuckProjectilesContainer = container.transform;
                }

                // [QUAN TRỌNG] Truyền cái Container vào cho Projectile dính vào
                // Thay vì truyền 'transform' (chính mình), ta truyền 'stuckProjectilesContainer'
                projectile.StickProjectile(stuckProjectilesContainer);
            }
        }
    }

    public void SetParentOld(Sliceable sliceable)
    {
        this.sliceable = sliceable;
        this.damage = sliceable.damage;
        this.parent = sliceable.GetParent;
    }

    public Sliceable GetParentOld => sliceable != null ? sliceable : this;

    public Enemy GetParent => parent;
}