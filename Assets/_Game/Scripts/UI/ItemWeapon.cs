using UnityEngine;
using UnityEngine.UI;

public class ItemWeapon : MonoBehaviour
{
    [SerializeField] private Button btnClick;
    [SerializeField] private Image iconWeapon;
    [SerializeField] private RectTransform rectLock, rectSelect;
    private WeaponData weaponData;
    void Start()
    {
        btnClick.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (weaponData == null) return;
        if (weaponData.stateWeapon == eStateWeapon.Lock) return;
        WeaponManager.Instance.SellectWeapon(weaponData);
    }

    public void SetData(WeaponData data)
    {
        weaponData = data;
        if (weaponData.icon != null)
        {
            iconWeapon.sprite = weaponData.icon;
        }
        UpdateUI();
    }
    public void UpdateUI()
    {
        if (weaponData.stateWeapon == eStateWeapon.Lock)
        {
            rectLock.gameObject.SetActive(true);
            rectSelect.gameObject.SetActive(false);
        }
        else if (weaponData.stateWeapon == eStateWeapon.Sellect)
        {
            rectLock.gameObject.SetActive(false);
            rectSelect.gameObject.SetActive(true);
        }
        else
        {
            rectLock.gameObject.SetActive(false);
            rectSelect.gameObject.SetActive(false);
        }
    }
}
