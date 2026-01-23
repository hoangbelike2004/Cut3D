using System.Collections.Generic;
using UnityEngine;

public class Level : MonoBehaviour
{
    public List<Enemy> enemies = new List<Enemy>();

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
            enemies.Remove(enemy);
            cameraFlow.RemovEnemyFlow(enemy.objectCamFolow);
        }
        if (enemies.Count == 0)
        {
            GameController.Instance.GameComplete();
        }
    }

    public void ResetLevel()
    {
        cameraFlow.Clear();
    }
}
