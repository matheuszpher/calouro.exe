using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta a cena separada do minigame de pingue-pongue com o Vitim. A arena, as
/// barras, a bola e a UI são construídas em tempo de execução pelo
/// PingPongGameController — aqui só criamos a cena com esse componente e
/// registramos nos Build Settings, senão SceneManager.LoadScene não acha a cena
/// pelo nome. Rodar de novo só é necessário se a cena for apagada/corrompida.
/// </summary>
public static class PingPongSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/PingPongMinigame.unity";

    [MenuItem("Tools/Calouro/Montar Cena do Pingue-Pongue")]
    public static void Build()
    {
        string previousScenePath = SceneManager.GetActiveScene().path;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var go = new GameObject("PingPongMinigame");
        go.AddComponent<PingPongGameController>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        RegisterInBuildSettings();

        if (!string.IsNullOrEmpty(previousScenePath))
            EditorSceneManager.OpenScene(previousScenePath);

        Debug.Log("[Calouro] Cena do pingue-pongue montada em " + ScenePath + " e registrada nos Build Settings.");
    }

    private static void RegisterInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == ScenePath)
            {
                scenes[i] = new EditorBuildSettingsScene(ScenePath, true);
                found = true;
                break;
            }
        }
        if (!found) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
