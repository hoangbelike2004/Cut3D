using System.Collections.Generic;
using UnityEngine;

public class ObjectSliceable : MonoBehaviour
{
    [Header("Cấu hình")]
    public Material internalMaterial; // Vật liệu bên trong khi cắt
    public bool canBeCut = true;
    public bool changeColor = true;
    [Header("Trạng thái (Debug)")]
    public int currentHitCountMax = 0; // Số lần chém cần thiết để cắt (0 là chém phát đứt luôn)

    public bool isBroken = false;      // Đánh dấu đã bị cắt chưa
    private int currentHitCount = 0;   // Đếm số lần đã trúng

    // Tham chiếu đến vật thể gốc (nếu vật này là mảnh vỡ được sinh ra từ vật khác)
    private ObjectSliceable originObject;

    // List các projectile đã găm vào vật thể này
    List<Projectile> projectiles = new List<Projectile>();
    private Transform stuckProjectilesContainer;
    void Start()
    {
        OnInit();
    }

    public void OnInit()
    {
        isBroken = false;
        currentHitCount = 0;
    }

    // Hàm nhận va chạm từ vũ khí
    public void AddProjectiles(Projectile projectile)
    {
        // Kiểm tra xem projectile này đã được tính chưa
        if (!projectiles.Contains(projectile))
        {
            projectiles.Add(projectile);
            currentHitCount++;

            // Kiểm tra xem số lần chém đã đủ để vỡ chưa
            isBroken = currentHitCount > currentHitCountMax;

            if (isBroken)
            {
                // Lấy danh sách transform của các vũ khí để tạo mặt phẳng cắt
                List<Transform> tfs = projectiles.ConvertAll(x => x.transform);

                // Ẩn các vũ khí đang găm trên vật thể (để khi cắt không bị lơ lửng)
                for (int i = 0; i < projectiles.Count; i++)
                {
                    if (projectiles[i].isStick)
                    {
                        projectiles[i].DespawnSelf();
                    }
                }

                // GỌI EVENT CẮT: Báo cho Observer biết để thực hiện cắt
                // Lưu ý: Bạn cần đảm bảo bên class Cutting hoặc Observer có xử lý cho Transform này
                Observer.OnCuttingMultipObject?.Invoke(tfs, transform);

                projectiles.Clear();
            }
            else
            {
                if (stuckProjectilesContainer == null)
                {
                    // 1. Tạo GameObject rỗng
                    GameObject container = new GameObject("Stuck_Container");

                    // 2. Set nó làm con của Sliceable này
                    container.transform.SetParent(transform);

                    // ---------------------------------

                    stuckProjectilesContainer = container.transform;
                }

                // Truyền Container vào
                projectile.StickProjectile(stuckProjectilesContainer);
            }
        }
    }

    // Hàm dùng để set thông tin khi đối tượng này là một mảnh vỡ vừa được cắt ra
    public void SetOriginOld(ObjectSliceable original)
    {
        this.originObject = original;
        // Copy lại các chỉ số cần thiết từ thằng cha
        this.currentHitCountMax = original.currentHitCountMax;
        this.internalMaterial = original.internalMaterial;
    }

    // Lấy đối tượng gốc (để check trùng lặp trong Projectile)
    public ObjectSliceable GetOriginOld => originObject != null ? originObject : this;
    void OnDestroy()
    {
        // Xóa bỏ tất cả projectiles khi đối tượng bị hủy
        foreach (var projectile in projectiles)
        {
            projectile.DespawnSelf();
        }
    }
}