using UnityEngine;

public class GameController : Singleton<GameController>
{
    [SerializeField] private int currentLevel = 1;

    [SerializeField] private float timeLoadLevel;
    private Level level;
    void Start()
    {
        LoadLevel();
    }

    public void GameComplete()
    {
        currentLevel++;

        Invoke(nameof(LoadLevel), timeLoadLevel);
    }

    public void ReplayGame()
    {
        Invoke(nameof(LoadLevel), timeLoadLevel);
    }
    public void LoadLevel()
    {
        //destroy level cũ
        if (level != null)
        {
            Destroy(level.gameObject);
            level = null;
        }
        level = Resources.Load<Level>(GameConstants.KEY_LEVEL + currentLevel);
        level = Instantiate(level);
    }
}
