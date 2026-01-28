using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public static class Observer
{
    public static UnityAction<List<Transform>, Transform> OnCuttingMultipObject;

    public static UnityAction OnDespawnObject;

    public static UnityAction<Enemy> OnDespawnProjectileStickEnemy;
}
