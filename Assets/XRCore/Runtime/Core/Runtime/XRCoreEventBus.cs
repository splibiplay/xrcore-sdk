using System;
using System.Collections.Generic;
using UnityEngine;

namespace XRCore.Core
{
    /// <summary>
    /// Event bus global para desacoplar modulos del framework.
    /// Disenado para evitar allocations durante Publish.
    /// </summary>
    public static class XRCoreEventBus
    {
        private interface IEventChannel
        {
            void Clear();
        }

        private sealed class EventChannel<TEvent> : IEventChannel
        {
            private readonly List<Action<TEvent>> _handlers = new(8);

            public void Add(Action<TEvent> handler)
            {
                if (handler == null)
                {
                    return;
                }

                if (!_handlers.Contains(handler))
                {
                    _handlers.Add(handler);
                }
            }

            public void Remove(Action<TEvent> handler)
            {
                if (handler == null)
                {
                    return;
                }

                _handlers.Remove(handler);
            }

            public void Publish(TEvent evt)
            {
                for (int i = 0; i < _handlers.Count; i++)
                {
                    _handlers[i]?.Invoke(evt);
                }
            }

            public bool IsEmpty => _handlers.Count == 0;

            public void Clear()
            {
                _handlers.Clear();
            }
        }

        private static readonly Dictionary<Type, IEventChannel> Channels = new();

        public static void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var channel = GetOrCreateChannel<TEvent>();
            channel.Add(handler);
        }

        public static void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            if (!Channels.TryGetValue(eventType, out var rawChannel))
            {
                return;
            }

            if (rawChannel is EventChannel<TEvent> channel)
            {
                channel.Remove(handler);
                if (channel.IsEmpty)
                {
                    Channels.Remove(eventType);
                }
            }
        }

        public static void Publish<TEvent>(TEvent evt)
        {
            if (!Channels.TryGetValue(typeof(TEvent), out var rawChannel))
            {
                return;
            }

            if (rawChannel is EventChannel<TEvent> channel)
            {
                channel.Publish(evt);
            }
        }

        public static void ClearAllSubscribers()
        {
            foreach (var channel in Channels.Values)
            {
                channel.Clear();
            }

            Channels.Clear();
        }

        private static EventChannel<TEvent> GetOrCreateChannel<TEvent>()
        {
            var eventType = typeof(TEvent);
            if (Channels.TryGetValue(eventType, out var rawChannel) && rawChannel is EventChannel<TEvent> existing)
            {
                return existing;
            }

            var created = new EventChannel<TEvent>();
            Channels[eventType] = created;
            return created;
        }
    }

    [Serializable]
    public readonly struct XRSignalEvent
    {
        public readonly string Signal;
        public readonly GameObject Source;

        public XRSignalEvent(string signal, GameObject source)
        {
            Signal = signal;
            Source = source;
        }
    }
}
