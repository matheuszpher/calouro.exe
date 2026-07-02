using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Tapete de saída de uma sala. Por padrão, ao pisar o jogador já volta ao
/// campus (comportamento de pilha do InteriorController). Se requireInteract
/// estiver marcado, só sai apertando E (como uma porta) em vez de sair
/// automaticamente ao pisar — usado onde o ponto de entrada da sala fica perto
/// demais do tapete e o auto-disparo expulsava o jogador no mesmo instante em
/// que entrava (ver sala de aula).
/// Se useOverridePosition estiver marcado, ignora a pilha e sai sempre em
/// overridePosition — usado pelos blocos-túnel, onde o lado por onde se sai
/// (norte/sul) não depende do lado por onde se entrou.
/// Precisa de um Collider2D "Is Trigger".
/// </summary>
public class RoomExit : MonoBehaviour
{
    public bool useOverridePosition;
    public Vector3 overridePosition;

    [Tooltip("Se marcado, só sai apertando E (em vez de sair automaticamente ao pisar).")]
    public bool requireInteract;

    private bool near;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!requireInteract) { Exit(); return; }
        near = true;
        DialogueManager.Instance?.ShowActionHint("Aperte E para sair");
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        near = false;
        DialogueManager.Instance?.HideActionHint();
    }

    private void Update()
    {
        if (!requireInteract || !near) return;
        if (DialogueManager.IsActive || TitleScreen.IsShowing) return;

        var kb = Keyboard.current;
        if (kb != null && kb.eKey.wasPressedThisFrame)
        {
            DialogueManager.Instance?.HideActionHint();
            near = false;
            Exit();
        }
    }

    private void Exit()
    {
        if (useOverridePosition)
            InteriorController.Instance?.ExitRoomTo(overridePosition);
        else
            InteriorController.Instance?.ExitRoom();
    }
}
