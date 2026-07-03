using UnityEngine;

/// <summary>
/// NPC cujo nome, sprite e fala espelham o gênero OPOSTO ao escolhido pelo
/// jogador na tela de título (GameProgress.PlayerCharacter) — usado pelo
/// Gabriel/Gabriela (SQ2, Dia 32, roadmap 3.10). Decisão de 04/07/2026: caloura
/// (jogadora) → aparece o Gabriel, com o sprite de calouro; calouro (jogador)
/// → aparece a Gabriela, com o sprite de caloura. Usa as MESMAS folhas do
/// personagem principal (calouro.png/caloura.png, 6x4), não uma arte de NPC —
/// por isso a troca só pode acontecer em tempo de execução: a escolha de
/// personagem só existe depois da tela de título, bem depois da cena montada.
/// </summary>
[RequireComponent(typeof(NpcInteractable))]
[RequireComponent(typeof(SpriteWalkAnimator))]
public class GenderMirrorNpc : MonoBehaviour
{
    [Tooltip("Poses fatiadas de calouro.png (6x4) — usadas quando a jogadora escolheu 'caloura' (vira o Gabriel).")]
    public Sprite[] maleFrames;
    [Tooltip("Poses fatiadas de caloura.png (6x4) — usadas quando o jogador escolheu 'calouro' (vira a Gabriela).")]
    public Sprite[] femaleFrames;

    public string maleName = "Gabriel";
    public string femaleName = "Gabriela";

    [TextArea(2, 5)] public string[] maleLines;
    [TextArea(2, 5)] public string[] femaleLines;

    private void Start() => Apply();

    /// <summary>Aplica o espelhamento — chamado no Start (a escolha do jogador já existe nessa hora).</summary>
    public void Apply()
    {
        bool playerIsFemale = GameProgress.PlayerCharacter == "caloura";
        // Espelho INVERTIDO: jogadora (caloura) -> Gabriel (macho); jogador (calouro) -> Gabriela (fêmea).
        bool showMale = playerIsFemale;

        var frames = showMale ? maleFrames : femaleFrames;
        var lines = showMale ? maleLines : femaleLines;

        var npc = GetComponent<NpcInteractable>();
        npc.npcName = showMale ? maleName : femaleName;
        if (lines != null && lines.Length > 0) npc.lines = lines;

        if (frames == null || frames.Length == 0) return;

        // Layout do personagem principal (calouro.png/caloura.png): 6x4, coluna
        // 0 = parado, colunas 3-5 = passos (mesma convenção de PlayerAppearance.Apply()).
        var anim = GetComponent<SpriteWalkAnimator>();
        anim.frames = frames;
        anim.downFrames = new[] { 3, 4, 5 };
        anim.sideFrames = new[] { 9, 10, 11 };
        anim.upFrames = new[] { 15, 16, 17 };
        anim.downIdle = 0;
        anim.sideIdle = 6;
        anim.upIdle = 12;
        anim.invertSide = true;
        anim.swayWhenUp = false;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null && frames[anim.downIdle] != null) sr.sprite = frames[anim.downIdle];
    }
}
