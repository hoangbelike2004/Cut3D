using UnityEngine;
using UnityEngine.UI;

public class CanvasGameSetting : UICanvas
{
    [SerializeField] Button btnSound, btnVibrate, btnClose;

    [SerializeField] RectTransform vibrateOf, vibrateOn;
    [SerializeField] RectTransform soundOf, soundOn;

    private GameSetting gameSetting;
    void Awake()
    {
        gameSetting = Resources.Load<GameSetting>(GameConstants.KEY_DATA_GAME_SETTING);
    }
    void Start()
    {
        vibrateOn.gameObject.SetActive(gameSetting.isVibrate);
        vibrateOf.gameObject.SetActive(!gameSetting.isVibrate);
        btnVibrate.onClick.AddListener(() =>
        {
            if (gameSetting == null) return;
            gameSetting.isVibrate = !gameSetting.isVibrate;
            vibrateOn.gameObject.SetActive(gameSetting.isVibrate);
            vibrateOf.gameObject.SetActive(!gameSetting.isVibrate);
        });
        btnSound.onClick.AddListener(() =>
        {
            if (gameSetting == null) return;
            gameSetting.isSound = !gameSetting.isSound;
            soundOn.gameObject.SetActive(gameSetting.isSound);
            soundOf.gameObject.SetActive(!gameSetting.isSound);
        });
        btnClose.onClick.AddListener(() =>
        {
            UIManager.Instance.CloseUI<CanvasGameSetting>(0);
        });
    }


}
