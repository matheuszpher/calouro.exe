using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Porta de um prédio: quando o jogador chega perto e aperta E, entra na sala
/// (troca de tela via InteriorController). Precisa de um Collider2D "Is Trigger".
/// </summary>
public class BuildingDoor : MonoBehaviour
{
    public Vector3 roomSpawn;         // onde o jogador aparece dentro da sala
    public Vector3 returnPosition;    // onde volta ao sair (na frente do prédio)
    public Vector2 roomBoundsMin;
    public Vector2 roomBoundsMax;
    public string roomLabel = "sala";

    private bool near;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        near = true;
        DialogueManager.Instance?.ShowActionHint($"Aperte E para entrar — {roomLabel}");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        near = false;
        DialogueManager.Instance?.HideActionHint();
    }

    private void Update()
    {
        if (!near || InteriorController.InRoom || MazeController.InMaze
            || DialogueManager.IsActive || TitleScreen.IsShowing || QuestManager.IsGameOver) return;

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
        {
            DialogueManager.Instance?.HideActionHint();
            InteriorController.Instance?.EnterRoom(roomSpawn, returnPosition, roomBoundsMin, roomBoundsMax);
        }
    }
}
