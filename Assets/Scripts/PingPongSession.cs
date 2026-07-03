using UnityEngine;

/// <summary>
/// Guarda o estado da Convivência (spawn, retorno, limites da câmera, escala do
/// jogador) durante a troca de cena para o minigame de pingue-pongue com o
/// Vitim, para restaurar tudo exatamente igual ao voltar para a SampleScene.
/// É só um handoff entre cenas — não é progresso do jogo, por isso não mora em
/// GameProgress (ver VitimPingPongTrigger / InteriorController).
/// </summary>
public static class PingPongSession
{
    public static bool Active;

    /// <summary>Marcado ao terminar a partida; o QuestManager lê ao recarregar a
    /// SampleScene pra dar o prêmio (Ética) e concluir a missão.</summary>
    public static bool MatchPlayed;

    /// <summary>Se o jogador venceu a última partida (flavor da mensagem de retorno).</summary>
    public static bool PlayerWon;

    public static Vector3 ReturnSpawn;
    public static Vector3 ReturnFront;
    public static Vector2 RoomBoundsMin;
    public static Vector2 RoomBoundsMax;
    public static float PlayerScale = 1f;
}
