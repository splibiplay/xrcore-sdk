using System;
using System.Collections.Generic;

namespace XRCore.Agents
{
    /// <summary>
    /// Evita repetir el mismo mensaje del agente dentro de una ventana temporal.
    /// </summary>
    public sealed class AgentMessageDeduplicator
    {
        private readonly struct MessageKey : IEquatable<MessageKey>
        {
            public readonly string Trigger;
            public readonly string Message;

            public MessageKey(string trigger, string message)
            {
                Trigger = trigger ?? string.Empty;
                Message = message ?? string.Empty;
            }

            public bool Equals(MessageKey other)
            {
                return string.Equals(Trigger, other.Trigger, StringComparison.Ordinal) &&
                       string.Equals(Message, other.Message, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is MessageKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Trigger != null ? Trigger.GetHashCode() : 0) * 397) ^
                           (Message != null ? Message.GetHashCode() : 0);
                }
            }
        }

        private readonly Dictionary<MessageKey, float> _lastSentAtByMessage = new();
        private float _nextCleanupAt;

        public bool ShouldEmit(string trigger, string message, float now, float cooldownSeconds)
        {
            if (cooldownSeconds <= 0f || string.IsNullOrWhiteSpace(message))
            {
                return true;
            }

            var key = new MessageKey(trigger, message);
            if (_lastSentAtByMessage.TryGetValue(key, out var lastSentAt))
            {
                if (now - lastSentAt < cooldownSeconds)
                {
                    return false;
                }
            }

            _lastSentAtByMessage[key] = now;
            CleanupIfNeeded(now, cooldownSeconds);
            return true;
        }

        public void Reset()
        {
            _lastSentAtByMessage.Clear();
            _nextCleanupAt = 0f;
        }

        private void CleanupIfNeeded(float now, float cooldownSeconds)
        {
            if (now < _nextCleanupAt)
            {
                return;
            }

            _nextCleanupAt = now + Math.Max(1f, cooldownSeconds);
            float maxAge = Math.Max(2f, cooldownSeconds * 2f);

            List<MessageKey> stale = null;
            foreach (var pair in _lastSentAtByMessage)
            {
                if (now - pair.Value <= maxAge)
                {
                    continue;
                }

                stale ??= new List<MessageKey>(8);
                stale.Add(pair.Key);
            }

            if (stale == null)
            {
                return;
            }

            for (int i = 0; i < stale.Count; i++)
            {
                _lastSentAtByMessage.Remove(stale[i]);
            }
        }
    }
}
