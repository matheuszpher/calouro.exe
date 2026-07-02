using UnityEngine;

/// <summary>
/// Aparência do jogador: troca a folha de sprites do Player conforme o
/// personagem escolhido na tela de título (calouro/homem ou caloura/mulher).
/// As duas folhas (6x4 = 24 poses, mesmo layout) são preenchidas em edição
/// pelo montador da cena; a escolha em si mora em GameProgress.PlayerCharacter.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(SpriteWalkAnimator))]
public class PlayerAppearance : MonoBehaviour
{
    [Tooltip("Poses fatiadas de calouro.png (homem), preenchidas pelo montador.")]
    public Sprite[] calouroFrames;

    [Tooltip("Poses fatiadas de caloura.png (mulher), preenchidas pelo montador.")]
    public Sprite[] calouraFrames;

    private void Start()
    {
        // Aplica a escolha atual. Cobre dois casos sem UI: o padrão antes de
        // qualquer seleção e a volta de um minigame (a cena recarrega inteira,
        // mas GameProgress é estático e guarda a escolha da partida).
        Apply();
    }

    /// <summary>
    /// Troca a folha do Player para o personagem em GameProgress.PlayerCharacter.
    /// Chamado no Start() e de novo pela tela de título quando o jogador confirma.
    /// </summary>
    public void Apply()
    {
        Sprite[] frames = GameProgress.PlayerCharacter == "caloura" ? calouraFrames : calouroFrames;
        if (frames == null || frames.Length == 0) return;

        var anim = GetComponent<SpriteWalkAnimator>();
        anim.frames = frames;
        // Layout 6x4: linha 0 = frente, 1 = lado (encara a direita), 2 = costas,
        // 3 = lado esquerdo (não usado; o animador espelha a direita). Em cada
        // linha, coluna 0 = parado e colunas 3-5 = passos.
        anim.downFrames = new[] { 3, 4, 5 };
        anim.sideFrames = new[] { 9, 10, 11 };
        anim.upFrames = new[] { 15, 16, 17 };
        anim.downIdle = 0;
        anim.sideIdle = 6;
        anim.upIdle = 12;
        // As poses de lado encaram a direita; o animador assume que encaram a
        // esquerda, então invertemos para o espelhamento sair certo.
        anim.invertSide = true;
        // Há 3 quadros reais de costas — não precisa balançar para fingir passos.
        anim.swayWhenUp = false;

        var sr = GetComponent<SpriteRenderer>();
        int idle = Mathf.Clamp(anim.downIdle, 0, frames.Length - 1);
        if (frames[idle] != null) sr.sprite = frames[idle];

        // Reajusta o colisor aos "pés" da pose atual (as duas artes têm tamanhos
        // levemente diferentes).
        var col = GetComponent<BoxCollider2D>();
        if (col != null && sr.sprite != null)
        {
            Vector2 b = sr.sprite.bounds.size;
            col.size = new Vector2(b.x * 0.6f, b.y * 0.5f);
            col.offset = new Vector2(0f, -b.y * 0.2f);
        }
    }
}
