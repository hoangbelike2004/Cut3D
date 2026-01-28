using System.Collections;
using UnityEngine;

public enum eGameState
{
    None,
    Home,
    Playing,
    GameWin,
    GameOver
}
public class GameController : Singleton<GameController>
{

    public eGameState State => currentState;
    [SerializeField] private int currentLevel = 1;

    [SerializeField] private float timeLoadLevel;

    [SerializeField] private float timeWaitForDelay;

    [SerializeField] private float timeScale;

    private CanvasGameplay canvasGameplay;
    private Level level;

    private eGameState currentState;
    void Start()
    {
        canvasGameplay = UIManager.Instance.OpenUI<CanvasGameplay>();
        StartCoroutine(Playing(true));
    }
    public void SetState(eGameState newState)
    {
        if (newState == currentState) return;

        currentState = newState;
        switch (currentState)
        {
            case eGameState.Home:
                break;
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
        currentLevel++;
        canvasGameplay.ShowGameComplete();
        StartCoroutine(Playing(false));
        //Invoke(nameof(LoadLevel), timeLoadLevel);
    }

    public void ReplayGame()
    {
        StartCoroutine(Playing(false));
        //Invoke(nameof(LoadLevel), timeLoadLevel);
    }
    IEnumerator Playing(bool isStart)
    {
        if (!isStart)
        {
            yield return new WaitForSeconds(timeLoadLevel / 2);
            Observer.OnDespawnObject?.Invoke();
            yield return new WaitForSeconds(timeLoadLevel / 2);
        }
        else
        {
            yield return null;
        }
        canvasGameplay.UpdateLevel(currentLevel);
        canvasGameplay.ResetUI();
        if (level != null)
        {
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
}
