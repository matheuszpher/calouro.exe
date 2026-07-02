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
    public static Vector3 ReturnSpawn;
    public static Vector3 ReturnFront;
    public static Vector2 RoomBoundsMin;
    public static Vector2 RoomBoundsMax;
    public static float PlayerScale = 1f;
}
