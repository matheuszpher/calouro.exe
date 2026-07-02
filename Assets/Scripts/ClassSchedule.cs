/// <summary>
/// Guarda qual sala de aula é a "correta" no momento. Por enquanto é um valor
/// fixo (só a sala da Rainara libera, ver TopDownSceneBuilder) — quando o fluxo
/// real de horário/aulas entrar no jogo, ele passa a atualizar isso em vez de
/// deixar fixo aqui.
/// </summary>
public static class ClassSchedule
{
    public static string CurrentRoomId = "BLOCO 1 (001) — Sala 1";
    public static string CurrentRoomLabel = "IHC com a Rainara (Bloco 1, Sala 1)";
}
