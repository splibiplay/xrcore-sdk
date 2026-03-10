using UnityEditor;
using UnityEngine;

namespace XRCore.Agents.Editor
{
    public sealed class XRCoreAgentDebuggerWindow : EditorWindow
    {
        private XRGuideAgent _agent;
        private Vector2 _scroll;

        [MenuItem("XRCore/Agent Debugger")]
        private static void Open()
        {
            GetWindow<XRCoreAgentDebuggerWindow>("XRCore Agent Debugger");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("XRCore Agent Debugger", EditorStyles.boldLabel);
            _agent = (XRGuideAgent)EditorGUILayout.ObjectField("Guide Agent", _agent, typeof(XRGuideAgent), true);

            if (_agent == null)
            {
                if (GUILayout.Button("Find First Agent In Scene"))
                {
                    _agent = Object.FindFirstObjectByType<XRGuideAgent>();
                }
                return;
            }

            DrawRuntimeState();
            EditorGUILayout.Space(8);
            DrawBehaviours();
        }

        private void DrawRuntimeState()
        {
            EditorGUILayout.LabelField("Runtime State", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Play Mode", EditorApplication.isPlaying ? "Yes" : "No");
            EditorGUILayout.LabelField("Last Trigger", string.IsNullOrWhiteSpace(_agent.LastDecisionTrigger) ? "-" : _agent.LastDecisionTrigger);
            EditorGUILayout.LabelField("Last Selected Behaviour", string.IsNullOrWhiteSpace(_agent.LastSelectedBehaviourName) ? "-" : _agent.LastSelectedBehaviourName);
            EditorGUILayout.LabelField("Evaluated Behaviours", _agent.LastEvaluatedBehaviourCount.ToString());

            if (_agent.LastDecisionTimestamp > 0f)
            {
                EditorGUILayout.LabelField("Last Decision Time", _agent.LastDecisionTimestamp.ToString("0.000"));
            }
        }

        private void DrawBehaviours()
        {
            SerializedObject so = new(_agent);
            SerializedProperty behavioursProp = so.FindProperty("behaviours");
            if (behavioursProp == null)
            {
                EditorGUILayout.HelpBox("Could not read behaviours list.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Behaviours", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < behavioursProp.arraySize; i++)
            {
                SerializedProperty item = behavioursProp.GetArrayElementAtIndex(i);
                var behaviour = item.objectReferenceValue as XRGuideAgentBehaviour;
                if (behaviour == null)
                {
                    continue;
                }

                float lastExec = _agent.GetBehaviourLastExecutionTime(behaviour);
                float cooldownRemaining = 0f;
                if (EditorApplication.isPlaying && lastExec >= 0f)
                {
                    cooldownRemaining = Mathf.Max(0f, behaviour.CooldownSeconds - (Time.realtimeSinceStartup - lastExec));
                }

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Name", behaviour.name);
                EditorGUILayout.LabelField("Priority", behaviour.Priority.ToString());
                EditorGUILayout.LabelField("Cooldown (s)", behaviour.CooldownSeconds.ToString("0.00"));
                EditorGUILayout.LabelField("Cooldown Remaining (s)", cooldownRemaining.ToString("0.00"));
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }
    }
}
