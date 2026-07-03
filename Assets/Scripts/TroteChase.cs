using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minigame do Dia 4 (Trote): Natan, Enzo, Matheus e Vitim — os mesmos NPCs que já
/// apareceram nos Dias 1–3 — saem de onde estavam (RU, Convivência, corredor do
/// Bloco 4, campus) e passam a correr atrás do calouro assim que o dia começa.
/// Ser pego gera uma cena de "sujaram você de ovo" (flag de cheiro pro resto do
/// dia); sobreviver ao tempo do trote ou entrar em qualquer prédio
/// conta como escapar. Sem cena separada — a perseguição acontece no próprio
/// campus (ver roadmap-v2.md, 3.1B/3.6).
/// </summary>
public class TroteChase : MonoBehaviour
{
    public static TroteChase Instance { get; private set; }
    public static bool Active { get; private set; }

    /// <summary>Ligada quando o jogador é pego e sujado — o DialogueManager usa pra comentar o cheiro.</summary>
    public const string SmellFlag = "trote_fedendo";

    private static readonly string[] ChaserIds = { "natan", "enzo", "aluno_matheus", "vitim" };

    private static readonly string[] CaptureLines =
    {
        "Os veteranos te cercaram!",
        "\"Bem-vindo à faculdade, calouro!\" — e antes que desse tempo de reagir, quebraram uma dúzia de ovos em você.",
        "Melado da cabeça aos pés e fedendo, só resta aturar as risadas ao redor pelo resto do dia.",
    };

    private static readonly string[] EscapeLines =
    {
        "Você corre o mais rápido que consegue e despista os veteranos.",
        "Ofegante, mas seco — hoje o trote não te pegou.",
    };

    private static readonly string[] SmellComments =
    {
        "Eca, que cheiro é esse? Você tá fedendo a ovo podre!",
        "Nossa, você tomou banho hoje? Tá com um cheiro estranho...",
        "Ihh, alguém aí passou pelo trote, né? Dá pra sentir o cheiro daqui.",
    };

    // Falas dos próprios veteranos ao serem procurados de novo depois do trote.
    private static readonly string[] CaughtBanterLines =
    {
        "Relaxa, é tradição! Ano que vem é você quem faz isso com os calouros novos.",
        "Até que você não ficou tão sujo assim, hein?",
        "Nem parece que você se sujou de ovo hoje... tá bem demais!",
    };
    private static readonly string[] EscapedBanterLines =
    {
        "Rapaz, você é rápido! Quase te pegamos hoje de manhã.",
        "Já vi muito calouro tentando fugir, mas poucos escapam que nem você.",
        "Não pensa que acabou não... ano que vem tem mais trote!",
    };

    private class ChaserState
    {
        public Transform t;
        public NpcInteractable npc;
        public Collider2D col;
        public NpcPatrol patrol;
        public Vector3 originalPos;   // de onde ele saiu, pra Vitim/Enzo voltarem exatamente pro lugar
        public Vector3 originalScale; // idem — Vitim/Enzo usam 1.6x nos interiores deles
    }

    // Pra onde cada um vai depois do trote: Vitim volta pra mesa de pingue-pongue
    // e Enzo pro corredor do Bloco 4 (ambos no lugar de origem, salvo antes da
    // corrida); Natan e Matheus passam a ficar juntos no deck da Convivência —
    // espaçados da Emilly (-3,-1) e um do outro, pra não sobrepor ninguém.
    private static readonly Vector2 ConvivenciaSpotNatan = new Vector2(-5f, -1f);
    private static readonly Vector2 ConvivenciaSpotMatheus = new Vector2(-1f, -1f);

    private readonly List<ChaserState> chasers = new List<ChaserState>();
    private Transform player;
    private readonly float speed = 3.6f;         // um pouco mais lento que o jogador (5) — dá pra fugir
    private readonly float captureRadius = 1.3f;
    private readonly float chaseDuration = 20f;
    private float timeLeft;
    private bool resolved;

