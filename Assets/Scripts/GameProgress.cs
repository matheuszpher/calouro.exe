/// <summary>
/// Guarda o progresso que precisa sobreviver entre telas/áreas na sessão de jogo.
/// (Placeholder do MVP — ainda não salva em disco.)
/// </summary>
public static class GameProgress
{
    /// <summary>Nota de Matemática Básica (0–10). -1 = ainda não fez a prova.</summary>
    public static float MathGrade = -1f;

    /// <summary>Nome do calouro (definido na tela de título).</summary>
    public static string PlayerName = "Calouro";
}
