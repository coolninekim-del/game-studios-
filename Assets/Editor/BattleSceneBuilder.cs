using System;
using KnightOrder.Battle.Presentation;
using KnightOrder.Duel.Presentation;
using KnightOrder.UI.Stable;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KnightOrder.Editor
{
    /// <summary>
    /// Builds the Battle, Duel and Stable scenes and the Windows player from the command line, so the scenes are
    /// reproducible instead of hand-edited. Implements: Story 002 (playable timing battle), Story 004 (duel scene),
    /// Story 005 (stable scene; the battle and duel read the trained profile).
    ///
    /// Usage:
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod KnightOrder.Editor.BattleSceneBuilder.BuildScene
    /// Unity.exe -batchmode -quit -projectPath . -executeMethod KnightOrder.Editor.BattleSceneBuilder.BuildWindowsPlayer
    /// </code>
    /// </summary>
    public static class BattleSceneBuilder
    {
        private const string SCENE_PATH = "Assets/Scenes/Battle.unity";
        private const string DUEL_SCENE_PATH = "Assets/Scenes/" + DuelSceneLauncher.SCENE_NAME + ".unity";
        private const string STABLE_SCENE_PATH = "Assets/Scenes/" + StableSceneLauncher.SCENE_NAME + ".unity";
        private const string DATA_DIR = "Assets/Data/Battle/";
        private const string DUEL_DATA_DIR = "Assets/Data/Duel/";
        private const string STABLE_DATA_DIR = "Assets/Data/Stable/";
        private const string PLAYER_PATH = "Builds/Win64/KnightOrder.exe";

        /// <summary>
        /// Creates (or overwrites) Assets/Scenes/Battle.unity, Duel.unity and Stable.unity and sets the build list
        /// to Battle (first, the startup scene), Duel, Stable.
        /// </summary>
        [MenuItem("Knight Order/Build Battle Scene")]
        public static void BuildScene()
        {
            var scene = NewSceneWithCamera();
            var battle = new GameObject("Battle");
            var view = battle.AddComponent<ChargeTimingBattleView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_battleRulesJson").objectReferenceValue = LoadText("battle_rules.json");
            serialized.FindProperty("_battleSetupJson").objectReferenceValue = LoadText("battle_sample_setup.json");
            serialized.FindProperty("_chargeTimingJson").objectReferenceValue = LoadText("battle_charge_timing.json");
            serialized.FindProperty("_stableRulesJson").objectReferenceValue = LoadText(STABLE_DATA_DIR, "stable_rules.json");
            serialized.FindProperty("_startProfileJson").objectReferenceValue = LoadText(STABLE_DATA_DIR, "stable_start_profile.json");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            SaveScene(scene, SCENE_PATH);

            var duelScene = NewSceneWithCamera();
            var duel = new GameObject("Duel");
            var duelView = duel.AddComponent<DuelView>();
            var duelSerialized = new SerializedObject(duelView);
            duelSerialized.FindProperty("_duelRulesJson").objectReferenceValue = LoadText(DUEL_DATA_DIR, "duel_rules.json");
            duelSerialized.FindProperty("_duelSetupJson").objectReferenceValue = LoadText(DUEL_DATA_DIR, "duel_sample_setup.json");
            duelSerialized.FindProperty("_startProfileJson").objectReferenceValue = LoadText(STABLE_DATA_DIR, "stable_start_profile.json");
            duelSerialized.ApplyModifiedPropertiesWithoutUndo();
            SaveScene(duelScene, DUEL_SCENE_PATH);

            var stableScene = NewSceneWithCamera();
            var stable = new GameObject("Stable");
            var stableView = stable.AddComponent<StableView>();
            var stableSerialized = new SerializedObject(stableView);
            stableSerialized.FindProperty("_stableRulesJson").objectReferenceValue = LoadText(STABLE_DATA_DIR, "stable_rules.json");
            stableSerialized.FindProperty("_startProfileJson").objectReferenceValue = LoadText(STABLE_DATA_DIR, "stable_start_profile.json");
            stableSerialized.FindProperty("_chargeTimingJson").objectReferenceValue = LoadText("battle_charge_timing.json");
            stableSerialized.FindProperty("_duelRulesJson").objectReferenceValue = LoadText(DUEL_DATA_DIR, "duel_rules.json");
            stableSerialized.FindProperty("_battleSceneName").stringValue = "Battle";
            stableSerialized.FindProperty("_duelSceneName").stringValue = DuelSceneLauncher.SCENE_NAME;
            stableSerialized.ApplyModifiedPropertiesWithoutUndo();
            SaveScene(stableScene, STABLE_SCENE_PATH);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SCENE_PATH, true),
                new EditorBuildSettingsScene(DUEL_SCENE_PATH, true),
                new EditorBuildSettingsScene(STABLE_SCENE_PATH, true)
            };
            AssetDatabase.SaveAssets();
        }

        /// <summary>Builds the scene, then a Windows 64-bit player at Builds/Win64/KnightOrder.exe.</summary>
        public static void BuildWindowsPlayer()
        {
            BuildScene();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { SCENE_PATH, DUEL_SCENE_PATH, STABLE_SCENE_PATH },
                locationPathName = PLAYER_PATH,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new InvalidOperationException("Player build failed: " + report.summary.result);
            }

            Debug.Log("BattleSceneBuilder: built " + PLAYER_PATH);
        }

        private static UnityEngine.SceneManagement.Scene NewSceneWithCamera()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();
            return scene;
        }

        private static void SaveScene(UnityEngine.SceneManagement.Scene scene, string path)
        {
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            if (!EditorSceneManager.SaveScene(scene, path))
            {
                throw new InvalidOperationException("Could not save " + path);
            }

            Debug.Log("BattleSceneBuilder: saved " + path);
        }

        private static TextAsset LoadText(string fileName)
        {
            return LoadText(DATA_DIR, fileName);
        }

        private static TextAsset LoadText(string directory, string fileName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(directory + fileName);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing data file " + directory + fileName);
            }

            return asset;
        }
    }
}
