using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    public List<Enemy> enemies = new List<Enemy>();
    public List<GameObject> interacable = new List<GameObject>();
    CameraFlow cameraFlow;

    public void SetPlayer(PlayerController player)
    {
        cameraFlow = Camera.main.GetComponent<CameraFlow>();
        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].SetTarget(player.transform);
            cameraFlow.AddEnemyFlow(enemies[i].objectCamFolow);
            cameraFlow.SetPosCam(player.transform.position);
        }

    }
    public void AddEnemy(Enemy enemy)
    {
        if (enemies.Contains(enemy)) return;
        enemies.Add(enemy);
    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (enemies.Contains(enemy))
        {
            ChangeStateOfLevel();
            enemies.Remove(enemy);
            cameraFlow.RemovEnemyFlow(enemy.objectCamFolow);
        }
        if (enemies.Count == 0)
        {
            GameController.Instance.SetState(eGameState.GameWin);
        }
    }
    public void ChangeStateOfLevel()
    {
        switch (GameController.Instance.CurrentLevel)
        {
            case 13:
                // Thay đổi trạng thái của level 13
                interacable[0].gameObject.SetActive(true);
                break;
            case 14:
                // Thay đổi trạng thái của level 14
                if (interacable.Count > 0)
                {
                    interacable[0].gameObject.SetActive(true);
                    interacable.RemoveAt(0);
                }
                break;
        }
    }

    public void ResetLevel()
    {
        cameraFlow.Clear();
    }
}
