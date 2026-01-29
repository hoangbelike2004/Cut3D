using UnityEngine;
public enum eStateWeapon
{
    Lock,
    Open,
    Sellect,
}
[System.Serializable]
public class WeaponData
{
    public PoolType Type;

    public eStateWeapon stateWeapon = eStateWeapon.Lock;

    public Sprite icon;

    public int goal;
}
