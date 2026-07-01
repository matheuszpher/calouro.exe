using UnityEngine;

/// <summary>Saída do labirinto. Precisa de um Collider2D "Is Trigger".</summary>
public class MazeExit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (MazeController.Instance != null)
            MazeController.Instance.Finish();
    }
}
