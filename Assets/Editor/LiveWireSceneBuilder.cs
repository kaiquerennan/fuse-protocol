using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using LiveWire;

namespace LiveWireEditor
{
    [InitializeOnLoad]
    public static class LiveWireSceneBuilder
    {
        const string SceneFolder = "Assets/Scenes";
        const string MainMenuPath = SceneFolder + "/MainMenu.unity";
        const string GameScenePath = SceneFolder + "/GameScene.unity";
        const string GameOverPath = SceneFolder + "/GameOver.unity";

        static LiveWireSceneBuilder()
        {
            EditorApplication.delayCall += TryBuildOnStartup;
        }

        [MenuItem("LiveWire/Build Scenes")]
        public static void ForceRebuild()
        {
            BuildAll(force: true);
            AddAllToBuildSettings();
        }

        [MenuItem("LiveWire/Add Scenes To Build Settings")]
        public static void AddToBuildMenu()
        {
            AddAllToBuildSettings();
        }

        static void TryBuildOnStartup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            BuildAll(force: false);
            AddAllToBuildSettings();
        }

        static void BuildAll(bool force)
        {
            if (!Directory.Exists(SceneFolder)) Directory.CreateDirectory(SceneFolder);

            string current = EditorSceneManager.GetActiveScene().path;

            BuildMenuScene(force);
            BuildGameScene(force);
            BuildGameOverScene(force);

            if (!string.IsNullOrEmpty(current) && File.Exists(current))
            {
                EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
            }
            else
            {
                EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
            }
        }

        static void BuildMenuScene(bool force)
        {
            if (!force && File.Exists(MainMenuPath)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("MainMenuBootstrap");
            go.AddComponent<MainMenuBootstrap>();
            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }

        static void BuildGameScene(bool force)
        {
            if (!force && File.Exists(GameScenePath)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("GameSceneBootstrap");
            go.AddComponent<GameSceneBootstrap>();
            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        static void BuildGameOverScene(bool force)
        {
            if (!force && File.Exists(GameOverPath)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("GameOverBootstrap");
            go.AddComponent<GameOverBootstrap>();
            EditorSceneManager.SaveScene(scene, GameOverPath);
        }

        static void AddAllToBuildSettings()
        {
            var wanted = new[] { MainMenuPath, GameScenePath, GameOverPath };
            var current = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var existing = new HashSet<string>();
            foreach (var s in current) existing.Add(s.path);

            var result = new List<EditorBuildSettingsScene>();
            foreach (var path in wanted)
            {
                if (!File.Exists(path)) continue;
                result.Add(new EditorBuildSettingsScene(path, true));
            }

            foreach (var s in current)
            {
                if (System.Array.IndexOf(wanted, s.path) >= 0) continue;
                result.Add(s);
            }

            EditorBuildSettings.scenes = result.ToArray();
        }
    }
}
