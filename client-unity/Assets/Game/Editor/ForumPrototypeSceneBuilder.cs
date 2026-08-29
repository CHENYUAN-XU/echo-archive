using EchoForum.Infrastructure;
using EchoForum.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace EchoForum.Editor
{
    public static class ForumPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Game/Scenes/ForumPrototype.unity";
        private const string PanelSettingsPath = "Assets/Game/Presentation/ForumPrototypePanelSettings.asset";
        private const string DocumentPath = "Assets/Game/Presentation/ForumPrototype.uxml";

        [MenuItem("Echo Archive/Create Forum Prototype Scene")]
        public static void CreateOrUpdate()
        {
            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panelSettings, PanelSettingsPath);
            }

            var document = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(DocumentPath);
            if (document == null)
            {
                Debug.LogError("ForumPrototype.uxml was not imported. Reimport the Presentation folder and run this command again.");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var forumRoot = new GameObject("ForumPrototype");
            var uiDocument = forumRoot.AddComponent<UIDocument>();
            uiDocument.panelSettings = panelSettings;
            uiDocument.visualTreeAsset = document;
            forumRoot.AddComponent<ForumPrototypeController>();
            forumRoot.AddComponent<EchoForum.Bootstrap.ForumPrototypeBootstrapper>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Forum prototype scene created at " + ScenePath);
        }
    }
}
