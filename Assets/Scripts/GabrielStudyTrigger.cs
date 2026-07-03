using UnityEngine;

/// <summary>
/// Efeito da escolha de ajudar (ou não) o Gabriel/Gabriela a estudar (SQ2, Dia 32,
/// roadmap 3.10). Não usa NpcInteractable.onChoiceA/onChoiceB: aquele mecanismo é um
/// delegate puro (System.Action), que a Unity não sabe serializar — quando o Editor
/// monta a cena (TopDownSceneBuilder) e atribui a lambda em tempo de Edição, ela vive
/// só na instância em memória daquele momento. Ao entrar em Play Mode a Unity faz um
/// domain reload (recompila e reconstrói os componentes a partir só do que foi
/// serializado), então qualquer delegate atribuído por fora do próprio script do
/// componente se perde (vira null) — a escolha "Bora, vamos estudar" parecia não
/// fazer nada. Mesma solução já usada pro pingue-pongue do Vitim
/// (VitimPingPongTrigger): a lógica mora num componente próprio, compilado junto com
/// o resto do assembly, então sobrevive ao reload igual qualquer outro código.
/// </summary>
public class GabrielStudyTrigger : MonoBehaviour
{
    public void Accepted()
    {
        GameProgress.SetFlag("gabriel_ajudado");
        QuestManager.Instance?.StartRevisaoGeral();
    }

    public void Declined()
    {
        GameProgress.SetFlag("gabriel_recusado");
        // Sem revisão pra esperar — dispara a virada de calendário na hora (ver
        // o objetivo "final_day_gate" em QuestManager.cs pra entender por que o
        // salto pro Dia 100 não pode morar direto no fim da conversa).
        QuestManager.Instance?.ForceComplete("final_day_gate");
    }
}
