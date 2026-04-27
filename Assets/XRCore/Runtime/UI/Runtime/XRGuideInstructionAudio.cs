using System;
using System.Collections.Generic;
using UnityEngine;
using XRCore.Agents;

namespace XRCore.UI
{
    /// <summary>
    /// Renderer de audio para instrucciones del agente.
    /// </summary>
    public sealed class XRGuideInstructionAudio : MonoBehaviour
    {
        [Serializable]
        private struct TriggerAudioEntry
        {
            public string trigger;
            public AudioClip clip;
        }

        [SerializeField] private AudioSource audioSource;
        [SerializeField] private List<TriggerAudioEntry> triggerAudio = new();
        [SerializeField] private AudioClip fallbackClip;

        public void Present(AgentInstructionEvent instruction)
        {
            if (audioSource == null)
            {
                return;
            }

            AudioClip clip = ResolveClip(instruction.Trigger);
            if (clip == null)
            {
                return;
            }

            audioSource.PlayOneShot(clip);
        }

        private AudioClip ResolveClip(string trigger)
        {
            for (int i = 0; i < triggerAudio.Count; i++)
            {
                if (triggerAudio[i].clip == null || string.IsNullOrWhiteSpace(triggerAudio[i].trigger))
                {
                    continue;
                }

                if (string.Equals(triggerAudio[i].trigger, trigger, StringComparison.Ordinal))
                {
                    return triggerAudio[i].clip;
                }
            }

            return fallbackClip;
        }
    }
}
