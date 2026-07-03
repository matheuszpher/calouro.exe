using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dia 100 (fim do semestre, roadmap 3.1/3.11): o Jeferson revisa as 5 notas do
/// aluno uma a uma (com comentário por nota), tira a média e decide entre
/// reprovação (média &lt;4), avaliação final (4–6,9 — aprova a partir de 6 na
/// média da própria avaliação, reaproveitando as provas ORIGINAIS pra não
/// repetir o conteúdo de revisão já usado na SQ2 do Gabriel) ou aprovação
/// direta (≥7), terminando com o Jeferson desejando algo (sucesso/mais
/// dedicação/boa sorte) conforme o resultado. Aí sim o jogo acaba: tela preta
/// fixa (DayTransition.PlayFinal) com a equipe e um agradecimento, tocando a
/// música dos créditos — sem cutscene de ônibus (cortada em 04/07/2026, ver
/// roadmap-v2.md §2) e sem voltar ao jogo: a tela trava ali de propósito.
/// Ativado pelo objetivo "final_day_review" (QuestManager.cs).
/// </summary>
public class FinalDayDirector : MonoBehaviour
{
    public static FinalDayDirector Instance { get; private set; }

    [Tooltip("Música dos créditos finais, atribuída pelo TopDownSceneBuilder (SetupQuest).")]
    public AudioClip endingMusic;

    // Nomes exibidos na tela final, junto do agradecimento — decisão de
    // 04/07/2026: sem cutscene/rolagem de créditos, só uma tela preta fixa
    // (mesmo estilo do DayTransition) que trava o jogo de propósito.
    private const string CreditsMessage =
        "CALOURO.EXE\n\n" +
        "Natan Lucena\n" +
        "Victor Veras Martins\n" +
        "Emilly Paiva Belo\n" +
        "Enzo Hariel\n" +
        "Matheus Rodrigues\n\n" +
        "Obrigado pela atenção!";

    // Mesma posição de abordagem usada pelo Jeferson na abertura do Dia 1
    // (CampusTourCutscene.approachTarget) — fecha o ciclo narrativo no mesmo
    // lugar onde tudo começou. O jogador reaparece em (-6, 18.5) por conta do
    // DayTransition (campusSpawn), então o Jeferson fica um pouco ao sul dele.
    private static readonly Vector3 JefersonSpot = new Vector3(-6f, 15.5f, 0f);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void BeginFinalDay() => StartCoroutine(Run());

    private IEnumerator Run()
    {
        // Roda enquanto a tela do time skip ainda está preta (chamado de dentro
        // do onMidpoint do DayTransition) — o teleporte do Jeferson fica
        // invisível de propósito, igual o truque já usado com o Aragão (SQ1).
        PositionJefersonOnPassarela();

        while (DayTransition.Active) yield return null; // espera a tela clarear de vez
        yield return new WaitForSeconds(0.3f);

        var subjects = new (string label, float grade)[]
        {
            ("Fundamentos da Programação", GameProgress.FupGrade),
            ("Interação Humano-Computador", GameProgress.IhcGrade),
            ("Ética", GameProgress.EthicsGrade),
            ("Matemática Básica", GameProgress.MathGrade),
            ("Introdução à Engenharia de Software", GameProgress.IesGrade),
        };

        var lines = new List<string>
        {
            $"Ei, {GameProgress.PlayerName}! Não acredito que já chegamos ao fim do semestre.",
            "Antes de você ir, deixa eu dar uma olhada em como você se saiu...",
        };
        float sum = 0f;
        foreach (var (label, grade) in subjects)
        {
            sum += grade;
            lines.Add(CommentFor(label, grade));
        }
        float average = sum / subjects.Length;
        lines.Add($"No total, sua média final ficou em {average:0.0}.");

        DialogueManager.Instance.StartDialogue("Jeferson", lines.ToArray());
        while (!DialogueManager.IsActive) yield return null;
        while (DialogueManager.IsActive) yield return null;

        yield return RunOutcome(average);

        QuestManager.Instance?.ForceComplete("final_day_review");

        // Fim de jogo: tela preta fixa com a equipe + agradecimento, tocando a
        // música dos créditos — sem cutscene, sem pular dia, sem fechar o jogo.
        // DayTransition.Active fica true pra sempre a partir daqui, o que já
        // trava sozinho o movimento do jogador, diálogo e caderneta (todos
        // esses sistemas já checam essa flag).
        DayTransition.Instance?.PlayFinal(CreditsMessage, endingMusic);
    }

