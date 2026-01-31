using System.Collections;
using UnityEngine;
using CandyCoded.HapticFeedback;

public enum eGameState
{
    None,
    Playing,
    GameWin,
    GameOver
}
public class GameController : Singleton<GameController>
{

    public eGameState State => currentState;

    public int CurrentLevel => currentLevel;

    public bool isStop;
    [SerializeField] private int currentLevel = 1;

    [SerializeField] private float timeLoadLevel;

    [SerializeField] private float timeWaitForDelay;

    [SerializeField] private float timeScale;

    private CanvasGameplay canvasGameplay;
    private Level level;

    private GameSetting gameSetting;

    private eGameState currentState;
    void Awake()
    {
        gameSetting = Resources.Load<GameSetting>(GameConstants.KEY_DATA_GAME_SETTING);
    }
    void Start()
    {
        canvasGameplay = UIManager.Instance.OpenUI<CanvasGameplay>();
        StartCoroutine(Playing(true));
    }
    public void SetState(eGameState newState)
    {
        if (newState == currentState) return;
        if (newState == eGameState.GameWin && currentState == eGameState.GameOver) return;
        if (newState == eGameState.GameOver && currentState == eGameState.GameWin) return;
        currentState = newState;
        switch (currentState)
        {
            case eGameState.GameWin:
                GameComplete();
                break;
            case eGameState.GameOver:
                ReplayGame();
                break;
        }
    }
    public void GameComplete()
    {
        SoundManager.Instance.PlaySound(eAudioName.Audio_Complete);
        currentLevel++;
        WeaponManager.Instance.SetWeapon(currentLevel);
        canvasGameplay.ShowGameComplete();
        StartCoroutine(Playing(false));
        //Invoke(nameof(LoadLevel), timeLoadLevel);
    }

    public void ReplayGame()
    {
        canvasGameplay.ShowGameLose();
        StartCoroutine(Playing(false));
        //Invoke(nameof(LoadLevel), timeLoadLevel);
    }
    IEnumerator Playing(bool isStart)
    {
        if (!isStart)
        {

            yield return new WaitForSeconds(timeLoadLevel);
        }
        else
        {
            yield return null;
        }
        canvasGameplay.UpdateLevel(currentLevel);
        if (currentState == eGameState.GameWin)
        {
            canvasGameplay.ResetUI();
        }
        else if (currentState == eGameState.GameOver)
        {
            canvasGameplay.ResetUILose();
        }
        if (level != null)
        {
            Observer.OnDespawnObject?.Invoke();
            level.ResetLevel();
            Destroy(level.gameObject);
            level = null;
        }
        currentState = eGameState.Playing;
        level = Resources.Load<Level>(GameConstants.KEY_LEVEL + currentLevel);
        level = Instantiate(level);
    }
    public void DelayGame()
    {
        StartCoroutine(WaitForDelay());
    }

    IEnumerator WaitForDelay()
    {
        // 1. Set Slow Motion
        Time.timeScale = timeScale; // Ví dụ: 0.2f

        // [QUAN TRỌNG] Điều chỉnh Physics để chuyển động mượt mà khi slow motion
        // Mặc định fixedDeltaTime là 0.02f
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        // 2. Đợi theo thời gian thực (Realtime)
        // Dùng cái này thì dù timeScale = 0.1 hay = 0 thì nó vẫn đếm đúng giây
        yield return new WaitForSecondsRealtime(timeWaitForDelay);

        // 3. Trả về bình thường
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f; // Trả lại mặc định của Unity
    }

    public void OnWardrobe(bool isOpen)
    {
        isStop = isOpen;
    }

    public void Vibrate()
    {
        if (gameSetting != null && !gameSetting.isVibrate)
        {
            return;
        }
        HapticFeedback.LightFeedback();
    }
    void OnEnable()
    {
        Observer.OnOpenWardrobe += OnWardrobe;
    }

    void OnDisable()
    {
        Observer.OnOpenWardrobe -= OnWardrobe;
    }
}
