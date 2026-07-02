using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Portal no campus que inicia a Prova-Labirinto. Chega perto → dica; aperta E.
/// Precisa de um Collider2D "Is Trigger".
/// </summary>
public class MazePortal : MonoBehaviour
{
    [Tooltip("Para onde o jogador volta ao terminar a prova.")]
    public Vector3 returnPosition = new Vector3(0f, -2f, 0f);

    private bool near;

    private bool ExamTime => QuestManager.Instance != null && QuestManager.Instance.IsCurrent("prova_mat");

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        near = true;
        if (ExamTime)
            DialogueManager.Instance?.ShowActionHint("Aperte E para fazer a Prova de Matemática (Labirinto)");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        near = false;
        DialogueManager.Instance?.HideActionHint();
    }

    private void Update()
    {
        if (!near || MazeController.InMaze || DialogueManager.IsActive || TitleScreen.IsShowing) return;
        // Só é a Prova de Matemática quando o objetivo dela está ativo (pós time skip).
        if (!ExamTime) return;

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
        {
            DialogueManager.Instance?.HideActionHint();
            MazeController.Instance?.StartMaze(returnPosition);
        }
    }
}
