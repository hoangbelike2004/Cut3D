using System.Collections.Generic;
using UnityEngine;

public class Sliceable : MonoBehaviour
{
    [Header("Cấu hình")]
    public Material internalMaterial;
    public bool canBeCut = true;

    [Header("Trạng thái (Debug)")]

    public int currentHitCountMax = 3;

    public int damage;
    private int currentHitCount = 0; // Đếm số lần trúng

    private Enemy parent;
    public bool isBroken = false;   // Đánh dấu xem đã bị vỡ lần đầu chưa

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
        parent = GetComponentInParent<Enemy>();
    }
    public void AddProjectiles(Projectile projectile)
    {
        if (!projectiles.Contains(projectile))
        {
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
                        projectiles[i].gameObject.SetActive(false);
                    }
                }
                Observer.OnCuttingMultipObject?.Invoke(tfs, transform);
                projectiles.Clear();
            }
            else
            {
                Debug.Log(1);
                projectile.StickProjectile(transform);
            }
        }
    }
}