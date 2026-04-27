#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XRCore.Agents;
using XRCore.Core;
using XRCore.Tasks;
using XRCore.UI;
using XRCore.Vision;

namespace XRCore.UI.Editor
{
    // Editor-only setup helpers for XRCore scene bootstrap and demo presets.
    public enum XRCoreReasonerPreset
    {
        None = 0,
        RuleEngine = 1,
        StateMachine = 2,
        LocalLlm = 3,
        ApiLlm = 4
    }

    public enum XRCoreDetectionProviderPreset
    {
        None = 0,
        Raycast = 1,
        Simulation = 2,
        Sentis = 3,
        VisionApi = 4
    }

    public sealed class XRCoreSetupOptions
    {
        public XRCoreReasonerPreset ReasonerPreset = XRCoreReasonerPreset.RuleEngine;
        public XRCoreDetectionProviderPreset DetectionProviderPreset = XRCoreDetectionProviderPreset.Raycast;
        public XRCoreDemoPreset DemoPreset = XRCoreDemoPreset.Beginner;
    }

    public enum XRCoreDemoPreset
    {
        Beginner = 0,
        Strict = 1,
        Fast = 2
    }

    public static class XRCoreSceneSetupMenu
    {
        private const string DefaultSettingsAssetPath = "Assets/XRCore/Resources/XRCoreSettings.asset";
        private const string DefaultReasonersFolder = "Assets/XRCore/Resources/Reasoners";

        [MenuItem("GameObject/XRCore/Setup XR Assistant", false, 10)]
        private static void SetupXRCoreAssistantFromMenu(MenuCommand command)
        {
            Transform parent = (command.context as GameObject)?.transform;
            SetupXRCoreAssistant(parent, null);
        }

        [MenuItem("Tools/XRCore/Setup Wizard/Apply Demo Preset/Beginner", false, 20)]
        private static void ApplyDemoPresetBeginnerMenu()
        {
            ApplyDemoPresetToCurrentScene(XRCoreDemoPreset.Beginner);
        }

        [MenuItem("Tools/XRCore/Setup Wizard/Apply Demo Preset/Strict", false, 21)]
        private static void ApplyDemoPresetStrictMenu()
        {
            ApplyDemoPresetToCurrentScene(XRCoreDemoPreset.Strict);
        }

        [MenuItem("Tools/XRCore/Setup Wizard/Apply Demo Preset/Fast", false, 22)]
        private static void ApplyDemoPresetFastMenu()
        {
            ApplyDemoPresetToCurrentScene(XRCoreDemoPreset.Fast);
        }

        public static GameObject SetupXRCoreAssistant(Transform parent, XRCoreSetupOptions options)
        {
            options ??= new XRCoreSetupOptions();

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup XR Assistant");

            GameObject root = null;
            try
            {
                XRCoreSettings settings = GetOrCreateSettingsAsset();

                root = GetOrCreate("XRCore", parent);
                GetOrAdd<XRTaskRunner>(root);
                XRGuideAgent guideAgent = GetOrAdd<XRGuideAgent>(root);
                XRCoreInstaller installer = GetOrAdd<XRCoreInstaller>(root);

                GameObject debug = GetOrCreate("XRCoreDebug", root.transform);
                XRCoreRuntimeStats runtimeStats = GetOrAdd<XRCoreRuntimeStats>(debug);
                XRCoreDiagnosticsOverlay diagnostics = GetOrAdd<XRCoreDiagnosticsOverlay>(debug);
                SetObjectReference(diagnostics, "runtimeStats", runtimeStats);

                GameObject audio = GetOrCreate("XRCoreAudio", root.transform);
                AudioSource audioSource = GetOrAdd<AudioSource>(audio);
                XRGuideInstructionAudio instructionAudio = GetOrAdd<XRGuideInstructionAudio>(audio);
                SetObjectReference(instructionAudio, "audioSource", audioSource);

                GameObject canvasGo = GetOrCreate("XRCoreCanvas", root.transform);
                Canvas canvas = GetOrAdd<Canvas>(canvasGo);
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                AddOptionalUiComponent(canvasGo, "UnityEngine.UI.CanvasScaler, UnityEngine.UI");
                AddOptionalUiComponent(canvasGo, "UnityEngine.UI.GraphicRaycaster, UnityEngine.UI");
                XRGuideInstructionUI instructionUi = GetOrAdd<XRGuideInstructionUI>(canvasGo);

                GameObject presenterGo = GetOrCreate("XRInstructionPresenter", root.transform);
                XRGuideInstructionPresenter presenter = GetOrAdd<XRGuideInstructionPresenter>(presenterGo);
                SetObjectReference(presenter, "instructionUI", instructionUi);
                SetObjectReference(presenter, "instructionAudio", instructionAudio);

                SetObjectReference(installer, "settings", settings);
                SetObjectReference(installer, "guideAgent", guideAgent);
                SetObjectReference(installer, "instructionPresenter", presenter);
                SetObjectReference(installer, "diagnosticsOverlay", diagnostics);
                SetBool(installer, "autoFindComponents", true);

                ConfigureReasonerPreset(guideAgent, options.ReasonerPreset);
                ConfigureDetectionPreset(root, options.DetectionProviderPreset);
                ApplyDemoPreset(root, options.DemoPreset);

                Selection.activeGameObject = root;
                EditorSceneManager.MarkSceneDirty(root.scene);
            }
            finally
            {
                Undo.CollapseUndoOperations(group);
            }

            return root;
        }

