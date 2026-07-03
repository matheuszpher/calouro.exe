using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Save em disco, 1 slot único, JSON via JsonUtility em
/// Application.persistentDataPath. Guarda só o que precisa pra retomar de onde
/// parou (notas, dia do semestre, objetivo atual, flags) — não a posição no
/// mapa: quem chama Load() decide pra onde teleportar o jogador (ver
/// TitleScreen, que manda pro campus e reativa o objetivo restaurado).
/// </summary>
public static class SaveSystem
{
    [System.Serializable]
    private class SaveData
    {
        public string playerName;
        public string playerCharacter;
        public bool campusTourSeen;
        public string currentObjectiveId;

        public float mathGrade, fupGrade, ihcGrade, iesGrade, ethicsGrade;
        public float ethicsGainedToday;

        public int currentDay;
        public int semesterDay;

        public List<string> flags;
    }

    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static bool HasSave() => File.Exists(SavePath);

    public static void Save()
    {
        var data = new SaveData
        {
            playerName = GameProgress.PlayerName,
            playerCharacter = GameProgress.PlayerCharacter,
            campusTourSeen = GameProgress.CampusTourSeen,
            currentObjectiveId = GameProgress.CurrentObjectiveId,
            mathGrade = GameProgress.MathGrade,
            fupGrade = GameProgress.FupGrade,
            ihcGrade = GameProgress.IhcGrade,
            iesGrade = GameProgress.IesGrade,
            ethicsGrade = GameProgress.EthicsGrade,
            ethicsGainedToday = GameProgress.EthicsGainedToday,
            currentDay = GameProgress.CurrentDay,
            semesterDay = GameProgress.SemesterDay,
            flags = new List<string>(GameProgress.Flags),
        };

        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            Debug.Log("[Calouro] Jogo salvo.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Calouro] Falha ao salvar: {e.Message}");
        }
    }

    /// <summary>Carrega o save pro GameProgress/AcademicHud. Retorna false se não há save ou está corrompido.</summary>
    public static bool Load()
    {
        if (!HasSave()) return false;
        try
        {
            SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            if (data == null) return false;

            GameProgress.PlayerName = data.playerName;
            GameProgress.PlayerCharacter = data.playerCharacter;
            GameProgress.CampusTourSeen = data.campusTourSeen;
            GameProgress.CurrentObjectiveId = data.currentObjectiveId;
            GameProgress.MathGrade = data.mathGrade;
            GameProgress.FupGrade = data.fupGrade;
            GameProgress.IhcGrade = data.ihcGrade;
            GameProgress.IesGrade = data.iesGrade;
            GameProgress.EthicsGrade = data.ethicsGrade;
            GameProgress.EthicsGainedToday = data.ethicsGainedToday;
            GameProgress.CurrentDay = data.currentDay;
            GameProgress.SemesterDay = data.semesterDay;

            GameProgress.Flags.Clear();
            if (data.flags != null) foreach (var f in data.flags) GameProgress.Flags.Add(f);

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Calouro] Falha ao carregar save: {e.Message}");
            return false;
        }
    }

    public static void Delete()
    {
        if (HasSave()) File.Delete(SavePath);
    }
}
