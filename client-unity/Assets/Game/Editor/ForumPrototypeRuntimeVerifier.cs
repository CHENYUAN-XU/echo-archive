using EchoForum.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace EchoForum.Editor
{
    /// <summary>
    /// Batch-friendly smoke test for the prototype's runtime navigation.
    /// </summary>
    public static class ForumPrototypeRuntimeVerifier
    {
        private static int framesUntilCheck;
        private static bool originalEnterPlayModeOptionsEnabled;
        private static EnterPlayModeOptions originalEnterPlayModeOptions;

        [MenuItem("Echo Archive/Verify Forum Prototype")]
        public static void Verify()
        {
            EditorSceneManager.OpenScene("Assets/Game/Scenes/ForumPrototype.unity");
            originalEnterPlayModeOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            originalEnterPlayModeOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = originalEnterPlayModeOptions | EnterPlayModeOptions.DisableDomainReload;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            framesUntilCheck = 4;
            EditorApplication.update += WaitForForumToInitialize;
        }

        private static void WaitForForumToInitialize()
        {
            if (--framesUntilCheck > 0)
            {
                return;
            }

            EditorApplication.update -= WaitForForumToInitialize;
            try
            {
                var controller = Object.FindFirstObjectByType<ForumPrototypeController>();
                var document = controller == null ? null : controller.GetComponent<UIDocument>();
                var root = document == null ? null : document.rootVisualElement;
                var threadList = root == null ? null : root.Q<VisualElement>("thread-list");
                var detailView = root == null ? null : root.Q<VisualElement>("detail-view");
                var homeView = root == null ? null : root.Q<VisualElement>("home-view");
                var backButton = root == null ? null : root.Q<Button>("back-button");

                if (threadList == null || detailView == null || homeView == null || backButton == null || threadList.childCount < 4)
                {
                    throw new System.InvalidOperationException("Forum home did not initialize its expected UI elements.");
                }

                controller.GetType().GetMethod("ShowThread", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(controller, new object[] { "thread-guidelines" });
                if (detailView.style.display.value != DisplayStyle.Flex)
                {
                    throw new System.InvalidOperationException("Selecting a thread did not open the detail view.");
                }

                controller.GetType().GetMethod("ShowHome", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(controller, null);
                if (homeView.style.display.value != DisplayStyle.Flex)
                {
                    throw new System.InvalidOperationException("Back navigation did not restore the thread list.");
                }

                Debug.Log("Forum prototype runtime verification passed: home list, detail navigation, and return navigation.");
                RestorePlayModeSettings();
                EditorApplication.Exit(0);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                RestorePlayModeSettings();
                EditorApplication.Exit(1);
            }
        }
        private static void RestorePlayModeSettings()
        {
            EditorSettings.enterPlayModeOptionsEnabled = originalEnterPlayModeOptionsEnabled;
            EditorSettings.enterPlayModeOptions = originalEnterPlayModeOptions;
        }

    }
}
