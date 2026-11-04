using System;
using KnightOrder.Battle.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace KnightOrder.Editor
{
    /// <summary>
    /// Builds the Battle scene and the Windows player from the command line, so the scene is reproducible
    /// instead of hand-edited. Implements: Story 002 (playable timing battle).
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
        private const string DATA_DIR = "Assets/Data/Battle/";
        private const string PLAYER_PATH = "Builds/Win64/KnightOrder.exe";

        /// <summary>Creates (or overwrites) Assets/Scenes/Battle.unity and puts it first in the build list.</summary>
        [MenuItem("Knight Order/Build Battle Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<AudioListener>();

            var battle = new GameObject("Battle");
            var view = battle.AddComponent<ChargeTimingBattleView>();
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_battleRulesJson").objectReferenceValue = LoadText("battle_rules.json");
            serialized.FindProperty("_battleSetupJson").objectReferenceValue = LoadText("battle_sample_setup.json");
            serialized.FindProperty("_chargeTimingJson").objectReferenceValue = LoadText("battle_charge_timing.json");
            serialized.ApplyModifiedPropertiesWithoutUndo();

            System.IO.Directory.CreateDirectory("Assets/Scenes");
            if (!EditorSceneManager.SaveScene(scene, SCENE_PATH))
            {
                throw new InvalidOperationException("Could not save " + SCENE_PATH);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(SCENE_PATH, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("BattleSceneBuilder: saved " + SCENE_PATH);
        }

        /// <summary>Builds the scene, then a Windows 64-bit player at Builds/Win64/KnightOrder.exe.</summary>
        public static void BuildWindowsPlayer()
        {
            BuildScene();
            var options = new BuildPlayerOptions
            {
                scenes = new[] { SCENE_PATH },
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

        private static TextAsset LoadText(string fileName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(DATA_DIR + fileName);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing data file " + DATA_DIR + fileName);
            }

            return asset;
        }
    }
}
