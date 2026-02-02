using UnityEngine;

public class Iron : MonoBehaviour
{
    void OnCollisionEnter(Collision other)
    {
        if (other.collider.CompareTag("Player"))
        {
            if (GameController.Instance != null) GameController.Instance.SetState(eGameState.GameOver);
        }
    }
}
