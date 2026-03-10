using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace XRCore.Vision
{
    /// <summary>
    /// Adapter agnostico para obtener detecciones desde un manager Sentis por reflexion.
    /// Evita dependencia de compilacion con un tipo concreto.
    /// </summary>
    public sealed class SentisDetectionProvider : MonoBehaviour, IXRDetectionProvider
    {
        [Header("Source")]
        [SerializeField] private MonoBehaviour sentisUiManager;
        [SerializeField] private string managerTypeName = "SentisInferenceUiManager";
        [SerializeField] private string boxesMemberName = "m_boxDrawn";

        [Header("Output")]
        [SerializeField] private string providerId = "sentis";
        [SerializeField] private bool normalizeLabelToLower = true;
        [SerializeField] private bool trimLabel = true;

        [Header("Debug")]
        [SerializeField] private bool logConfigurationWarnings = true;

        private readonly List<DetectionResult> _buffer = new(64);
        private MemberInfo _boxesMember;

        private void Awake()
        {
            ResolveManagerAndMembers();
        }

        private void OnValidate()
        {
            ResolveManagerAndMembers();
        }

        public IEnumerable<DetectionResult> GetDetections()
        {
            _buffer.Clear();
            if (sentisUiManager == null || _boxesMember == null)
            {
                return _buffer;
            }

            object boxesObject = GetMemberValue(_boxesMember, sentisUiManager);
            if (boxesObject is not IEnumerable boxesEnumerable)
            {
                return _buffer;
            }

            foreach (var item in boxesEnumerable)
            {
                if (item == null)
                {
                    continue;
                }

                if (!TryReadDetection(item, out var result))
                {
                    continue;
                }

                _buffer.Add(result);
            }

            return _buffer;
        }

        private bool TryReadDetection(object boxItem, out DetectionResult result)
        {
            result = default;
            Type boxType = boxItem.GetType();

            if (!TryGetBoxRectTransform(boxType, boxItem, out var rectTransform))
            {
                return false;
            }

            string label = ReadLabel(boxType, boxItem);
            float timestamp = ReadTimestamp(boxType, boxItem);

            if (string.IsNullOrWhiteSpace(label))
            {
                return false;
            }

            var size = rectTransform.sizeDelta;
            var pos = rectTransform.position;
            var bbox = new Rect(
                pos.x - size.x * 0.5f,
                pos.z - size.y * 0.5f,
                size.x,
                size.y
            );

            var detection = new Detection(label, 1f, bbox, timestamp);
            result = new DetectionResult(providerId, detection);
            return true;
        }

        private bool TryGetBoxRectTransform(Type boxType, object boxItem, out RectTransform rectTransform)
        {
            rectTransform = null;
            var member = FindMember(boxType, "BoxRectTransform");
            if (member == null)
            {
                return false;
            }

            rectTransform = GetMemberValue(member, boxItem) as RectTransform;
            return rectTransform != null;
        }

        private string ReadLabel(Type boxType, object boxItem)
        {
            var member = FindMember(boxType, "ClassName");
            if (member == null)
            {
                return string.Empty;
            }

            string label = GetMemberValue(member, boxItem)?.ToString() ?? string.Empty;
            if (trimLabel)
            {
                label = label.Trim();
            }

            if (normalizeLabelToLower)
            {
                label = label.ToLowerInvariant();
            }

            return label;
        }

        private float ReadTimestamp(Type boxType, object boxItem)
        {
            var member = FindMember(boxType, "lastUpdateTime");
            if (member == null)
            {
                return Time.time;
            }

            object raw = GetMemberValue(member, boxItem);
            return raw is float f ? f : Time.time;
        }

        private void ResolveManagerAndMembers()
        {
            if (sentisUiManager == null)
            {
                sentisUiManager = FindManagerByTypeName(managerTypeName);
            }

            _boxesMember = null;
            if (sentisUiManager == null)
            {
                if (logConfigurationWarnings)
                {
                    Debug.LogWarning("[XRCore.Vision.SentisDetectionProvider] Manager not assigned.");
                }
                return;
            }

            _boxesMember = FindMember(sentisUiManager.GetType(), boxesMemberName);
            if (_boxesMember == null && logConfigurationWarnings)
            {
                Debug.LogWarning(
                    $"[XRCore.Vision.SentisDetectionProvider] Member '{boxesMemberName}' not found in '{sentisUiManager.GetType().Name}'.");
            }
        }

        private static MonoBehaviour FindManagerByTypeName(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            var type = FindTypeByName(typeName);
            if (type == null || !typeof(MonoBehaviour).IsAssignableFrom(type))
            {
                return null;
            }

            return UnityEngine.Object.FindFirstObjectByType(type) as MonoBehaviour;
        }

        private static Type FindTypeByName(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var type = assemblies[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }

                var allTypes = assemblies[i].GetTypes();
                for (int j = 0; j < allTypes.Length; j++)
                {
                    if (allTypes[j].Name == typeName)
                    {
                        return allTypes[j];
                    }
                }
            }

            return null;
        }

        private static MemberInfo FindMember(Type type, string memberName)
        {
            if (type == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var property = type.GetProperty(memberName, flags);
            if (property != null)
            {
                return property;
            }

            var field = type.GetField(memberName, flags);
            if (field != null)
            {
                return field;
            }

            return null;
        }

        private static object GetMemberValue(MemberInfo member, object target)
        {
            if (member is PropertyInfo property)
            {
                return property.GetValue(target);
            }

            if (member is FieldInfo field)
            {
                return field.GetValue(target);
            }

            return null;
        }
    }
}
