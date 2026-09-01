using System;
using System.Collections.Generic;
using System.IO;
using EchoForum.Content;
using UnityEditor;
using UnityEngine;

namespace EchoForum.Editor
{
    public static class ContentBindingValidator
    {
        [MenuItem("Echo Archive/Content/Validate Bindings")]
        public static void Validate()
        {
            var errors = new List<string>();
            var threadIds = Unique(Resources.LoadAll<ForumThreadDefinition>("Forum"), definition => definition.ThreadId, "ThreadId", errors);
            var cases = Unique(Resources.LoadAll<CaseDefinitionAsset>("Cases"), definition => definition.CaseId, "CaseId", errors);
            foreach (var binding in Resources.LoadAll<ThreadCaseBindingAsset>("Bindings"))
            {
                if (binding == null) continue;
                if (!threadIds.ContainsKey(binding.ThreadId)) errors.Add("Binding references missing ThreadId: " + binding.ThreadId);
                if (!cases.TryGetValue(binding.CaseId, out var definition)) { errors.Add("Binding references missing CaseId: " + binding.CaseId); continue; }
                if (binding.AllowsEntry && (!definition.IsAvailableInCurrentBuild || string.IsNullOrEmpty(definition.InvestigationSceneName) || !SceneIsInBuild(definition.InvestigationSceneName))) errors.Add("Enterable binding has no valid enabled scene: " + binding.ThreadId + " -> " + binding.CaseId);
            }
            if (errors.Count > 0) throw new InvalidOperationException("Content binding validation failed:\n- " + string.Join("\n- ", errors));
            Debug.Log("Content binding validation passed.");
        }

        private static Dictionary<string, T> Unique<T>(T[] definitions, Func<T, string> id, string label, List<string> errors) where T : UnityEngine.Object
        {
            var result = new Dictionary<string, T>();
            foreach (var definition in definitions)
            {
                var value = definition == null ? null : id(definition);
                if (string.IsNullOrEmpty(value)) { errors.Add(label + " is empty on " + (definition == null ? "<null>" : definition.name)); continue; }
                if (!result.TryAdd(value, definition)) errors.Add(label + " is duplicated: " + value);
            }
            return result;
        }

        private static bool SceneIsInBuild(string sceneName)
        {
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled && Path.GetFileNameWithoutExtension(scene.path) == sceneName) return true;
            }
            return false;
        }
    }
}
