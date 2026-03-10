#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using XRCore.Agents;
using XRCore.Core;
using XRCore.Tasks;
using XRCore.Vision;

namespace XRCore.UI.Editor
{
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
                GetOrAdd<CanvasScaler>(canvasGo);
                GetOrAdd<GraphicRaycaster>(canvasGo);
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

                Selection.activeGameObject = root;
                EditorSceneManager.MarkSceneDirty(root.scene);
            }
            finally
            {
                Undo.CollapseUndoOperations(group);
            }

            return root;
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
    }
}
#endif
