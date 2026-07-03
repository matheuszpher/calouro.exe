using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Aplica as provas interativas: quiz de IES (perguntas objetivas) e o "montar a
/// solução" de FUP (ordenar os passos de um algoritmo). Constrói a própria UI em
/// overlay e devolve a nota (0–10) por callback. Enquanto uma prova roda, o jogador
/// fica parado (Active). Conteúdo hardcoded, por decisão de escopo.
/// </summary>
public class ExamManager : MonoBehaviour
{
    public static ExamManager Instance { get; private set; }
    public static bool Active { get; private set; }

    private enum Mode { None, Quiz, Problem, Result }
    private Mode mode = Mode.None;

    // ---- Conteúdo das provas ----
    private class Question { public string text; public string[] options; public int correct; }

    private static readonly Question[] IesQuiz =
    {
        new Question { text = "O que é levantamento de requisitos?",
            options = new[] { "Entender o que o software precisa fazer", "Escrever todo o código de uma vez", "Escolher a cor da tela" }, correct = 0 },
        new Question { text = "Para que serve um teste de software?",
            options = new[] { "Deixar o programa mais bonito", "Verificar se ele funciona como esperado", "Aumentar o tamanho do arquivo" }, correct = 1 },
        new Question { text = "O que é manutenção de software?",
            options = new[] { "Trocar o computador", "Corrigir e melhorar o sistema depois de pronto", "Desligar o servidor" }, correct = 1 },
        new Question { text = "Trabalho em equipe na Engenharia de Software é...",
            options = new[] { "Cada um faz tudo escondido", "Dividir tarefas e se comunicar bem", "Algo sem importância" }, correct = 1 },
        new Question { text = "O que é um processo de software?",
            options = new[] { "Uma sequência de etapas pra desenvolver o sistema", "Um vírus", "Um tipo de hardware" }, correct = 0 },
    };

    private static readonly string[] FupSteps =
    {
        "Comece com o total valendo 0",
        "Repita para cada número de 1 até N",
        "Some o número atual ao total",
        "Ao terminar o laço, mostre o total",
    };
    private const string FupEnunciado = "Monte a solução: somar os números de 1 até N.";

    // Conteúdo da revisão geral do Gabriel/Gabriela (SQ2, Dia 32, roadmap 3.10) —
    // perguntas e passos NOVOS, distintos da prova oficial de Dia 20, pra não repetir.
    private static readonly Question[] ReviewQuiz =
    {
        new Question { text = "O que é um requisito não funcional?",
            options = new[] { "Uma qualidade do sistema, tipo desempenho ou segurança", "Uma função que o sistema executa", "Um tipo de banco de dados" }, correct = 0 },
        new Question { text = "Por que documentar decisões de projeto é importante?",
            options = new[] { "Não é importante", "Pra a equipe lembrar o porquê das escolhas depois", "Pra deixar o código mais lento" }, correct = 1 },
        new Question { text = "O que caracteriza um bom trabalho em equipe num projeto de software?",
            options = new[] { "Cada um programar sozinho, sem avisar os outros", "Comunicação e divisão clara de tarefas", "Evitar reuniões sempre" }, correct = 1 },
        new Question { text = "Pra que serve um controle de versão (como o Git)?",
            options = new[] { "Deixar o código mais bonito", "Aumentar a velocidade do processador", "Guardar o histórico de mudanças e permitir trabalho em conjunto" }, correct = 2 },
        new Question { text = "O que é um bug?",
            options = new[] { "Um novo recurso do sistema", "Um comportamento inesperado ou erro no software", "Um tipo de teste automatizado" }, correct = 1 },
    };

    private static readonly string[] ReviewFupSteps =
    {
        "Leia o número informado",
        "Divida o número por 2 e observe o resto",
        "Se o resto for 0, o número é par",
        "Caso contrário, o número é ímpar",
    };
    private const string ReviewFupEnunciado = "Monte a solução: verificar se um número é par ou ímpar.";

