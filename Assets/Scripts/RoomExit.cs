using UnityEngine;

/// <summary>
/// Tapete de saída de uma sala: ao pisar, o jogador volta ao campus.
/// Precisa de um Collider2D "Is Trigger".
/// </summary>
public class RoomExit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            InteriorController.Instance?.ExitRoom();
    }
}
