using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasGameplay : UICanvas
{
    [SerializeField] Button btnWardrobe, btnOpenSetting;

    [SerializeField] RectTransform parent, wardrobe;

    public TextMeshProUGUI txtLevel;

    public RectTransform rectGamecomplete;
    public RectTransform rectGameLose;

    private WeaponSO weaponSO;

    private ItemWeapon itemWeapon;

    private Dictionary<WeaponData, ItemWeapon> dictItemWeapon = new Dictionary<WeaponData, ItemWeapon>();

    private bool isOpenWardrobe = false;

    private GameSetting gameSetting;
    void Awake()
    {
        weaponSO = Resources.Load<WeaponSO>(GameConstants.KEY_DATA_GAME_WEAPON);
        itemWeapon = Resources.Load<ItemWeapon>("UI/ItemWeapon");
    }
    void Start()
    {
        InitItemWeapon();
        btnWardrobe.onClick.AddListener(OpenWardrobe);
        btnOpenSetting.onClick.AddListener(() =>
        {
            UIManager.Instance.OpenUI<CanvasGameSetting>();
        });
    }

    void InitItemWeapon()
    {
        for (int i = 0; i < weaponSO.weapons.Count; i++)
        {
            itemWeapon = Instantiate(itemWeapon, parent);
            itemWeapon.SetData(weaponSO.weapons[i]);
            dictItemWeapon.Add(weaponSO.weapons[i], itemWeapon);
        }
    }
    public void OpenWardrobe()
    {
        isOpenWardrobe = !isOpenWardrobe;
        wardrobe.gameObject.SetActive(isOpenWardrobe);
        Observer.OnOpenWardrobe?.Invoke(isOpenWardrobe);

    }
    public void UpdateLevel(int level)
    {
        txtLevel.text = "LEVEL " + level;
    }
    private void UpdateItemWeapon(WeaponData weaponData)
    {
        if (dictItemWeapon.ContainsKey(weaponData))
        {
            dictItemWeapon[weaponData].UpdateUI();
        }
    }

    public void OnSellectItem(WeaponData weaponData)
    {
        if (dictItemWeapon.ContainsKey(weaponData))
        {
            dictItemWeapon[weaponData].UpdateUI();
        }
    }
    public void ShowGameComplete()
    {
        rectGamecomplete.gameObject.SetActive(true);
        RectTransform rect = rectGamecomplete.GetChild(0).GetComponent<RectTransform>();
        rectGamecomplete.DOScale(1, 0.75f);
        rect.DOScale(1.4f, 0.5f);
        rect.GetComponent<TextMeshProUGUI>().DOFade(0, 0.5f);
    }

    public void ResetUI()
    {
        RectTransform rect = rectGamecomplete.GetChild(0).GetComponent<RectTransform>();
        rect.DOScale(1f, 0);
        rectGamecomplete.DOScale(0.2f, 0);
        rect.GetComponent<TextMeshProUGUI>().DOFade(1, 0.5f);
        rectGamecomplete.gameObject.SetActive(false);
    }

    public void ShowGameLose()
    {
        rectGameLose.gameObject.SetActive(true);
        RectTransform rect = rectGameLose.GetChild(0).GetComponent<RectTransform>();
        rectGameLose.DOScale(1, 0.75f);
        rect.DOScale(1.4f, 0.5f);
        rect.GetComponent<TextMeshProUGUI>().DOFade(0, 0.5f);
    }

    public void ResetUILose()
    {
        RectTransform rect = rectGameLose.GetChild(0).GetComponent<RectTransform>();
        rect.DOScale(1f, 0);
        rectGameLose.DOScale(0.2f, 0);
        rect.GetComponent<TextMeshProUGUI>().DOFade(1, 0.5f);
        rectGameLose.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        Observer.OnUpdateItemWeapon += UpdateItemWeapon;
        Observer.OnSellectWeapon += OnSellectItem;
    }

    void OnDisable()
    {
        Observer.OnUpdateItemWeapon -= UpdateItemWeapon;
        Observer.OnSellectWeapon -= OnSellectItem;
    }
}
