using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sistema sequencial de objetivos: só um objetivo fica ativo por vez, e concluir
/// um ativa o próximo (o encadeamento é dado por cada objetivo). Concluir depende
/// de uma condição (falar com um NPC, entrar numa sala, chegar numa zona), então
/// ações fora de ordem simplesmente não avançam nada. Mostra o objetivo atual no
/// canto superior esquerdo e um aviso curto ("Objetivo concluído!") ao completar.
/// O objetivo atual sobrevive a trocas de cena via GameProgress.CurrentObjectiveId.
///
/// A lista de objetivos é fixa (conteúdo hardcoded, por decisão de escopo). Novos
/// dias/aulas entram estendendo esta lista nas próximas etapas.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    /// <summary>Mantido para o restante do código (caderneta/portas) checar; hoje sempre falso.</summary>
    public static bool IsGameOver => false;

    private enum Cond { Manual, TalkToNpc, EnterRoom, ReachZone }

    private class Objective
    {
        public string id;
        public string titulo;      // mostrado na HUD
        public Cond cond;          // como se conclui
        public string alvo;        // npcId (TalkToNpc) ou roomId (EnterRoom)
        public string proximo;     // id do próximo objetivo ("" = fim do dia por enquanto)
        public string roomId;      // se preenchido, vira a sala liberada ao ativar (ClassSchedule)
        public string roomLabel;   // rótulo amigável da sala liberada
        public bool dayEnd;        // ao concluir, roda a transição de dia antes de ir pro próximo
        public bool timeSkip;      // igual dayEnd, mas com o corte grande ("Algumas semanas depois")
        public int semesterDayAfterSkip; // só usado com timeSkip: dia absoluto do semestre (1–100) após o salto
        public string skipLine1, skipLine2; // só usado com timeSkip: mensagens custom (null = as genéricas de sempre)
        public System.Action onActivate; // disparado quando o objetivo vira o atual (ex.: iniciar o trote)
        public System.Action onComplete; // disparado ao concluir o objetivo (ex.: recompensa da side quest)
    }

    // Sequência do Dia 1. Os objetivos de "ir para a aula" liberam a sala certa
    // (roomId/roomLabel) ao serem ativados; os de "assistir" concluem ao falar com
    // o professor. Estende-se com novos dias nas próximas etapas.
    private readonly Objective[] objetivos =
    {
        new Objective
        {
            id = "ir_aula_ihc",
            titulo = "Ir para a aula de IHC (Bloco 1 — Sala 1)",
            cond = Cond.EnterRoom,
            alvo = ClassSchedule.RoomIHC,
            proximo = "assistir_ihc",
            roomId = ClassSchedule.RoomIHC,
            roomLabel = "IHC com a Rainara (Bloco 1, Sala 1)",
        },
        new Objective
        {
            id = "assistir_ihc",
            titulo = "Assistir a aula de IHC (fale com a Rainara)",
            cond = Cond.TalkToNpc,
            alvo = "rainara",
            proximo = "ir_aula_aragao",
        },
        new Objective
        {
            id = "ir_aula_aragao",
            titulo = "Ir para a aula do professor Aragão (Bloco 2 — Sala 1)",
            cond = Cond.EnterRoom,
            alvo = ClassSchedule.RoomAragao,
            proximo = "assistir_aragao",
            roomId = ClassSchedule.RoomAragao,
            roomLabel = "Matemática Básica com o Aragão (Bloco 2, Sala 1)",
        },
        new Objective
        {
            id = "assistir_aragao",
            titulo = "Assistir a aula do professor Aragão (fale com o Aragão)",
            cond = Cond.TalkToNpc,
            alvo = "aragao",
            proximo = "interacao_etica",
        },
        new Objective
        {
            id = "interacao_etica",
            titulo = "Converse com alguém no campus (procure a Emilly, perto da Convivência)",
            cond = Cond.TalkToNpc,
            alvo = "emilly",
            proximo = "jogar_pingpong",
        },
        new Objective
        {
            // Concluído pela partida em si (PingPongGameController → ForceComplete ao
            // voltar da cena do minigame). Dá Ética + alívio de estresse (ver Start).
            id = "jogar_pingpong",
            titulo = "Relaxe um pouco: jogue pingue-pongue com o Vitim (na Convivência)",
            cond = Cond.Manual,
            proximo = "ir_aula_fup",
            dayEnd = true, // encerra o Dia 1 → transição → Dia 2
        },

        // ---- Dia 2 ----
        new Objective
        {
            id = "ir_aula_fup",
            titulo = "Ir para a aula de FUP (Bloco 3 — Sala 1)",
            cond = Cond.EnterRoom,
            alvo = ClassSchedule.RoomFUP,
            proximo = "assistir_fup",
            roomId = ClassSchedule.RoomFUP,
            roomLabel = "FUP com a Paulyne (Bloco 3, Sala 1)",
        },
        new Objective
        {
            id = "assistir_fup",
            titulo = "Assistir a aula de FUP (fale com a Paulyne)",
            cond = Cond.TalkToNpc,
            alvo = "paulete",
            proximo = "interacao_etica_d2",
        },
        new Objective
        {
            id = "interacao_etica_d2",
            titulo = "Converse com um colega (procure a Yasmin, no corredor do Bloco 3)",
            cond = Cond.TalkToNpc,
            alvo = "yasmin",
            proximo = "socializar_enzo",
        },
        new Objective
        {
            id = "socializar_enzo",
            titulo = "Faça amizade: converse com o Enzo (corredor do Bloco 4)",
            cond = Cond.TalkToNpc,
            alvo = "enzo",
            proximo = "ajudar_matheus",
            dayEnd = true, // encerra o Dia 2 → transição → Dia 3
        },

        // ---- Dia 3 (véspera das provas) ----
        new Objective
        {
            id = "ajudar_matheus",
            titulo = "Ajude um colega: fale com o Matheus no campus",
            cond = Cond.TalkToNpc,
            alvo = "aluno_matheus",
            proximo = "estudar_natan",
        },
        new Objective
        {
            id = "estudar_natan",
            titulo = "Estude para as provas (vá ao RU e fale com o Natan)",
            cond = Cond.TalkToNpc,
            alvo = "natan",
            proximo = "trote_correr",
            dayEnd = true, // encerra o Dia 3 → transição → Dia 4 (trote)
        },

        // ---- Dia 4 (trote) ----
        new Objective
        {
            // Não tem "condição" no sentido normal: assim que este objetivo fica
            // ativo, TroteChase.BeginChase() põe Natan, Enzo, Matheus e Vitim pra
            // correr atrás do jogador (ver onActivate). O próprio TroteChase chama
            // ForceComplete quando a perseguição termina (pego ou escapou).
            id = "trote_correr",
            titulo = "Fuja dos veteranos! Corra ou entre em algum prédio.",
            cond = Cond.Manual,
            onActivate = () => TroteChase.Instance?.BeginChase(),
            proximo = "prova_ihc",
            timeSkip = true, // depois do trote vem o salto temporal pras provas
            semesterDayAfterSkip = 20, // calendário dos 100 dias: Prova R1 cai no Dia 20 (roadmap-v2.md, 3.1B)
        },

        // ---- Bloco de provas (pós time skip) ----
        // Todas concluem por Cond.Manual: a prova em si (fala do professor / quiz /
        // problema / labirinto) chama ForceComplete quando termina. O roomId de cada
        // uma libera a sala certa, guiando o jogador de prova em prova.
        new Objective
        {
            id = "prova_ihc",
            titulo = "Prova de IHC — fale com a Rainara (Bloco 1 — Sala 1)",
            cond = Cond.Manual,
            proximo = "prova_ies",
            roomId = ClassSchedule.RoomIHC,
            roomLabel = "Prova de IHC (Bloco 1, Sala 1)",
        },
        new Objective
        {
            id = "prova_ies",
            titulo = "Prova de IES — faça o quiz com o Jeferson (Bloco 4 — Sala 1)",
            cond = Cond.Manual,
            proximo = "prova_fup",
            roomId = ClassSchedule.RoomIES,
            roomLabel = "Prova de IES (Bloco 4, Sala 1)",
        },
        new Objective
        {
            id = "prova_fup",
            titulo = "Prova de FUP — monte a solução com a Paulyne (Bloco 3 — Sala 1)",
            cond = Cond.Manual,
            proximo = "prova_mat",
            roomId = ClassSchedule.RoomFUP,
            roomLabel = "Prova de FUP (Bloco 3, Sala 1)",
        },
        new Objective
        {
            id = "prova_mat",
            titulo = "Prova de Matemática — fale com o Aragão (Bloco 2 — Sala 1)",
            cond = Cond.Manual,
            proximo = "notebook_prof",
            roomId = ClassSchedule.RoomAragao,
            roomLabel = "Prova de Matemática (Bloco 2, Sala 1)",
            timeSkip = true, // Dia 20 (Prova R1) → Dia 28 (side quest do notebook)
            semesterDayAfterSkip = 28,
            skipLine1 = "Alguns dias depois...",
            skipLine2 = "Um comunicado circula pelo campus: o professor Aragão perdeu um caderno importante.",
        },

        // ---- Dia 28 (SQ1 — Notebook Desaparecido, roadmap 3.9) ----
        // As 4 etapas acontecem no mesmo dia (decisão de 03/07/2026): professor →
        // Gabi (atendente do RU) → laboratório do Bloco 2 → devolução.
        new Objective
        {
            // O Aragão não está na sala dele aqui: ele sai pra relaxar dentro da
            // Convivência assim que percebe o sumiço (decisão de 04/07/2026) — por
            // isso sem roomId/porta de sala de aula (a Convivência já tem porta
            // própria, sem gating); é só entrar lá e falar com ele.
            id = "notebook_prof",
            titulo = "Fale com o Aragão sobre o caderno sumido (ele está dentro da Convivência)",
            cond = Cond.TalkToNpc,
            alvo = "aragao",
            proximo = "notebook_ru",
            // Primeiro ponto estável depois da Prova R1 — autosave daqui (decisão de
            // 04/07/2026: "após a prova do Aragão"). Salvar aqui em vez de no exato
            // fim da prova evita gravar um estado transitório (objetivo ainda vazio,
            // dia do semestre ainda não saltado) enquanto o time skip está rodando.
            onActivate = () => { MoveAragaoToConvivencia(); SaveSystem.Save(); },
            // Ele volta pra sala assim que o papo acaba — é lá que a devolução
            // acontece (objetivo "notebook_devolucao").
            onComplete = () => MoveAragaoHome(),
        },
        new Objective
        {
            id = "notebook_ru",
            titulo = "Pergunte no RU se alguém viu o caderno (fale com a Gabi)",
            cond = Cond.TalkToNpc,
            alvo = "atendente_ru",
            proximo = "notebook_lab",
        },
        new Objective
        {
            id = "notebook_lab",
            titulo = "Procure o caderno no laboratório do Bloco 2",
            cond = Cond.TalkToNpc,
            alvo = "notebook_objeto",
            proximo = "notebook_devolucao",
            roomId = ClassSchedule.RoomBloco2Lab,
            roomLabel = "Laboratório (Bloco 2, Sala 2)",
        },
        new Objective
        {
            id = "notebook_devolucao",
            titulo = "Devolva o caderno ao Aragão (Bloco 2 — Sala 1)",
            cond = Cond.TalkToNpc,
            alvo = "aragao",
            proximo = "",
            roomId = ClassSchedule.RoomAragao,
            roomLabel = "Bloco 2, Sala 1",
            onComplete = () =>
            {
                GameProgress.SetFlag("notebook_devolvido");
                float granted = GameProgress.AddEthics(1.0f);
                QuestManager.Instance?.ShowMessage(granted > 0f
                    ? $"Caderno devolvido! Ética +{granted:0.0}"
                    : "Caderno devolvido!");
            },
        },
    };

    private string currentId = "";

    // Fim de dia pendente: guardado ao concluir o último objetivo do dia; a
    // transição só roda quando não há diálogo/cutscene aberta (ver Update).
    private bool pendingDayEnd;
    private bool pendingTimeSkip;
    private int pendingFinishedDay;
    private int pendingSemesterDayAfterSkip;
    private string pendingSkipLine1, pendingSkipLine2;
    private string pendingNextId = "";

    private Text objectiveText;
    private Text toastText;
    private Coroutine toastCo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Time.timeScale = 1f;
        BuildUI();

        // Restaura o objetivo atual (ex.: ao voltar de um minigame, a cena recarrega
        // mas GameProgress.CurrentObjectiveId persiste). SetCurrent reaplica também a
        // sala liberada (ClassSchedule) do objetivo restaurado.
        string restore = GameProgress.CurrentObjectiveId;

        // Rede de segurança: se a abertura já rodou nesta sessão mas nenhum objetivo
        // está ativo (ex.: replay no Editor sem domain reload, onde a abertura é
        // pulada), garante que a sequência comece mesmo assim.
        if (string.IsNullOrEmpty(restore) && GameProgress.CampusTourSeen)
            restore = FirstObjectiveId();

        SetCurrent(restore);
    }

    private void Start()
    {
        // Voltando da partida de pingue-pongue (cena separada): jogar com o Vitim é
        // uma interação social — concede Ética (com teto diário) e alivia o estresse.
        // Feito no Start (não no Awake) porque depende do AcademicHud já existir.
        if (PingPongSession.MatchPlayed)
        {
            PingPongSession.MatchPlayed = false;

            if (!GameProgress.HasFlag("pingpong_jogado"))
            {
                GameProgress.SetFlag("pingpong_jogado");
                float granted = GameProgress.AddEthics(1.0f);
                if (AcademicHud.Instance != null) AcademicHud.Instance.AddStress(-8f);
                ShowToast(granted > 0f
                    ? $"Boa partida! Ética +{granted:0.0} · estresse aliviado"
                    : "Boa partida! Estresse aliviado");
            }

            // Conclui o objetivo do ping-pong, se for o atual (encerra o Dia 1).
            ForceComplete("jogar_pingpong");
        }
    }

    private string FirstObjectiveId() => objetivos.Length > 0 ? objetivos[0].id : "";

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; Time.timeScale = 1f; }
    }

    private void Update()
    {
        // Dispara a transição de fim de dia quando o caminho estiver livre (sem
        // diálogo, cutscene ou outra transição em andamento).
        if (!pendingDayEnd) return;
        if (DialogueManager.IsActive || CampusTourCutscene.Active) return;
        if (DayTransition.Instance == null || DayTransition.Active) return;

        pendingDayEnd = false;
        int finished = pendingFinishedDay;
        string next = pendingNextId;
        bool timeSkip = pendingTimeSkip;
        int semesterDayAfterSkip = pendingSemesterDayAfterSkip;

        string line1 = timeSkip ? (pendingSkipLine1 ?? "Algumas semanas depois...") : $"Dia {finished} finalizado";
        string line2 = timeSkip ? (pendingSkipLine2 ?? "Período de primeiras provas!") : $"Boa sorte no Dia {finished + 1}!";

        DayTransition.Instance.Play(line1, line2, () =>
        {
            // Efeitos de duração "só hoje" (ex.: cheiro do trote) não sobrevivem a
            // uma virada de dia, seja ela normal ou um salto temporal.
            GameProgress.ClearFlag(TroteChase.SmellFlag);

            if (timeSkip)
            {
                // Salto de semanas: pula o calendário e começa um novo período (zera
                // o teto diário de Ética), mas não conta como "mais um dia".
                GameProgress.EthicsGainedToday = 0f;
                if (semesterDayAfterSkip > 0) GameProgress.JumpSemesterDayTo(semesterDayAfterSkip);
            }
            else
            {
                GameProgress.AdvanceDay();
            }
            SetCurrent(next);
        });
    }

    /// <summary>Ativa um objetivo por id (usado pela abertura pra definir o primeiro).</summary>
    public void ActivateObjective(string id) => SetCurrent(id);

    /// <summary>
    /// Inicia a sequência no primeiro objetivo (chamado pela abertura ao terminar).
    /// Não sobrescreve um objetivo já em andamento (ex.: sessão retomada).
    /// </summary>
    public void StartSequence()
    {
        if (!string.IsNullOrEmpty(currentId)) return;
        SetCurrent(FirstObjectiveId());
        var o = Find(currentId);
        if (o != null) ShowToast("Novo objetivo: " + o.titulo);
    }

    /// <summary>Título do objetivo atual (para a caderneta). "" se não houver.</summary>
    public string CurrentObjectiveTitle
    {
        get { var o = Find(currentId); return o != null ? o.titulo : ""; }
    }

    /// <summary>Conclui o objetivo atual se a condição/alvo baterem, e avança.</summary>
    public void OnTalked(string npcId) => TryComplete(Cond.TalkToNpc, npcId);
    public void OnEnteredRoom(string roomId) => TryComplete(Cond.EnterRoom, roomId);
    public void OnReachedGoal() => TryComplete(Cond.ReachZone, null);

    /// <summary>Verdadeiro se o objetivo atual é o de id informado (para bloqueios/orientação).</summary>
    public bool IsCurrent(string id) => currentId == id;

    private void TryComplete(Cond cond, string alvo)
    {
        var o = Find(currentId);
        if (o == null || o.cond != cond) return;
        if (cond != Cond.ReachZone && o.alvo != alvo) return;
        AdvanceFrom(o);
    }

    /// <summary>
    /// Conclui o objetivo atual identificado por id (usado por provas/minigames que
    /// concluem por conta própria — objetivos Cond.Manual).
    /// </summary>
    public void ForceComplete(string id)
    {
        var o = Find(currentId);
        if (o != null && o.id == id) AdvanceFrom(o);
    }

    private void AdvanceFrom(Objective o)
    {
        o.onComplete?.Invoke();

        if (o.dayEnd || o.timeSkip)
        {
            // Não avança agora: guarda o fim-de-dia/salto e deixa a transição rodar
            // quando o diálogo/escolha em andamento fechar (ex.: a escolha ética).
            pendingDayEnd = true;
            pendingTimeSkip = o.timeSkip;
            pendingFinishedDay = GameProgress.CurrentDay;
            pendingSemesterDayAfterSkip = o.semesterDayAfterSkip;
            pendingSkipLine1 = o.skipLine1;
            pendingSkipLine2 = o.skipLine2;
            pendingNextId = o.proximo;
            ShowToast("✓ Objetivo concluído!");
            SetCurrent(""); // limpa o objetivo; o próximo vem depois da transição
            return;
        }

        var next = Find(o.proximo);
        if (next != null)
        {
            // Sem "próximo objetivo" pra anunciar (fim de uma cadeia, ex.: provas ou
            // a devolução do notebook): deixa o toast a cargo do onComplete acima
            // (ou nenhum, se não houver) em vez de um texto genérico que o pisaria.
            ShowToast("✓ Concluído! Novo objetivo: " + next.titulo);
        }
        SetCurrent(o.proximo);
    }

    private void SetCurrent(string id)
    {
        currentId = id ?? "";
        GameProgress.CurrentObjectiveId = currentId;

        // Objetivo de "ir para a aula" libera a sala certa (as outras mostram
        // "sala errada"). Assim o gating de sala acompanha o avanço do dia.
        var o = Find(currentId);
        if (o != null && !string.IsNullOrEmpty(o.roomId))
        {
            ClassSchedule.CurrentRoomId = o.roomId;
            if (!string.IsNullOrEmpty(o.roomLabel)) ClassSchedule.CurrentRoomLabel = o.roomLabel;
        }
        o?.onActivate?.Invoke();

        RefreshObjectiveHud();
    }

    /// <summary>Mostra um aviso curto no topo (ex.: ganho de Ética). Reusa o toast.</summary>
    public void ShowMessage(string message) => ShowToast(message);

    // ------------------------------------------------------------ Aragão (SQ1)

    // O Aragão mora fixo na sala dele (Bloco 2 — Sala 1), mas durante o gatilho
    // do notebook (objetivo "notebook_prof") sai pra dentro da Convivência (o
    // salão da AC, não o deck externo) — reaproveita a mesma instância, salvando
    // de onde veio, no mesmo esquema já usado pelo TroteChase pros veteranos
    // (nunca em dois lugares do mapa ao mesmo tempo). Estáticos (não por serem
    // globais de propósito, mas porque os lambdas de onActivate/onComplete vivem
    // no inicializador do campo "objetivos" — nesse ponto a instância ainda não
    // terminou de construir, então só dá pra referenciar membros static; ver
    // CS0236). Como só existe um QuestManager por sessão mesmo, isso não muda
    // o comportamento.
    private static NpcInteractable aragaoNpc;
    private static Vector3 aragaoHomePos;
    private static Vector3 aragaoHomeScale;

    private static NpcInteractable FindNpcById(string npcId)
    {
        foreach (var n in Object.FindObjectsByType<NpcInteractable>(FindObjectsSortMode.None))
            if (n.npcId == npcId) return n;
        return null;
    }

    private static NpcInteractable FindAragao()
    {
        if (aragaoNpc == null) aragaoNpc = FindNpcById("aragao");
        return aragaoNpc;
    }

    private static void MoveAragaoToConvivencia()
    {
        var a = FindAragao();
        if (a == null) return;
        aragaoHomePos = a.transform.position;
        aragaoHomeScale = a.transform.localScale;

        // O salão da AC fica numa região à parte (interiorsRoot), sem coordenada
        // fixa conhecida de fora — o Vitim (sempre parado perto da mesa de
        // pingpong nessa hora do jogo) serve de âncora em tempo de execução.
        // Offset medido pro vão livre entre as 4 mesinhas do meio, longe de
        // móveis e do próprio Vitim (ver BuildConvivenciaInterior).
        var vitim = FindNpcById("vitim");
        Vector3 spot = vitim != null
            ? vitim.transform.position + new Vector3(7.6f, 2.4f, 0f)
            : aragaoHomePos; // rede de segurança: sem o Vitim (não deveria acontecer), não mexe

        a.transform.position = new Vector3(spot.x, spot.y, aragaoHomePos.z);
        a.transform.localScale = new Vector3(1.6f, 1.6f, 1f); // escala "de perto" do salão da AC, como o Vitim
        a.hasChoice = false; // a escolha "Matemática te dá um frio na barriga?" já foi resolvida no Dia 1
    }

    private static void MoveAragaoHome()
    {
        var a = FindAragao();
        if (a == null) return;
        a.transform.position = aragaoHomePos;
        a.transform.localScale = aragaoHomeScale;
    }

    private Objective Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var o in objetivos) if (o.id == id) return o;
        return null;
    }

    private void RefreshObjectiveHud()
    {
        var o = Find(currentId);
        if (objectiveText != null)
            objectiveText.text = o != null ? "Objetivo: " + o.titulo : "";
    }

    private void ShowToast(string message)
    {
        if (toastText == null) return;
        toastText.text = message;
        if (toastCo != null) StopCoroutine(toastCo);
        toastCo = StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        var c = toastText.color; c.a = 1f; toastText.color = c;
        yield return new WaitForSecondsRealtime(2.2f);
        // Fade out.
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / 0.6f);
            toastText.color = c;
            yield return null;
        }
        toastText.text = "";
        toastCo = null;
    }

    // ------------------------------------------------------------------ UI

    private void BuildUI()
    {
        var canvasGO = new GameObject("QuestCanvas");
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        objectiveText = CreateText(canvasGO.transform, "Objective", font, 30, TextAnchor.UpperLeft);
        objectiveText.color = new Color(1f, 1f, 0.85f);
        objectiveText.fontStyle = FontStyle.Bold;
        var oRT = objectiveText.rectTransform;
        oRT.anchorMin = new Vector2(0f, 1f);
        oRT.anchorMax = new Vector2(0f, 1f);
        oRT.pivot = new Vector2(0f, 1f);
        oRT.anchoredPosition = new Vector2(28f, -22f);
        oRT.sizeDelta = new Vector2(900f, 60f);

        // Aviso de objetivo concluído (topo-centro).
        toastText = CreateText(canvasGO.transform, "Toast", font, 32, TextAnchor.MiddleCenter);
        toastText.color = new Color(0.6f, 1f, 0.6f);
        toastText.fontStyle = FontStyle.Bold;
        var tRT = toastText.rectTransform;
        tRT.anchorMin = new Vector2(0.5f, 1f);
        tRT.anchorMax = new Vector2(0.5f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0f, -100f);
        tRT.sizeDelta = new Vector2(1200f, 50f);
        toastText.text = "";
    }

    private Text CreateText(Transform parent, string name, Font font, int size, TextAnchor anchor)
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