    // ---- Estado ----
    private System.Action<float> onDone;
    private Question[] activeQuiz;
    private string[] activeFupSteps;
    private string activeFupEnunciado;
    private int qIndex, correctCount;
    private int[] shuffled;          // ordem embaralhada exibida (índices dos passos corretos)
    private readonly List<int> picked = new List<int>(); // passos escolhidos pelo jogador (na ordem)
    private float resultHideAt = -1f;

    // ---- UI ----
    private GameObject panel;
    private Text titleText, bodyText, hintText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Active = false;
        BuildUI();
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; Active = false; }
    }

    // ------------------------------------------------------------------ API

    public void StartQuiz(System.Action<float> done) => BeginQuiz(IesQuiz, done);

    public void StartProblem(System.Action<float> done) => BeginProblem(FupSteps, FupEnunciado, done);

    /// <summary>Revisão geral do Gabriel/Gabriela (roadmap 3.10) — quiz "estilo Jeferson" com perguntas novas.</summary>
    public void StartReviewQuiz(System.Action<float> done) => BeginQuiz(ReviewQuiz, done);

    /// <summary>Revisão geral do Gabriel/Gabriela (roadmap 3.10) — problema "estilo Paulyne" com passos novos.</summary>
    public void StartReviewProblem(System.Action<float> done) => BeginProblem(ReviewFupSteps, ReviewFupEnunciado, done);

    private void BeginQuiz(Question[] quiz, System.Action<float> done)
    {
        onDone = done;
        activeQuiz = quiz;
        qIndex = 0; correctCount = 0;
        mode = Mode.Quiz;
        Active = true;
        Show();
        RenderQuestion();
    }

    private void BeginProblem(string[] steps, string enunciado, System.Action<float> done)
    {
        onDone = done;
        activeFupSteps = steps;
        activeFupEnunciado = enunciado;
        picked.Clear();
        shuffled = Shuffle(activeFupSteps.Length);
        mode = Mode.Problem;
        Active = true;
        Show();
        RenderProblem();
    }

    private void Update()
    {
        if (mode == Mode.Result)
        {
            if (resultHideAt > 0f && Time.unscaledTime >= resultHideAt)
            {
                resultHideAt = -1f;
                mode = Mode.None;
                Active = false;
                Hide();
                var cb = onDone; onDone = null;
                cb?.Invoke(lastGrade);
            }
            return;
        }

        if (mode == Mode.None) return;
        int pressed = ReadNumberKey();
        if (pressed < 0) return;

        if (mode == Mode.Quiz)
        {
            var q = activeQuiz[qIndex];
            if (pressed >= 1 && pressed <= q.options.Length)
            {
                if (pressed - 1 == q.correct) correctCount++;
                qIndex++;
                if (qIndex >= activeQuiz.Length)
                    Grade(10f * correctCount / activeQuiz.Length, $"Você acertou {correctCount} de {activeQuiz.Length}.");
                else
                    RenderQuestion();
            }
        }
        else if (mode == Mode.Problem)
        {
            if (pressed >= 1 && pressed <= shuffled.Length && !picked.Contains(pressed - 1))
            {
                picked.Add(pressed - 1);
                if (picked.Count >= shuffled.Length)
                {
                    // Nota: quantos passos ficaram na posição certa.
                    int ok = 0;
                    for (int pos = 0; pos < picked.Count; pos++)
                        if (shuffled[picked[pos]] == pos) ok++;
                    Grade(10f * ok / activeFupSteps.Length, $"Você acertou a ordem de {ok} de {activeFupSteps.Length} passos.");
                }
                else RenderProblem();
            }
        }
    }

    private float lastGrade;

    private void Grade(float grade, string detail)
    {
        lastGrade = Mathf.Round(Mathf.Clamp(grade, 0f, 10f) * 10f) / 10f;
        mode = Mode.Result;
        if (titleText != null) titleText.text = "Prova concluída!";
        if (bodyText != null) bodyText.text = $"{detail}\n\nNota: {lastGrade:0.0}";
        if (hintText != null) hintText.text = "";
        resultHideAt = Time.unscaledTime + 3.5f;
    }

    // ------------------------------------------------------------------ Render

    private void RenderQuestion()
    {
        var q = activeQuiz[qIndex];
        if (titleText != null) titleText.text = $"Prova de IES — pergunta {qIndex + 1}/{activeQuiz.Length}";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(q.text);
        sb.AppendLine();
        for (int i = 0; i < q.options.Length; i++)
            sb.AppendLine($"[{i + 1}] {q.options[i]}");
        if (bodyText != null) bodyText.text = sb.ToString();
        if (hintText != null) hintText.text = "Responda com as teclas 1, 2 ou 3.";
    }

    private void RenderProblem()
    {
        if (titleText != null) titleText.text = "Prova de FUP — monte a solução";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(activeFupEnunciado);
        sb.AppendLine();
        sb.AppendLine("Escolha os passos na ORDEM correta de execução:");
        sb.AppendLine();
        for (int i = 0; i < shuffled.Length; i++)
        {
            string mark = picked.Contains(i) ? $" (escolhido: {picked.IndexOf(i) + 1}º)" : "";
            sb.AppendLine($"[{i + 1}] {activeFupSteps[shuffled[i]]}{mark}");
        }
        if (bodyText != null) bodyText.text = sb.ToString();
        if (hintText != null) hintText.text = $"Aperte os números na ordem certa. Escolhidos: {picked.Count}/{shuffled.Length}";
    }

    // ------------------------------------------------------------------ Utils

    private static int[] Shuffle(int n)
    {
        var arr = new int[n];
        for (int i = 0; i < n; i++) arr[i] = i;
        for (int i = n - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        return arr;
    }

    private int ReadNumberKey()
    {
        var kb = Keyboard.current;
        if (kb == null) return -1;
        if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) return 1;
        if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) return 2;
        if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) return 3;
        if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) return 4;
        if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) return 5;
        return -1;
    }

    // ------------------------------------------------------------------ UI

    private void Show() { if (panel != null) panel.SetActive(true); }
    private void Hide() { if (panel != null) panel.SetActive(false); }

    private void BuildUI()
    {
        var canvasGO = new GameObject("ExamCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 106;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        panel = new GameObject("ExamPanel");
        panel.transform.SetParent(canvasGO.transform, false);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0.05f, 0.06f, 0.09f, 0.97f);
        var pRT = panel.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(0.15f, 0.2f);
        pRT.anchorMax = new Vector2(0.85f, 0.8f);
        pRT.offsetMin = Vector2.zero;
        pRT.offsetMax = Vector2.zero;

        titleText = MakeText(panel.transform, "Title", font, 34, TextAnchor.UpperCenter);
        titleText.color = new Color(0.5f, 0.8f, 1f);
        titleText.fontStyle = FontStyle.Bold;
        var tRT = titleText.rectTransform;
        tRT.anchorMin = new Vector2(0f, 1f); tRT.anchorMax = new Vector2(1f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0f, -24f);
        tRT.sizeDelta = new Vector2(-60f, 50f);

        bodyText = MakeText(panel.transform, "Body", font, 30, TextAnchor.UpperLeft);
        bodyText.color = Color.white;
        var bRT = bodyText.rectTransform;
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(40f, 70f);
        bRT.offsetMax = new Vector2(-40f, -90f);

        hintText = MakeText(panel.transform, "Hint", font, 24, TextAnchor.LowerCenter);
        hintText.color = new Color(1f, 0.9f, 0.5f);
        var hRT = hintText.rectTransform;
        hRT.anchorMin = new Vector2(0f, 0f); hRT.anchorMax = new Vector2(1f, 0f);
        hRT.pivot = new Vector2(0.5f, 0f);
        hRT.anchoredPosition = new Vector2(0f, 20f);
        hRT.sizeDelta = new Vector2(-60f, 40f);
    }

    private Text MakeText(Transform parent, string name, Font font, int size, TextAnchor anchor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = anchor;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
}