        private static void ApplyDemoPresetToCurrentScene(XRCoreDemoPreset preset)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Apply XRCore Demo Preset ({preset})");

            try
            {
                GameObject root = FindBestRootForPreset();
                ApplyDemoPreset(root, preset);
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            }
            finally
            {
                Undo.CollapseUndoOperations(group);
            }
        }

        private static GameObject FindBestRootForPreset()
        {
            var selected = Selection.activeGameObject;
            if (selected != null)
            {
                Transform current = selected.transform;
                while (current != null)
                {
                    if (current.name == "XRCore")
                    {
                        return current.gameObject;
                    }

                    current = current.parent;
                }

                return selected;
            }

            XRCoreInstaller installer = Object.FindFirstObjectByType<XRCoreInstaller>(FindObjectsInactive.Include);
            if (installer != null)
            {
                return installer.gameObject;
            }

            return null;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : Undo.AddComponent<T>(go);
        }

        private static GameObject GetOrCreate(string name, Transform parent)
        {
            if (parent == null)
            {
                var sceneRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                for (int i = 0; i < sceneRoots.Length; i++)
                {
                    if (sceneRoots[i].name == name)
                    {
                        return sceneRoots[i];
                    }
                }
            }
            else
            {
                Transform existingChild = parent.Find(name);
                if (existingChild != null)
                {
                    return existingChild.gameObject;
                }
            }

            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            if (parent != null)
            {
                Undo.SetTransformParent(go.transform, parent, "Parent " + name);
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
            }

            return go;
        }

        private static XRCoreSettings GetOrCreateSettingsAsset()
        {
            string[] existing = AssetDatabase.FindAssets("t:XRCoreSettings");
            if (existing.Length > 0)
            {
                string existingPath = AssetDatabase.GUIDToAssetPath(existing[0]);
                var loaded = AssetDatabase.LoadAssetAtPath<XRCoreSettings>(existingPath);
                if (loaded != null)
                {
                    return loaded;
                }
            }

            EnsureFolder("Assets/XRCore");
            EnsureFolder("Assets/XRCore/Resources");

            var settings = ScriptableObject.CreateInstance<XRCoreSettings>();
            AssetDatabase.CreateAsset(settings, DefaultSettingsAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return settings;
        }

        private static void ConfigureReasonerPreset(XRGuideAgent guideAgent, XRCoreReasonerPreset preset)
        {
            XRGuideReasonerBase reasoner = preset switch
            {
                XRCoreReasonerPreset.RuleEngine => GetOrCreateAsset<RuleEngineReasoner>(Path.Combine(DefaultReasonersFolder, "RuleEngineReasoner.asset")),
                XRCoreReasonerPreset.StateMachine => GetOrCreateAsset<StateMachineReasoner>(Path.Combine(DefaultReasonersFolder, "StateMachineReasoner.asset")),
                XRCoreReasonerPreset.LocalLlm => GetOrCreateAsset<LocalLlmReasoner>(Path.Combine(DefaultReasonersFolder, "LocalLlmReasoner.asset")),
                XRCoreReasonerPreset.ApiLlm => GetOrCreateAsset<ApiLlmReasoner>(Path.Combine(DefaultReasonersFolder, "ApiLlmReasoner.asset")),
                _ => null
            };

            SetObjectReference(guideAgent, "reasoner", reasoner);
        }

        private static void ConfigureDetectionPreset(GameObject root, XRCoreDetectionProviderPreset preset)
        {
            GameObject visionRoot = GetOrCreate("XRCoreVision", root.transform);

            RemoveComponentIfExists<RaycastDetectionProvider>(visionRoot);
            RemoveComponentIfExists<SimulationDetectionProvider>(visionRoot);
            RemoveComponentIfExists<SentisDetectionProvider>(visionRoot);
            RemoveComponentIfExists<VisionApiDetectionProvider>(visionRoot);

            MonoBehaviour selectedProvider = preset switch
            {
                XRCoreDetectionProviderPreset.Raycast => GetOrAdd<RaycastDetectionProvider>(visionRoot),
                XRCoreDetectionProviderPreset.Simulation => GetOrAdd<SimulationDetectionProvider>(visionRoot),
                XRCoreDetectionProviderPreset.Sentis => GetOrAdd<SentisDetectionProvider>(visionRoot),
                XRCoreDetectionProviderPreset.VisionApi => GetOrAdd<VisionApiDetectionProvider>(visionRoot),
                _ => null
            };

            DetectionEventPublisher publisher = GetOrAdd<DetectionEventPublisher>(visionRoot);
            SetObjectReference(publisher, "providerBehaviour", selectedProvider);
            SetBool(publisher, "publishOnUpdate", selectedProvider != null);
            SetString(publisher, "source", GetProviderSource(preset));
        }

        private static void ApplyDemoPreset(GameObject root, XRCoreDemoPreset preset)
        {
            XRGuideAgent[] guideAgents = root != null
                ? root.GetComponentsInChildren<XRGuideAgent>(true)
                : Object.FindObjectsByType<XRGuideAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < guideAgents.Length; i++)
            {
                ConfigureGuideAgentForPreset(guideAgents[i], preset);
            }

            MonoBehaviour[] bridges = FindMonoBehavioursByTypeName(root, "XRCore.Samples.VisionDetectionToSignalBridge");
            for (int i = 0; i < bridges.Length; i++)
            {
                ConfigureBridgeForPreset(bridges[i], preset);
            }

            XRGuideInstructionUI[] instructionUi = root != null
                ? root.GetComponentsInChildren<XRGuideInstructionUI>(true)
                : Object.FindObjectsByType<XRGuideInstructionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < instructionUi.Length; i++)
            {
                ConfigureInstructionUiForPreset(instructionUi[i], preset);
            }

