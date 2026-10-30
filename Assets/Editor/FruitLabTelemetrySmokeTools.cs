using System;
using System.IO;
using System.Linq;
using Scripts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class FruitLabTelemetrySmokeTools
{
    private const string RestoreKey = "FruitLab.TelemetrySceneRestore";
    [Serializable] private class Scenes { public Entry[] entries; }
    [Serializable] private class Entry { public string path; public bool loaded, active; }
    static FruitLabTelemetrySmokeTools() => EditorApplication.playModeStateChanged += Restore;
    public static string Start()
    {
        var scenes = EditorSceneManager.GetSceneManagerSetup();
        if (Enumerable.Range(0, UnityEngine.SceneManagement.SceneManager.sceneCount)
            .Any(i => UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty))
            throw new Exception("Unsaved scene edits exist; SDK smoke test did not change the open scenes.");
        SessionState.SetString(RestoreKey, JsonUtility.ToJson(new Scenes { entries = scenes.Select(s => new Entry { path = s.path, loaded = s.isLoaded, active = s.isActive }).ToArray() }));
        File.WriteAllText(TelemetrySmokeProbe.RequestPath, "sdk-upload");
        EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
        EditorApplication.isPlaying = true;
        return "One explicitly tagged SDK test event will be uploaded; previous scene setup is restored on exit";
    }
    private static void Restore(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        string saved = SessionState.GetString(RestoreKey, "");
        if (string.IsNullOrEmpty(saved)) return;
        SessionState.EraseString(RestoreKey);
        File.Delete(TelemetrySmokeProbe.RequestPath);
        var entries = JsonUtility.FromJson<Scenes>(saved).entries;
        if (entries.All(e => !string.IsNullOrEmpty(e.path)))
            EditorSceneManager.RestoreSceneManagerSetup(entries.Select(e => new SceneSetup { path = e.path, isLoaded = e.loaded, isActive = e.active }).ToArray());
    }
}
