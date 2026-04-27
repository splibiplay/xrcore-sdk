#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace XRCore.UI.Editor
{
    public sealed class XRCoreSetupWizardWindow : EditorWindow
    {
        private Transform _parent;
        private XRCoreSetupOptions _options;

        [MenuItem("Tools/XRCore/Setup Wizard")]
        private static void OpenFromTools()
        {
            OpenWindow(null);
        }

        [MenuItem("GameObject/XRCore/Open Setup Wizard", false, 11)]
        private static void OpenFromGameObjectMenu(MenuCommand command)
        {
            Transform parent = (command.context as GameObject)?.transform;
            OpenWindow(parent);
        }

        private static void OpenWindow(Transform parent)
        {
            var window = GetWindow<XRCoreSetupWizardWindow>("XRCore Setup Wizard");
            window.minSize = new Vector2(420f, 220f);
            window._parent = parent;
            window._options ??= new XRCoreSetupOptions();
            window.Show();
        }

        private void OnEnable()
        {
            _options ??= new XRCoreSetupOptions();
        }

        private void OnGUI()
        {
            GUILayout.Space(8f);
            EditorGUILayout.LabelField("XRCore SDK Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Crea la base de XRCore en escena y aplica presets de Provider y Reasoner.",
                MessageType.Info);

            _parent = (Transform)EditorGUILayout.ObjectField("Parent (opcional)", _parent, typeof(Transform), true);
            _options.ReasonerPreset = (XRCoreReasonerPreset)EditorGUILayout.EnumPopup("Agent Reasoner", _options.ReasonerPreset);
            _options.DetectionProviderPreset = (XRCoreDetectionProviderPreset)EditorGUILayout.EnumPopup("Vision Provider", _options.DetectionProviderPreset);

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Setup XR Assistant", GUILayout.Width(190f), GUILayout.Height(28f)))
                {
                    GameObject root = XRCoreSceneSetupMenu.SetupXRCoreAssistant(_parent, _options);
                    if (root != null)
                    {
                        Selection.activeGameObject = root;
                    }
                }
            }

            GUILayout.Space(8f);
        }
    }
}
#endif
