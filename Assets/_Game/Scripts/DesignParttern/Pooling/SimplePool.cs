using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class SimplePool
{
    private static Dictionary<PoolType, Pool> poolInstance = new Dictionary<PoolType, Pool>();


    //khoi ta pool moi
    public static void PreLoad(GameUnit prefab, int amout, Transform parent)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab is emty!!");
            return;
        }

        if (!poolInstance.ContainsKey(prefab.poolType) || poolInstance[prefab.poolType] == null)
        {
            Pool p = new Pool();
            p.PreLoad(prefab, amout, parent);
            poolInstance[prefab.poolType] = p;
        }
    }


    //lay phan tu
    public static T Spawn<T>(PoolType type, Vector3 pos, Quaternion rot) where T : GameUnit
    {
        if (!poolInstance.ContainsKey(type))
        {
            Debug.LogError($"{type}PoolType is not PreLoad!!");
            return null;
        }
        return poolInstance[type].Spawn(pos, rot) as T;
    }

    //tra phan tu vao
    public static void Despawn(GameUnit unit)
    {
        if (!poolInstance.ContainsKey(unit.poolType))
        {
            Debug.LogError($"{unit.poolType} is not preLoad!!");

        }
        poolInstance[unit.poolType].Despawn(unit);
    }


    // thu thap 1 phan tu
    public static void Collect(PoolType poolType)
    {
        if (!poolInstance.ContainsKey(poolType))
        {
            Debug.LogError($"{poolType} is not preLoad!!");

        }
        poolInstance[poolType].Collect();
    }

    //thua thap tat ca
    public static void CollectAll()
    {
        foreach (var pool in poolInstance.Values)
        {
            pool.Collect();
        }
    }


    //xoa 1 phan tu
    public static void ReLease(PoolType poolType)
    {
        if (!poolInstance.ContainsKey(poolType))
        {
            Debug.LogError($"{poolType} is not preLoad!!");

        }
        poolInstance[poolType].Release();
    }


    // xoa tat ca
    public static void ReleaseAll()
    {
        foreach (var pool in poolInstance.Values)
        {
            pool.Release();
        }
    }
}

public class Pool
{
    Transform parent;
    GameUnit prefab;
    //list chua cac unit dang o trong pool
    Queue<GameUnit> inactives = new Queue<GameUnit>();
    //unit dang duoc su dung
    List<GameUnit> actives = new List<GameUnit>();



    //khoi tao cho chinh cai pool cua minh cac phan tu nhu nao
    public void PreLoad(GameUnit prefab, int amount, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
        for (int i = 0; i < amount; i++)
        {
            Despawn(Spawn(Vector3.zero, Quaternion.identity));
        }
    }


    //lay phantu tu pool
    public GameUnit Spawn(Vector3 pos, Quaternion rot)
    {
        GameUnit unit;
        if (inactives.Count <= 0)
        {
            unit = GameObject.Instantiate(prefab, parent);
        }
        else
        {
            unit = inactives.Dequeue();
        }
        unit.TF.SetPositionAndRotation(pos, rot);
        actives.Add(unit);
        unit.gameObject.SetActive(true);
        return unit;
    }

    //tra phan tu vao trong pool
    public void Despawn(GameUnit unit)
    {
        if (unit != null && unit.gameObject.activeSelf)
        {
            unit.TF.SetParent(parent);
            actives.Remove(unit);//xoa no khoi mang dang su dung(co nghia la khi vu khi nem qua tam danh thi deactive no di)
            inactives.Enqueue(unit);// dua no ve mang luu tru va an no di
            unit.gameObject.SetActive(false);
        }
    }

    //thu thap tat ca phan tu dang dung ve pool
    public void Collect()
    {
        while (actives.Count > 0)
        {
            Despawn(actives[0]);
        }
    }


    //destroy tat ca pha tu
    public void Release()
    {
        Collect();
        while (actives.Count > 0) // khi so luong phan tu > 0 thi cu xoa thang tren cung di thoi
        {
            GameObject.Destroy(inactives.Dequeue().gameObject);
        }
        inactives.Clear();
    }
}