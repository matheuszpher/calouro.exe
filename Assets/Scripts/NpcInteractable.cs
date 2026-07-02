using UnityEngine;

/// <summary>
/// NPC com quem o jogador pode conversar. Precisa de um Collider2D marcado como
/// "Is Trigger" (o montador adiciona um CircleCollider2D). Quando o jogador
/// (tag "Player") entra no raio, avisa o DialogueManager para mostrar a dica;
/// o E dispara o diálogo.
/// </summary>
public class NpcInteractable : MonoBehaviour
{
    public string npcName = "NPC";

    [Tooltip("Identificador usado pela quest (ex.: coordenador, natan).")]
    public string npcId = "";

    [TextArea(2, 5)]
    public string[] lines;

    [Header("Escolha opcional ao fim da fala (flavor — sem efeito mecânico)")]
    public bool hasChoice;
    public string choiceQuestion;
    public string choiceOptionA;
    public string choiceOptionB;
    [TextArea(1, 3)] public string choiceReplyA;
    [TextArea(1, 3)] public string choiceReplyB;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.SetNearbyNpc(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.ClearNearbyNpc(this);
    }
}