            XRCoreDiagnosticsOverlay[] diagnostics = root != null
                ? root.GetComponentsInChildren<XRCoreDiagnosticsOverlay>(true)
                : Object.FindObjectsByType<XRCoreDiagnosticsOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < diagnostics.Length; i++)
            {
                ConfigureDiagnosticsForPreset(diagnostics[i], preset);
            }
        }

        private static void ConfigureGuideAgentForPreset(XRGuideAgent guideAgent, XRCoreDemoPreset preset)
        {
            if (guideAgent == null)
            {
                return;
            }

            Undo.RecordObject(guideAgent, "Configure XRGuideAgent preset");
            switch (preset)
            {
                case XRCoreDemoPreset.Beginner:
                    SetFloat(guideAgent, "evaluationTickRateSeconds", 0.25f);
                    SetBool(guideAgent, "suppressRepeatedMessages", true);
                    SetFloat(guideAgent, "repeatedMessageCooldownSeconds", 2.5f);
                    SetBool(guideAgent, "logInstructions", true);
                    break;
                case XRCoreDemoPreset.Strict:
                    SetFloat(guideAgent, "evaluationTickRateSeconds", 0.18f);
                    SetBool(guideAgent, "suppressRepeatedMessages", true);
                    SetFloat(guideAgent, "repeatedMessageCooldownSeconds", 1f);
                    SetBool(guideAgent, "logInstructions", true);
                    break;
                case XRCoreDemoPreset.Fast:
                    SetFloat(guideAgent, "evaluationTickRateSeconds", 0.1f);
                    SetBool(guideAgent, "suppressRepeatedMessages", true);
                    SetFloat(guideAgent, "repeatedMessageCooldownSeconds", 0.5f);
                    SetBool(guideAgent, "logInstructions", false);
                    break;
            }
        }

        private static void ConfigureBridgeForPreset(MonoBehaviour bridge, XRCoreDemoPreset preset)
        {
            if (bridge == null)
            {
                return;
            }

            Undo.RecordObject(bridge, "Configure Vision bridge preset");
            SetBool(bridge, "emitOnlyWhileTaskRunning", true);
            SetBool(bridge, "restartTaskOnLookAwayAfterCompletion", true);

            switch (preset)
            {
                case XRCoreDemoPreset.Beginner:
                    SetFloat(bridge, "cooldownSeconds", 1.2f);
                    SetFloat(bridge, "targetAcquireSeconds", 0.2f);
                    SetFloat(bridge, "targetLostGraceSeconds", 0.35f);
                    break;
                case XRCoreDemoPreset.Strict:
                    SetFloat(bridge, "cooldownSeconds", 0.8f);
                    SetFloat(bridge, "targetAcquireSeconds", 0.12f);
                    SetFloat(bridge, "targetLostGraceSeconds", 0.2f);
                    break;
                case XRCoreDemoPreset.Fast:
                    SetFloat(bridge, "cooldownSeconds", 0.35f);
                    SetFloat(bridge, "targetAcquireSeconds", 0.05f);
                    SetFloat(bridge, "targetLostGraceSeconds", 0.08f);
                    break;
            }
        }

        private static MonoBehaviour[] FindMonoBehavioursByTypeName(GameObject root, string fullTypeName)
        {
            MonoBehaviour[] candidates = root != null
                ? root.GetComponentsInChildren<MonoBehaviour>(true)
                : Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            var matches = new System.Collections.Generic.List<MonoBehaviour>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                MonoBehaviour candidate = candidates[i];
                if (candidate == null)
                {
                    continue;
                }

                System.Type type = candidate.GetType();
                if (type != null && string.Equals(type.FullName, fullTypeName, System.StringComparison.Ordinal))
                {
                    matches.Add(candidate);
                }
            }

            return matches.ToArray();
        }

        private static void ConfigureInstructionUiForPreset(XRGuideInstructionUI instructionUi, XRCoreDemoPreset preset)
        {
            if (instructionUi == null)
            {
                return;
            }

            Undo.RecordObject(instructionUi, "Configure Instruction UI preset");
            SetBool(instructionUi, "persistMessageUntilReplaced", true);
            switch (preset)
            {
                case XRCoreDemoPreset.Beginner:
                    SetFloat(instructionUi, "visibleSeconds", 5f);
                    break;
                case XRCoreDemoPreset.Strict:
                    SetFloat(instructionUi, "visibleSeconds", 3f);
                    break;
                case XRCoreDemoPreset.Fast:
                    SetFloat(instructionUi, "visibleSeconds", 2f);
                    break;
            }
        }

        private static void ConfigureDiagnosticsForPreset(XRCoreDiagnosticsOverlay diagnostics, XRCoreDemoPreset preset)
        {
            if (diagnostics == null)
            {
                return;
            }

            Undo.RecordObject(diagnostics, "Configure Diagnostics preset");
            switch (preset)
            {
                case XRCoreDemoPreset.Beginner:
                    // Use explicit enum index for "full/verbose" mode to avoid hard dependency
                    // on a specific enum member name across package revisions.
                    SetInt(diagnostics, "diagnosticsMode", 2);
                    break;
                case XRCoreDemoPreset.Strict:
                    SetInt(diagnostics, "diagnosticsMode", (int)XRCoreDiagnosticsMode.Minimal);
                    break;
                case XRCoreDemoPreset.Fast:
                    SetInt(diagnostics, "diagnosticsMode", (int)XRCoreDiagnosticsMode.Disabled);
                    break;
            }
        }

        private static string GetProviderSource(XRCoreDetectionProviderPreset preset)
        {
            return preset switch
            {
                XRCoreDetectionProviderPreset.Raycast => "raycast",
                XRCoreDetectionProviderPreset.Simulation => "simulation",
                XRCoreDetectionProviderPreset.Sentis => "sentis",
                XRCoreDetectionProviderPreset.VisionApi => "vision.api",
                _ => "vision"
            };
        }

        private static T GetOrCreateAsset<T>(string assetPath) where T : ScriptableObject
        {
            string normalizedPath = assetPath.Replace("\\", "/");
            T existingAtPath = AssetDatabase.LoadAssetAtPath<T>(normalizedPath);
            if (existingAtPath != null)
            {
                return existingAtPath;
            }

            string[] matches = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (matches.Length > 0)
            {
                string matchPath = AssetDatabase.GUIDToAssetPath(matches[0]);
                T existing = AssetDatabase.LoadAssetAtPath<T>(matchPath);
                if (existing != null)
                {
                    return existing;
                }
            }

            string folder = Path.GetDirectoryName(normalizedPath)?.Replace("\\", "/");
            if (!string.IsNullOrWhiteSpace(folder))
            {
                EnsureFolder(folder);
            }

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, normalizedPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int lastSlash = path.LastIndexOf('/');
            string parent = path.Substring(0, lastSlash);
            string folderName = path.Substring(lastSlash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void RemoveComponentIfExists<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            if (component != null)
            {
                Undo.DestroyObjectImmediate(component);
            }
        }

        private static void AddOptionalUiComponent(GameObject go, string assemblyQualifiedTypeName)
        {
            var type = System.Type.GetType(assemblyQualifiedTypeName, throwOnError: false);
            if (type == null || !typeof(Component).IsAssignableFrom(type))
            {
                return;
            }

            if (go.GetComponent(type) != null)
            {
                return;
            }

            Undo.AddComponent(go, type);
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.boolValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.stringValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.floatValue = value;
            serializedObject.ApplyModifiedProperties();
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            var serializedObject = new SerializedObject(target);
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.intValue = value;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
