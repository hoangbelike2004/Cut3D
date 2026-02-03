using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public WeaponData WeaponSellect => weaponSellect;
    private WeaponData weaponSellect;

    private WeaponSO weaponSO;
    void Awake()
    {
        weaponSO = Resources.Load<WeaponSO>(GameConstants.KEY_DATA_GAME_WEAPON);
        if (PlayerPrefs.HasKey(GameConstants.KEY_SAVE_DATA_WEAPON))
        {
            string json = PlayerPrefs.GetString(GameConstants.KEY_SAVE_DATA_WEAPON);
            WeaponDatas weaponDatas = JsonUtility.FromJson<WeaponDatas>(json);
            List<WeaponData> listWeaponData = weaponDatas.weapons;
            LoadData(listWeaponData);
        }
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
            weaponSO.weapons[i].goal <= currentlevel)
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

    public void LoadData(List<WeaponData> listWeaponData)
    {
        for (int i = 0; i < weaponSO.weapons.Count; i++)
        {
            weaponSO.weapons[i].stateWeapon = listWeaponData[i].stateWeapon;
        }
    }

    void SaveData()
    {
        WeaponDatas weaponDatas = new WeaponDatas();
        weaponDatas.weapons = weaponSO.weapons;
        string json = JsonUtility.ToJson(weaponDatas);
        PlayerPrefs.SetString(GameConstants.KEY_SAVE_DATA_WEAPON, json);
    }
    private void OnApplicationQuit()
    {
        SaveData();
    }
    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveData();
        }
    }
}

[System.Serializable]
public class WeaponDatas
{
    public List<WeaponData> weapons;
}
