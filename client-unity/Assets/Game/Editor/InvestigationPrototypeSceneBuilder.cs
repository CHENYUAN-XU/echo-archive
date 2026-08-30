using EchoForum.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace EchoForum.Editor
{
    public static class InvestigationPrototypeSceneBuilder
    {
        [MenuItem("Echo Archive/Create Investigation Prototype Scene")]
        public static void CreateOrUpdate()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var controller = new GameObject("InvestigationPrototype").AddComponent<InvestigationPrototypeController>();
            controller.gameObject.AddComponent<UIDocument>().panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/Game/Presentation/ForumPrototypePanelSettings.asset");
            controller.gameObject.AddComponent<EchoForum.Bootstrap.InvestigationPrototypeBootstrapper>();
            CreateCube("Floor", new Vector3(0, -0.5f, 0), new Vector3(12, 1, 18), new Color(.08f, .12f, .12f));
            CreateCube("NorthWall", new Vector3(0, 2, 9), new Vector3(12, 5, 1), new Color(.12f, .17f, .16f));
            CreateCube("WestWall", new Vector3(-6, 2, 0), new Vector3(1, 5, 18), new Color(.12f, .17f, .16f));
            CreateCube("EastWall", new Vector3(6, 2, 0), new Vector3(1, 5, 18), new Color(.12f, .17f, .16f));
            CreatePoint("观测点 A", "observation-a", new Vector3(0, .6f, 3), new Color(.72f, .7f, .3f));
            CreatePoint("记录残片", "record-fragment", new Vector3(-3, .6f, -2), new Color(.35f, .65f, .7f));
            CreatePoint("处置对象", "disposal-object", new Vector3(3, .6f, -6), new Color(.65f, .35f, .45f));
            var player = new GameObject("Observer"); player.transform.position = new Vector3(0, 1, -7); player.AddComponent<CharacterController>(); var input = player.AddComponent<InvestigationPlayerController>(); input.Controller = controller;
            var camera = new GameObject("Observation Camera"); camera.transform.SetParent(player.transform); camera.transform.localPosition = new Vector3(0, .65f, 0); camera.AddComponent<Camera>();
            var light = new GameObject("Muted Light"); light.transform.position = new Vector3(0, 4, 0); var source = light.AddComponent<Light>(); source.type = LightType.Point; source.range = 14; source.intensity = 3; source.color = new Color(.55f, .7f, .62f);
            RenderSettings.ambientLight = new Color(.08f, .1f, .1f);
            EditorSceneManager.SaveScene(scene, "Assets/Game/Scenes/InvestigationPrototype.unity");
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Game/Scenes/ForumPrototype.unity", true), new EditorBuildSettingsScene("Assets/Game/Scenes/InvestigationPrototype.unity", true) };
        }
        private static void CreatePoint(string name, string id, Vector3 position, Color color) { var point = CreateCube(name, position, Vector3.one, color); point.AddComponent<InvestigationPoint>().PointId = id; }
        private static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Color color) { var item = GameObject.CreatePrimitive(PrimitiveType.Cube); item.name = name; item.transform.position = position; item.transform.localScale = scale; item.GetComponent<Renderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = color }; return item; }
    }
}