    // Os veteranos ficam parados por um instante depois que o jogador recupera o
    // controle (a corrida já começa a valer durante o time skip anterior, com a
    // tela preta e o input travado — sem isso, quando a tela clareia o jogador já
    // aparece cercado, sem chance de reagir).
    private float startDelay;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; Active = false; }
    }

    /// <summary>Chamado quando o objetivo "trote_correr" fica ativo (ver QuestManager).</summary>
    public void BeginChase()
    {
        if (Active) return;
        player = GameObject.FindWithTag("Player")?.transform;
        if (player == null) return;

        chasers.Clear();
        // Bem mais longe que o raio de captura, um em cada direção — cerca o
        // jogador sem já começar a corrida "dentro do alcance" dele.
        Vector2[] offsets =
        {
            new Vector2(0f, 10f), new Vector2(0f, -10f), new Vector2(10f, 0f), new Vector2(-10f, 0f),
        };
        var all = Object.FindObjectsByType<NpcInteractable>(FindObjectsSortMode.None);

        int i = 0;
        foreach (var id in ChaserIds)
        {
            NpcInteractable npc = null;
            foreach (var n in all) if (n.npcId == id) { npc = n; break; }
            if (npc == null) { i++; continue; }

            var state = new ChaserState
            {
                t = npc.transform,
                npc = npc,
                col = npc.GetComponent<Collider2D>(),
                patrol = npc.GetComponent<NpcPatrol>(),
                originalPos = npc.transform.position,
                originalScale = npc.transform.localScale,
            };
            // Tira o NPC de onde estava (RU/Convivência/corredor/campus): sem
            // interação, sem patrulha própria, e teleportado pra perto do jogador —
            // nunca fica em dois lugares do mapa ao mesmo tempo. Escala volta a 1x:
            // Enzo/Vitim usam 1.6x nos interiores "de perto" deles, mas isso fica
            // enorme no campus aberto (ver PlayerAppearance/convenção do CLAUDE.md).
            // Restaurada de novo em Resolve() se o NPC voltar pro lugar de origem.
            if (state.patrol != null) state.patrol.enabled = false;
            if (state.col != null) state.col.enabled = false;
            npc.enabled = false;
            state.t.localScale = Vector3.one;

            state.t.position = (Vector2)player.position + offsets[i % offsets.Length];
            chasers.Add(state);
            i++;
        }

        if (chasers.Count == 0)
        {
            // Rede de segurança: se os NPCs não existirem na cena, não trava o dia.
            GameProgress.SetFlag("trote_escapou");
            QuestManager.Instance?.ForceComplete("trote_correr");
            return;
        }

        timeLeft = chaseDuration;
        startDelay = 1.5f;
        resolved = false;
        Active = true;
    }

    private void Update()
    {
        if (!Active || resolved || player == null) return;

        // Enquanto a tela ainda está preta/clareando (DayTransition) o jogador não
        // tem controle nenhum — não deixa os veteranos "roubarem" esse tempo.
        if (DayTransition.Active) return;

        if (startDelay > 0f)
        {
            startDelay -= Time.deltaTime;
            if (startDelay <= 0f)
                QuestManager.Instance?.ShowMessage("Os veteranos te viram! Corre ou entra em algum prédio!");
            return; // parados até o jogador ter controle e um instante pra reagir
        }

        foreach (var c in chasers)
        {
            if (c.t == null) continue;
            c.t.position = Vector3.MoveTowards(c.t.position, player.position, speed * Time.deltaTime);
            if (Vector2.Distance(c.t.position, player.position) <= captureRadius)
            {
                Resolve(caught: true);
                return;
            }
        }

        if (InteriorController.InRoom)
        {
            Resolve(caught: false);
            return;
        }

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f) Resolve(caught: false);
    }

    private void Resolve(bool caught)
    {
        resolved = true;
        Active = false;

        // Devolve os 4 NPCs à vida normal (interativos, com patrulha própria de
        // novo) — mas agora com falas de zoação sobre o trote, em vez das falas
        // originais (já usadas nos Dias 1–3) e sem reabrir a escolha ética/
        // pingue-pongue de novo. Reposiciona cada um em vez de deixar onde a
        // corrida terminou (senão os 4 ficam empilhados no mesmo ponto):
        // Vitim e Enzo voltam pro lugar de onde saíram; Natan e Matheus passam
        // a ficar na Convivência.
        // Vira "repeatLines" (não "lines"): os 4 já foram conhecidos nos Dias
        // 1–3, então CurrentLines() já pula "lines" e usa isso direto — ver
        // NpcInteractable.CurrentLines().
        var banter = new[] { new NpcInteractable.LineSet { lines = caught ? CaughtBanterLines : EscapedBanterLines } };
        foreach (var c in chasers)
        {
            if (c.npc != null)
            {
                c.npc.enabled = true;
                c.npc.repeatLines = banter;
                c.npc.hasChoice = false;

                bool backToOrigin = c.npc.npcId != "natan" && c.npc.npcId != "aluno_matheus";
                Vector3 finalPos = c.npc.npcId switch
                {
                    "natan" => new Vector3(ConvivenciaSpotNatan.x, ConvivenciaSpotNatan.y, c.originalPos.z),
                    "aluno_matheus" => new Vector3(ConvivenciaSpotMatheus.x, ConvivenciaSpotMatheus.y, c.originalPos.z),
                    _ => c.originalPos, // Vitim (mesa de pingue-pongue) e Enzo (corredor do Bloco 4)
                };
                c.t.position = finalPos;
                if (backToOrigin) c.t.localScale = c.originalScale; // Vitim/Enzo voltam à escala 1.6x de origem
            }
            if (c.col != null) c.col.enabled = true;
            if (c.patrol != null) c.patrol.enabled = true;
        }
        chasers.Clear();

        if (caught)
        {
            GameProgress.SetFlag("trote_pego");
            GameProgress.SetFlag(SmellFlag);
            DialogueManager.Instance?.StartDialogue("Trote", CaptureLines);
        }
        else
        {
            GameProgress.SetFlag("trote_escapou");
            DialogueManager.Instance?.StartDialogue("Trote", EscapeLines);
        }

        StartCoroutine(WaitDialogueThenComplete());
    }

    private IEnumerator WaitDialogueThenComplete()
    {
        yield return null; // deixa o StartDialogue abrir antes de checar IsActive
        while (DialogueManager.IsActive) yield return null;
        QuestManager.Instance?.ForceComplete("trote_correr");
    }

    /// <summary>Comentário aleatório sobre o cheiro (usado pelo DialogueManager enquanto SmellFlag estiver ativa).</summary>
    public static string RandomSmellComment() => SmellComments[Random.Range(0, SmellComments.Length)];
}
