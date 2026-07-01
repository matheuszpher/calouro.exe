using UnityEngine;

/// <summary>
/// Zona de chegada (Bloco 1). Quando o jogador entra, avisa a quest.
/// Precisa de um Collider2D marcado como "Is Trigger".
/// </summary>
public class GoalZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnReachedGoal();
    }
}
