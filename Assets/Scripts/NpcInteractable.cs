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

    [System.Serializable]
    public class ObjectiveLineSet
    {
        public string objectiveId;
        [TextArea(2, 5)] public string[] lines;
    }

    [Tooltip("Falas alternativas conforme o objetivo atual (ex.: o mesmo professor falando coisas diferentes sobre uma side quest). Se nenhuma bater, usa 'lines' (1ª vez) ou 'repeatLines' (já conhecido).")]
    public ObjectiveLineSet[] objectiveLines;

    [System.Serializable]
    public class LineSet
    {
        [TextArea(2, 5)] public string[] lines;
    }

    [Tooltip("Conversas variadas pra quando o jogador já falou com este NPC antes (sorteia uma a cada conversa, em vez de repetir a apresentação de 'lines'). Vazio = repete 'lines' pra sempre.")]
    public LineSet[] repeatLines;

    private const string MetFlagPrefix = "conheceu_";

    /// <summary>Verdadeiro se o jogador já teve uma conversa (fora de quest) com este NPC antes.</summary>
    public bool AlreadyMet => !string.IsNullOrEmpty(npcId) && GameProgress.HasFlag(MetFlagPrefix + npcId);

    /// <summary>Marca que o jogador conheceu este NPC — a partir de agora, 'repeatLines' entra no lugar de 'lines'.</summary>
    public void MarkMet()
    {
        if (!string.IsNullOrEmpty(npcId)) GameProgress.SetFlag(MetFlagPrefix + npcId);
    }

    /// <summary>
    /// Fala a mostrar agora: a de 'objectiveLines' que bater com o objetivo atual
    /// (sempre vence, quest em andamento); senão, se já conhecido e houver
    /// 'repeatLines', sorteia uma delas; senão 'lines' (1ª conversa).
    /// </summary>
    public string[] CurrentLines()
    {
        if (objectiveLines != null && QuestManager.Instance != null)
            foreach (var set in objectiveLines)
                if (!string.IsNullOrEmpty(set.objectiveId) && QuestManager.Instance.IsCurrent(set.objectiveId))
                    return set.lines;

        if (AlreadyMet && repeatLines != null && repeatLines.Length > 0)
            return repeatLines[Random.Range(0, repeatLines.Length)].lines;

        return lines;
    }

    [Header("Escolha opcional ao fim da fala")]
    public bool hasChoice;
    public string choiceQuestion;
    public string choiceOptionA;
    public string choiceOptionB;
    [TextArea(1, 3)] public string choiceReplyA;
    [TextArea(1, 3)] public string choiceReplyB;

    [Header("Recompensa de Ética por escolha (0 = sem efeito). Concedida uma vez.")]
    public float ethicsRewardA = 0f;
    public float ethicsRewardB = 0f;

    [Header("Prova aplicada por este NPC (opcional)")]
    [Tooltip("Se este objetivo estiver ativo, falar com o NPC aplica a prova em vez da fala normal.")]
    public string examObjective = "";
    [Tooltip("Tipo de prova: ihc (nota fixa), ies (quiz) ou fup (montar solução).")]
    public string examKind = "";
    [TextArea(2, 5)] public string[] examLines;

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
