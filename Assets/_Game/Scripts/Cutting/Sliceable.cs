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

    private Sliceable sliceable;//thang ban dau khi chua bi cat

    List<Projectile> projectiles = new List<Projectile>();
    List<Projectile> projectilesOnPeople = new List<Projectile>();
    void Start()
    {
        OnInit();
    }
    public void OnInit()
    {
        isBroken = false;
        currentHitCount = 0;
        if (parent == null) parent = GetComponentInParent<Enemy>();
    }
    public void AddProjectiles(Projectile projectile)
    {
        if (!projectiles.Contains(projectile))
        {
            if (parent != null) parent.SetMoving();
            projectiles.Add(projectile);
            currentHitCount++;
            isBroken = currentHitCount > currentHitCountMax ? true : false;
            if (isBroken)
            {
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
            }
            else
            {
                projectile.StickProjectile(transform);
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