    private static string CommentFor(string subject, float grade)
    {
        bool isEthics = subject == "Ética";
        if (grade >= 9f)
            return isEthics
                ? $"Um {grade:0.0} em Ética! Muito íntegro da sua parte."
                : $"Uau, um {grade:0.0} em {subject}! Você é muito bom mesmo.";
        if (grade >= 7f)
            return isEthics
                ? $"{grade:0.0} em Ética — você levou os valores a sério. Parabéns."
                : $"{grade:0.0} em {subject}, muito bem! Mandou bem.";
        if (grade >= 5f)
            return isEthics
                ? $"Hmm, {grade:0.0} em Ética... dava pra ter sido um pouco mais cuidadoso."
                : $"{grade:0.0} em {subject}... deu pra passar, mas dava pra ser melhor.";
        return isEthics
            ? $"Nossa, que antiético, um {grade:0.0} em Ética!"
            : $"Um {grade:0.0} em {subject}? Isso me preocupa um pouco...";
    }

    /// <summary>
    /// Ramifica pelo resultado e termina com o Jeferson desejando algo —
    /// "sucesso" (aprovado direto), "boa sorte" (passou pela avaliação final,
    /// em qualquer um dos dois sentidos) ou "mais dedicação" (reprovado) — sem
    /// nenhuma cutscene depois disso (decisão de 04/07/2026).
    /// </summary>
    private IEnumerator RunOutcome(float average)
    {
        if (average < 4f)
        {
            yield return SayJeferson("Poxa... sua média não foi suficiente pra passar. Você está reprovado no semestre.");
            yield return SayJeferson("Da próxima vez, um pouco mais de dedicação, tá bom? Eu confio em você.");
        }
        else if (average < 7f)
        {
            yield return SayJeferson("Sua média ficou na média — vamos fazer uma avaliação final pra confirmar o que você aprendeu.");

            bool battleDone = false, battlePassed = false;
            yield return StartCoroutine(AvaliacaoFinalBattery(passed => { battlePassed = passed; battleDone = true; }));
            while (!battleDone) yield return null;

            yield return SayJeferson(battlePassed
                ? "Boa! Você mostrou que aprendeu o suficiente. Aprovado!"
                : "Poxa, não foi dessa vez... você está reprovado no semestre.");
            yield return SayJeferson("De qualquer forma, boa sorte daqui pra frente!");
        }
        else
        {
            yield return SayJeferson("Excelente semestre! Você está aprovado, e com méritos. Desejo muito sucesso daqui pra frente!");
        }
    }

    private IEnumerator SayJeferson(string line)
    {
        DialogueManager.Instance.StartDialogue("Jeferson", new[] { line });
        while (!DialogueManager.IsActive) yield return null;
        while (DialogueManager.IsActive) yield return null;
    }

    /// <summary>
    /// "Outra bateria de exercícios" (avaliação final) — reaproveita as provas
    /// ORIGINAIS (não o conteúdo de revisão da SQ2 do Gabriel, pra não repetir
    /// pergunta): quiz de IES, 2 labirintos de Matemática, problema de FUP.
    /// Aprova se a média das 3 partes fechar em 6 ou mais.
    /// </summary>
    private IEnumerator AvaliacaoFinalBattery(System.Action<bool> onDone)
    {
        QuestManager.Instance?.ShowMessage("Avaliação final: quiz de IES...");
        yield return new WaitForSeconds(0.8f);
        bool quizDone = false; float quizGrade = 0f;
        ExamManager.Instance?.StartQuiz(g => { quizGrade = g; quizDone = true; });
        while (!quizDone) yield return null;

        yield return new WaitForSeconds(0.5f);
        QuestManager.Instance?.ShowMessage("Avaliação final: labirinto de Matemática...");
        yield return new WaitForSeconds(0.8f);
        var player = GameObject.FindWithTag("Player");
        Vector3 returnPos = player != null ? player.transform.position : Vector3.zero;
        bool mazeDone = false; float mazeGrade = 0f;
        MazeController.Instance?.StartMaze(returnPos, g => { mazeGrade = g; mazeDone = true; }, 2);
        while (!mazeDone) yield return null;

        yield return new WaitForSeconds(0.5f);
        QuestManager.Instance?.ShowMessage("Avaliação final: exercício de FUP...");
        yield return new WaitForSeconds(0.8f);
        bool fupDone = false; float fupGrade = 0f;
        ExamManager.Instance?.StartProblem(g => { fupGrade = g; fupDone = true; });
        while (!fupDone) yield return null;

        float battAvg = (quizGrade + mazeGrade + fupGrade) / 3f;
        onDone(battAvg >= 6f);
    }

    private void PositionJefersonOnPassarela()
    {
        var jeferson = FindNpcById("coordenador");
        if (jeferson == null) return;
        jeferson.gameObject.SetActive(true);
        jeferson.transform.position = JefersonSpot;
        jeferson.transform.localScale = Vector3.one;
        var col = jeferson.GetComponent<Collider2D>();
        if (col != null) col.enabled = false; // fala automática, sem precisar de "E"
        var anim = jeferson.GetComponent<SpriteWalkAnimator>();
        if (anim != null) anim.LockFacing(Vector2.up); // o jogador reaparece ao norte (passarela, y=18.5)
    }

    private static NpcInteractable FindNpcById(string npcId)
    {
        foreach (var n in Object.FindObjectsByType<NpcInteractable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (n.npcId == npcId) return n;
        return null;
    }
}
