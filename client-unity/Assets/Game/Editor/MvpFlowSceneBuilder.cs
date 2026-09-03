using EchoForum.Bootstrap;
using EchoForum.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace EchoForum.Editor
{
    public static class MvpFlowSceneBuilder
    {
        private const string BootPath = "Assets/Game/Scenes/Boot.unity";
        private const string MainMenuPath = "Assets/Game/Scenes/MainMenu.unity";

        [MenuItem("Echo Archive/Create MVP Flow Scenes")]
        public static void CreateOrUpdate()
        {
            ForumPrototypeSceneBuilder.CreateOrUpdate();
            InvestigationPrototypeSceneBuilder.CreateOrUpdate();
            CreateBootScene();
            CreateMainMenuScene();
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootPath, true),
                new EditorBuildSettingsScene(MainMenuPath, true),
                new EditorBuildSettingsScene("Assets/Game/Scenes/ForumPrototype.unity", true),
                new EditorBuildSettingsScene("Assets/Game/Scenes/InvestigationPrototype.unity", true)
            };
            AssetDatabase.SaveAssets();
            Debug.Log("MVP flow scenes created: Boot, MainMenu, ForumPrototype, InvestigationPrototype.");
        }

        private static void CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, BootPath);
        }

        private static void CreateMainMenuScene()
        {
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Game/Presentation/ForumPrototypePanelSettings.asset");
            var document = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Game/Presentation/MainMenu.uxml");
            if (panel == null || document == null) throw new System.InvalidOperationException("Main menu UI Toolkit assets were not imported.");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("MainMenu");
            var ui = root.AddComponent<UIDocument>();
            ui.panelSettings = panel;
            ui.visualTreeAsset = document;
            root.AddComponent<MainMenuController>();
            root.AddComponent<MainMenuBootstrapper>();
            EditorSceneManager.SaveScene(scene, MainMenuPath);
        }
    }
}
