using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gerencia a troca de tela para os interiores (corredores dos blocos e salas).
/// Os interiores ficam numa região afastada da cena (como o labirinto). Suporta
/// aninhamento: campus → corredor do bloco → sala. Cada EnterRoom empilha o estado
/// atual (limites da câmera + posição de retorno); ExitRoom desempilha e volta um
/// nível. Assim sair da sala volta ao corredor, e sair do corredor volta ao campus.
/// </summary>
public class InteriorController : MonoBehaviour
{
    public static InteriorController Instance { get; private set; }
    public static bool InRoom => Instance != null && Instance.stack.Count > 0;

    /// <summary>
    /// Zoom da câmera dentro de qualquer interior (corredor ou sala) — mais
    /// próximo que o do campus (TopDownSceneBuilder.CampusOrthoSize), calibrado
    /// junto com a escala 1.6x "de perto" dos NPCs/jogador nesses ambientes.
    /// </summary>
    public const float InteriorOrthoSize = 8f;

    private GameObject player;
    private CameraFollow2D cam;
    private Camera mainCamera;

    private struct LocState
    {
        public bool useBounds;
        public Vector2 boundsMin, boundsMax;
        public float orthoSize;
        public Vector3 returnPos;
        public Vector3 playerScale;
    }
    private readonly Stack<LocState> stack = new Stack<LocState>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        stack.Clear();

        // Voltando do minigame de pingue-pongue (cena separada): reabre a
        // Convivência exatamente de onde o jogador saiu (ver PingPongSession).
        // Não zera o flag aqui — o TitleScreen também precisa lê-lo no Start()
        // (que roda depois de todos os Awake) pra saber que não deve se mostrar.
        if (PingPongSession.Active)
        {
            EnterRoom(PingPongSession.ReturnSpawn, PingPongSession.ReturnFront,
                PingPongSession.RoomBoundsMin, PingPongSession.RoomBoundsMax, PingPongSession.PlayerScale);
        }
    }

    /// <summary>Espia (sem tirar da pilha) o ponto de retorno do nível atual — usado
    /// pelo handoff do minigame de pingue-pongue pra saber pra onde voltar.</summary>
    public Vector3? PeekReturnPos() => stack.Count > 0 ? (Vector3?)stack.Peek().returnPos : null;

    private void OnDestroy()
    {
        if (Instance == this) { Instance = null; stack.Clear(); }
    }

    public void EnterRoom(Vector3 spawn, Vector3 returnTo, Vector2 boundsMin, Vector2 boundsMax, float playerScale = 1f, float orthoSize = InteriorOrthoSize)
    {
        if (MazeController.InMaze) return;
        EnsureRefs();

        // Guarda de onde viemos (câmera + zoom + retorno + escala do jogador) para
        // restaurar ao sair.
        var prev = new LocState { returnPos = returnTo };
        if (cam != null)
        {
            prev.useBounds = cam.useBounds;
            prev.boundsMin = cam.boundsMin;
            prev.boundsMax = cam.boundsMax;
        }
        if (mainCamera != null) prev.orthoSize = mainCamera.orthographicSize;
        if (player != null) prev.playerScale = player.transform.localScale;
        stack.Push(prev);

        if (cam != null)
        {
            cam.boundsMin = boundsMin;
            cam.boundsMax = boundsMax;
            cam.useBounds = true;
        }
        if (mainCamera != null) mainCamera.orthographicSize = orthoSize;
        if (player != null) player.transform.localScale = new Vector3(playerScale, playerScale, 1f);
        Teleport(spawn);
    }

    public void ExitRoom()
    {
        if (stack.Count == 0) return;
        ExitRoomTo(stack.Peek().returnPos);
    }

    /// <summary>
    /// Como ExitRoom(), mas teleporta para uma posição explícita em vez da posição
    /// de retorno empilhada. Usado pelos blocos-túnel: o tapete de saída norte/sul
    /// sempre leva para o lado norte/sul do prédio, não importa por onde se entrou.
    /// </summary>
    public void ExitRoomTo(Vector3 pos)
    {
        if (stack.Count == 0) return;
        var s = stack.Pop();
        if (cam != null)
        {
            cam.boundsMin = s.boundsMin;
            cam.boundsMax = s.boundsMax;
            cam.useBounds = s.useBounds;
        }
        if (mainCamera != null) mainCamera.orthographicSize = s.orthoSize;
        if (player != null) player.transform.localScale = s.playerScale;
        Teleport(pos);
    }

    /// <summary>
    /// Sai de TODOS os interiores de uma vez e teleporta o jogador para o campus,
    /// restaurando os limites de câmera do campus (o estado guardado no fundo da
    /// pilha). Usado pela transição de dia / time skip, que teleporta direto sem
    /// passar pelos tapetes de saída. Funciona mesmo já estando no campus.
    /// </summary>
    public void ForceCampus(Vector3 pos)
    {
        EnsureRefs();
        LocState baseState = default;
        bool had = false;
        while (stack.Count > 0) { baseState = stack.Pop(); had = true; }
        if (had && cam != null)
        {
            cam.boundsMin = baseState.boundsMin;
            cam.boundsMax = baseState.boundsMax;
            cam.useBounds = baseState.useBounds;
        }
        if (had && mainCamera != null) mainCamera.orthographicSize = baseState.orthoSize;
        if (player != null) player.transform.localScale = Vector3.one;
        Teleport(pos);
    }

    private void Teleport(Vector3 pos)
    {
        if (player == null) EnsureRefs();
        if (player == null) return;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        player.transform.position = pos;
        if (rb != null) rb.position = pos;
    }

    private void EnsureRefs()
    {
        if (player == null) player = GameObject.FindWithTag("Player");
        if (mainCamera == null) mainCamera = Camera.main;
        if (cam == null && mainCamera != null) cam = mainCamera.GetComponent<CameraFollow2D>();
    }
}
