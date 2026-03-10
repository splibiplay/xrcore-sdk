using UnityEngine;
using XRCore.Agents;
using XRCore.Core;

namespace XRCore.UI
{
    /// <summary>
    /// Bootstrap simple para aplicar settings del framework y cablear componentes base.
    /// </summary>
    public sealed class XRCoreInstaller : MonoBehaviour
    {
        [SerializeField] private XRCoreSettings settings;
        [SerializeField] private XRGuideAgent guideAgent;
        [SerializeField] private XRGuideInstructionPresenter instructionPresenter;
        [SerializeField] private XRCoreDiagnosticsOverlay diagnosticsOverlay;
        [SerializeField] private bool autoFindComponents = true;
        [SerializeField] private bool clearEventBusOnInstall = false;

        private void Awake()
        {
            if (ShouldInstallOnAwake())
            {
                Install();
            }
        }

        public void Install()
        {
            if (clearEventBusOnInstall)
            {
                XRCoreEventBus.ClearAllSubscribers();
            }

            if (autoFindComponents)
            {
                if (guideAgent == null)
                {
                    guideAgent = Object.FindFirstObjectByType<XRGuideAgent>();
                }

                if (instructionPresenter == null)
                {
                    instructionPresenter = Object.FindFirstObjectByType<XRGuideInstructionPresenter>();
                }

                if (diagnosticsOverlay == null)
                {
                    diagnosticsOverlay = Object.FindFirstObjectByType<XRCoreDiagnosticsOverlay>();
                }
            }

            ApplyDebugSettings();
            guideAgent?.ApplySettings(settings);
            instructionPresenter?.ApplySettings(settings);
            diagnosticsOverlay?.ApplySettings(settings);
        }

        private void ApplyDebugSettings()
        {
            if (settings == null)
            {
                return;
            }

            XRCoreDebug.EnableLogs = settings.EnableLogs;
            XRCoreDebug.EnableWarnings = settings.EnableWarnings;
            XRCoreDebug.EnableErrors = settings.EnableErrors;
        }

        private bool ShouldInstallOnAwake()
        {
            if (settings == null)
            {
                return true;
            }

            return settings.BootstrapMode == XRCoreBootstrapMode.AutoOnSceneLoad;
        }
    }
}
