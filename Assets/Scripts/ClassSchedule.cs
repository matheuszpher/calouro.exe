/// <summary>
/// Guarda qual sala de aula é a "correta" no momento. O sistema de objetivos
/// (QuestManager) atualiza isto conforme o dia avança: cada objetivo "ir para a
/// aula X" aponta a sala liberada, e as portas das demais salas mostram um
/// "pensamento" de sala errada (ver BuildingDoor).
///
/// Os ids das salas são os MESMOS que o montador dá às portas (classroomId =
/// "BLOCO N (00N) — Sala K"). Por isso ficam como constantes aqui, reaproveitadas
/// tanto pelas portas quanto pelos objetivos, pra não haver divergência de texto.
/// </summary>
public static class ClassSchedule
{
    public const string RoomIHC = "BLOCO 1 (001) — Sala 1";
    public const string RoomAragao = "BLOCO 2 (002) — Sala 1";
    public const string RoomFUP = "BLOCO 3 (003) — Sala 1";
    public const string RoomIES = "BLOCO 4 (004) — Sala 1";
    public const string RoomBloco2Lab = "BLOCO 2 (002) — Sala 2"; // laboratório — side quest do notebook (3.9)

    public static string CurrentRoomId = RoomIHC;
    public static string CurrentRoomLabel = "IHC com a Rainara (Bloco 1, Sala 1)";
}
