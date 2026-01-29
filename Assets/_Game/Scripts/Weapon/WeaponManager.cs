using UnityEngine;

public class WeaponManager : Singleton<WeaponManager>
{
    public WeaponData WeaponSellect => weaponSellect;
    private WeaponData weaponSellect;

    private WeaponSO weaponSO;
    void Awake()
    {
        weaponSO = Resources.Load<WeaponSO>(GameConstants.KEY_DATA_GAME_WEAPON);
        for (int i = 0; i < weaponSO.weapons.Count; i++)
        {
            if (weaponSO.weapons[i].stateWeapon == eStateWeapon.Sellect)
            {
                weaponSellect = weaponSO.weapons[i];
                break;
            }
        }
    }
    public void SetWeapon(int currentlevel)
    {
        for (int i = 0; i < weaponSO.weapons.Count; i++)
        {
            if (weaponSO.weapons[i].stateWeapon == eStateWeapon.Lock &&
            weaponSO.weapons[i].goal >= currentlevel)
            {
                weaponSO.weapons[i].stateWeapon = eStateWeapon.Open;
                Observer.OnUpdateItemWeapon?.Invoke(weaponSO.weapons[i]);
                break;
            }
        }
    }
    public void SellectWeapon(WeaponData weaponData)
    {
        if (weaponData == weaponSellect) return;
        for (int i = 0; i < weaponSO.weapons.Count; i++)
        {
            if (weaponSO.weapons[i].stateWeapon == eStateWeapon.Lock) break;
            if (weaponSO.weapons[i] == weaponData)
            {
                weaponSO.weapons[i].stateWeapon = eStateWeapon.Sellect;
                weaponSellect.stateWeapon = eStateWeapon.Open;
                Observer.OnUpdateItemWeapon?.Invoke(weaponSellect);
                weaponSellect = weaponSO.weapons[i];
            }
            else
            {
                weaponSO.weapons[i].stateWeapon = eStateWeapon.Open;
            }
        }

        Observer.OnSellectWeapon?.Invoke(weaponSellect);
    }
}
