/// <summary>
/// Guarda o progresso que precisa sobreviver entre telas/áreas na sessão de jogo.
/// (Placeholder do MVP — ainda não salva em disco.)
/// </summary>
public static class GameProgress
{
    /// <summary>Nota de Matemática Básica (0–10). -1 = ainda não fez a prova.</summary>
    public static float MathGrade = -1f;

    /// <summary>Notas das demais provas (0–10). -1 = ainda não avaliado.</summary>
    public static float FupGrade = -1f;
    public static float IhcGrade = -1f;
    public static float IesGrade = -1f;

    /// <summary>Nome do calouro (definido na tela de título).</summary>
    public static string PlayerName = "Calouro";

    /// <summary>
    /// Personagem escolhido na tela de título: "calouro" (homem) ou "caloura"
    /// (mulher). Define qual folha de sprites o Player usa (ver PlayerAppearance).
    /// </summary>
    public static string PlayerCharacter = "calouro";

    /// <summary>
    /// Se a cutscene de abertura (Jeferson mostrando o campus) já rodou nesta
    /// sessão. Impede que ela repita ao recarregar a cena (ex.: volta de minigame).
    /// </summary>
    public static bool CampusTourSeen = false;

    /// <summary>
    /// Id do objetivo atual do jogador (sistema sequencial em QuestManager).
    /// Sobrevive a trocas de cena na sessão; "" = nenhum objetivo ativo.
    /// </summary>
    public static string CurrentObjectiveId = "";

    /// <summary>Nota/progresso de Ética (0–10), construída por interações sociais.</summary>
    public static float EthicsGrade = 0f;

    /// <summary>Dia atual (começa no 1). Avança em AdvanceDay (time skip vem depois).</summary>
    public static int CurrentDay = 1;

    /// <summary>
    /// Dia absoluto do semestre (1–100), fonte única do contador "faltam N dias" no
    /// HUD e da semana derivada na caderneta. Diferente de CurrentDay (que só conta
    /// dias jogáveis em sequência): SemesterDay salta de acordo com o calendário do
    /// jogo (ver roadmap-v2.md, seção 3.1 — Calendário dos 100 dias) sempre que um
    /// time skip acontece.
    /// </summary>
    public static int SemesterDay = 1;

    /// <summary>Duração do semestre em dias, pro contador regressivo do HUD.</summary>
    public const int SemesterTotalDays = 100;

    /// <summary>Ética já ganha no dia atual (zera a cada dia) — sustenta o teto diário.</summary>
    public static float EthicsGainedToday = 0f;

    /// <summary>Teto de ganho de Ética por dia, pra a nota 10 só vir com o acúmulo de vários dias.</summary>
    public const float EthicsDailyCap = 2.0f;

    /// <summary>
    /// Concede Ética respeitando o teto diário e o máximo 10. Retorna quanto foi
    /// realmente concedido (0 se já bateu o teto do dia ou a nota máxima).
    /// </summary>
    public static float AddEthics(float delta)
    {
        if (delta <= 0f) return 0f;
        float roomDay = EthicsDailyCap - EthicsGainedToday;
        if (roomDay < 0f) roomDay = 0f;
        float granted = delta < roomDay ? delta : roomDay;
        float toMax = 10f - EthicsGrade;
        if (granted > toMax) granted = toMax;
        if (granted <= 0f) return 0f;
        EthicsGrade += granted;
        EthicsGainedToday += granted;
        return granted;
    }

    /// <summary>Avança um dia (zera o teto diário de Ética). Usado no fim de cada dia.</summary>
    public static void AdvanceDay()
    {
        CurrentDay++;
        SemesterDay++;
        EthicsGainedToday = 0f;
    }

    /// <summary>
    /// Salta SemesterDay pra um valor absoluto (usado nos time skips do calendário
    /// dos 100 dias). Nunca recua, pra um save/replay não bagunçar o contador.
    /// </summary>
    public static void JumpSemesterDayTo(int day)
    {
        if (day > SemesterDay) SemesterDay = day;
    }

    /// <summary>
    /// Flags narrativas em snake_case PT-BR (ex.: "etica_emilly", "trote_escapou").
    /// Usadas para consequências e para não repetir efeitos únicos.
    /// </summary>
    public static readonly System.Collections.Generic.HashSet<string> Flags =
        new System.Collections.Generic.HashSet<string>();

    public static bool HasFlag(string flag) => Flags.Contains(flag);
    public static void SetFlag(string flag) => Flags.Add(flag);

    /// <summary>Remove uma flag (usado por efeitos de duração limitada, ex.: cheiro do trote só no dia).</summary>
    public static void ClearFlag(string flag) => Flags.Remove(flag);

    /// <summary>
    /// Zera o progresso pra um novo jogo de verdade (os campos são estáticos e
    /// sobrevivem a um SceneManager.LoadScene normal, então "Novo Jogo" sozinho
    /// não limpa isso). PlayerName/PlayerCharacter não precisam ser zerados
    /// aqui: a TitleScreen já pergunta os dois de novo em toda tela de "Novo Jogo".
    /// Ainda sem chamador (a mecânica que ia usar isso — créditos de fim de jogo —
    /// foi cortada em 04/07/2026); mantido pronto pro dia que precisar de um
    /// reset de verdade.
    /// </summary>
    public static void Reset()
    {
        MathGrade = FupGrade = IhcGrade = IesGrade = -1f;
        EthicsGrade = 0f;
        EthicsGainedToday = 0f;
        CampusTourSeen = false;
        CurrentObjectiveId = "";
        CurrentDay = 1;
        SemesterDay = 1;
        Flags.Clear();
    }
}
