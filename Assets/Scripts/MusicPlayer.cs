using UnityEngine;

/// <summary>
/// Música tema do jogo: um AudioSource em loop, tocando desde a tela de
/// título e durante toda a exploração do campus (roadmap 3.18 — 1ª trilha).
/// Sem controle de volume ainda (o slider fica no menu de pausa, 3.15, que
/// ainda não existe) — volume fixo no Inspector/montador por enquanto.
/// </summary>
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance { get; private set; }

    public AudioClip theme;
    [Range(0f, 1f)] public float volume = 0.5f;

    private AudioSource source;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        source = gameObject.AddComponent<AudioSource>();
        source.clip = theme;
        source.loop = true;
        source.volume = volume;
        source.playOnAwake = false;
        if (theme != null) source.Play();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Para a música tema — usado ao trocar pra outra trilha (ex.: créditos finais).</summary>
    public void Stop()
    {
        if (source != null) source.Stop();
    }
}
