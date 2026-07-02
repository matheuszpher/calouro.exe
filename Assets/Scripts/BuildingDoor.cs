using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Porta de um prédio: quando o jogador chega perto e aperta E, entra na sala
/// (troca de tela via InteriorController). Precisa de um Collider2D "Is Trigger".
/// Se classroomId estiver preenchido (usado pelas portas de sala de aula), só
/// entra quando bater com ClassSchedule.CurrentRoomId — caso contrário mostra um
/// pensamento do jogador em vez de entrar (fluxo de horário real vem depois).
/// </summary>
public class BuildingDoor : MonoBehaviour
{
    public Vector3 roomSpawn;         // onde o jogador aparece dentro da sala
    public Vector3 returnPosition;    // onde volta ao sair (na frente do prédio)
    public Vector2 roomBoundsMin;
    public Vector2 roomBoundsMax;
    public string roomLabel = "sala";

    [Tooltip("Se preenchido, só abre quando igual a ClassSchedule.CurrentRoomId (salas de aula).")]
    public string classroomId = "";

    [Tooltip("Escala do jogador dentro da sala (1 = normal). Volta ao normal ao sair.")]
    public float playerScale = 1f;

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
        // OBS: não bloqueia mais por InteriorController.InRoom — essa checagem
        // impedia entrar na sala a partir de dentro do corredor do bloco (que já
        // conta como "estar numa sala" na pilha), travando a porta da sala de aula.
        if (!near || MazeController.InMaze
            || DialogueManager.IsActive || TitleScreen.IsShowing || QuestManager.IsGameOver) return;

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
        {
            DialogueManager.Instance?.HideActionHint();

            if (!string.IsNullOrEmpty(classroomId) && classroomId != ClassSchedule.CurrentRoomId)
            {
                DialogueManager.Instance?.ShowThought(
                    $"Ops, sala errada. A sala correta de hoje é: {ClassSchedule.CurrentRoomLabel}");
                return;
            }

            // Entrar na sala de aula certa pode concluir um objetivo (ex.: "Ir para
            // a aula de IHC"). Portas comuns (classroomId vazio) não mexem no objetivo.
            if (!string.IsNullOrEmpty(classroomId))
                QuestManager.Instance?.OnEnteredRoom(classroomId);

            InteriorController.Instance?.EnterRoom(roomSpawn, returnPosition, roomBoundsMin, roomBoundsMax, playerScale);
        }
    }
}
