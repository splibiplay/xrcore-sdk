using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DomusViRoutineUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RoutineEngine routineEngine;

    [Header("UI - Texts")]
    [SerializeField] private TMP_Text txtTitulo;
    [SerializeField] private TMP_Text txtSubtitulo;

    [Tooltip("Opcional: lista de objetos presentes estabilizados")]
    [SerializeField] private TMP_Text txtPresentObjects;

    [Tooltip("Opcional: debug de candidato actual")]
    [SerializeField] private TMP_Text txtCandidateDebug;

    [Header("UI - Poke Buttons (roots)")]
    [Tooltip("GameObject del botón Poke 'Iniciar' (para ocultar/mostrar o cambiar estilo)")]
    [SerializeField] private GameObject btnIniciarRoot;

    [Tooltip("GameObject del botón Poke 'Cambiar rutina' (para ocultar/mostrar o cambiar estilo)")]
    [SerializeField] private GameObject btnCambiarRutinaRoot;

    [Header("Behaviour")]
    [Tooltip("Si true, al pulsar Iniciar se 'bloquea' la propuesta para que no cambie con nuevas detecciones.")]
    [SerializeField] private bool lockAfterStart = true;

    [Tooltip("Si true, el botón Iniciar se oculta cuando no hay candidato. Si false, se deja visible pero no hace nada.")]
    [SerializeField] private bool hideStartWhenNoCandidate = false;

    [Tooltip("Si true, el botón Cambiar se oculta cuando no aplica. Si false, se deja visible pero no hace nada.")]
    [SerializeField] private bool hideChangeWhenNotAvailable = true;

    /// <summary> Se dispara cuando el usuario confirma la rutina con Btn_Iniciar. </summary>
    public event System.Action<RoutineCandidate> OnRoutineConfirmed;

    // Estado interno
    private List<RoutineCandidate> _candidates = new();
    private int _index = 0;
    private HashSet<string> _lastPresent = new();

    private bool _locked = false;
    private RoutineCandidate _lockedCandidate;

    private void OnEnable()
    {
        if (routineEngine != null)
            routineEngine.OnCandidateChanged += OnCandidateChanged;

        RenderNoRoutine();
        ApplyButtonsState(hasCandidate: false, canChange: false);
    }

    private void OnDisable()
    {
        if (routineEngine != null)
            routineEngine.OnCandidateChanged -= OnCandidateChanged;
    }

    /// <summary>
    /// Evento del motor: han cambiado los objetos presentes (estabilizados) o el candidato.
    /// </summary>
    private void OnCandidateChanged(RoutineCandidate _, HashSet<string> present)
    {
        // Si hemos bloqueado la selección tras "Iniciar", ignoramos cambios de detección.
        if (_locked) return;

        _lastPresent = present != null
            ? new HashSet<string>(present)
            : new HashSet<string>();

        // Recalculamos candidatas ordenadas por score (usa tu RoutineCatalog.EvaluateAll)
        _candidates = RoutineCatalog.EvaluateAll(present);
        _index = 0;

        if (txtPresentObjects != null)
            txtPresentObjects.text = $"Objetos: {string.Join(", ", present)}";

        // Para la rutina de Poner la mesa, el plato es imprescindible.
        // Si no hay plato en los objetos presentes, eliminamos PonerMesa de las candidatas.
        bool hasPlate = HasPlateInPresent();
        if (!hasPlate && _candidates != null && _candidates.Count > 0)
        {
            _candidates.RemoveAll(c => c.id == RoutineId.PonerMesa);
        }

        if (_candidates == null || _candidates.Count == 0)
        {
            // Sin candidatos válidos, seguimos en el estado genérico de "Detectando objetos…".
            RenderNoRoutine();
            ApplyButtonsState(hasCandidate: false, canChange: false);
            return;
        }

        RenderCandidate(_candidates[_index]);

        bool canChange = _candidates.Count > 1;
        ApplyButtonsState(hasCandidate: true, canChange: canChange);
    }

    // =========================================================
    //  Poke events (asignar desde When Select())
    // =========================================================

    /// <summary>
    /// Llamar desde When Select() del botón Poke "Iniciar".
    /// </summary>
    public void OnStartPressed()
    {
        // Si ya está bloqueado, ignoramos pulsaciones repetidas (o podrías tratarlo como "confirmar otra vez")
        if (_locked) return;

        if (_candidates == null || _candidates.Count == 0)
        {
            // No hay candidato todavía
            return;
        }

        var chosen = _candidates[_index];
        var sessionLabels = BuildSessionScopeLabels(chosen);
        ApplySessionObjectScope(sessionLabels);

        Debug.Log($"[DomusViRoutineUI] Rutina CONFIRMADA: {chosen.id} ({chosen.version}) score={chosen.score}");

        if (lockAfterStart)
        {
            _locked = true;
            _lockedCandidate = chosen;

            // Una vez confirmada, ya no tiene sentido "Cambiar"
            ApplyButtonsState(hasCandidate: true, canChange: false);

            // Render por si quieres marcar estado "confirmado"
            RenderLockedCandidate(chosen);
        }

        Debug.Log("[DomusViRoutineUI] OnRoutineConfirmed INVOKE");
        OnRoutineConfirmed?.Invoke(chosen);

        var stepFlow = FindObjectOfType<DomusViStepFlow>(true);
        if (chosen.id == RoutineId.HigieneBucal)
        {
            Debug.Log("[DomusViRoutineUI] OnStartPressed -> calling StepFlow.StartHigieneBucal_MVP()");
            stepFlow?.StartHigieneBucal_MVP(sessionLabels);
        }
        else if (chosen.id == RoutineId.PonerMesa)
        {
            Debug.Log("[DomusViRoutineUI] OnStartPressed -> calling StepFlow.StartPonerMesa_MVP()");
            stepFlow?.StartPonerMesa_MVP(chosen.version, sessionLabels);
        }

        // TODO (Fase 2): notificar a FlowController / registro.
        // TODO (Fase 3): arrancar step-by-step.
    }

    /// <summary>
    /// Llamar desde When Select() del botón Poke "Cambiar rutina".
    /// </summary>
    public void OnChangePressed()
    {
        if (_locked) return;

        if (_candidates == null || _candidates.Count <= 1)
        {
            // 0 o 1 candidatos: no hay nada que cambiar
            ApplyButtonsState(hasCandidate: _candidates != null && _candidates.Count == 1, canChange: false);
            return;
        }

        int next = _index + 1;

        if (next >= _candidates.Count)
        {
            // No hay más: nos quedamos en el actual, y desactivamos cambiar
            ApplyButtonsState(hasCandidate: true, canChange: false);
            return;
        }

        _index = next;
        RenderCandidate(_candidates[_index]);

        bool canChange = _index < _candidates.Count - 1;
        ApplyButtonsState(hasCandidate: true, canChange: canChange);
    }

    /// <summary>
    /// (Opcional) para pruebas: desbloquear manualmente desde otro botón o debug.
    /// </summary>
    public void Unlock()
    {
        _locked = false;
        _lockedCandidate = null;
        if (routineEngine != null && routineEngine.Provider is SentisDetectionProvider sentisProvider)
        {
            sentisProvider.ClearSessionObjectScope();
        }
        RenderNoRoutine();
        ApplyButtonsState(hasCandidate: false, canChange: false);
    }

    // =========================================================
    //  UI rendering
    // =========================================================

    private void RenderNoRoutine()
    {
        if (txtTitulo != null) txtTitulo.text = "Detectando objetos…";
        if (txtSubtitulo != null) txtSubtitulo.text = "Coloca los objetos mínimos para comenzar.";

        if (txtCandidateDebug != null)
            txtCandidateDebug.text = "Candidate: none";
    }

    private void RenderNoRoutineMesaSinPlato()
    {
        if (txtTitulo != null) txtTitulo.text = "Coloca un plato delante de ti";
        if (txtSubtitulo != null) txtSubtitulo.text = "Para poner la mesa, necesitamos detectar al menos un plato.";

        if (txtCandidateDebug != null)
            txtCandidateDebug.text = "Candidate: none (sin plato)";
    }

    private void RenderCandidate(RoutineCandidate c)
    {
        if (txtTitulo != null)
        {
            txtTitulo.text = c.id == RoutineId.HigieneBucal
                ? "Rutina propuesta: Higiene bucal"
                : "Rutina propuesta: Poner la mesa";
        }

        if (txtSubtitulo != null)
        {
            txtSubtitulo.text = c.version == RoutineVersion.Completa
                ? "Versión completa"
                : "Versión básica";
        }

        if (txtCandidateDebug != null)
        {
            txtCandidateDebug.text = $"Candidate: {c.id} | {c.version} | score {c.score}";
        }
    }

    private void RenderLockedCandidate(RoutineCandidate c)
    {
        // Misma idea, pero marcando visualmente que ya está confirmada
        if (txtTitulo != null)
        {
            txtTitulo.text = c.id == RoutineId.HigieneBucal
                ? "Rutina confirmada: Higiene bucal"
                : "Rutina confirmada: Poner la mesa";
        }

        if (txtSubtitulo != null)
        {
            txtSubtitulo.text = "Confirmada. Pulsa para continuar.";
        }

        if (txtCandidateDebug != null)
        {
            txtCandidateDebug.text = $"LOCKED: {c.id} | {c.version} | score {c.score}";
        }
    }

    private void ApplyButtonsState(bool hasCandidate, bool canChange)
    {
        // Iniciar
        if (btnIniciarRoot != null)
        {
            if (hideStartWhenNoCandidate)
                btnIniciarRoot.SetActive(hasCandidate || !hideStartWhenNoCandidate);
            else
                btnIniciarRoot.SetActive(true); // siempre visible si quieres
        }

        // Cambiar
        if (btnCambiarRutinaRoot != null)
        {
            if (hideChangeWhenNotAvailable)
                btnCambiarRutinaRoot.SetActive(canChange);
            else
                btnCambiarRutinaRoot.SetActive(true);
        }

        // Nota: si quisieras "deshabilitar" sin ocultar, puedes cambiar material/color/texto aquí.
        // Como son Poke, no hay interactable.enabled genérico sin tocar el componente. Si quieres, lo hacemos.
    }

    // =========================================================
    //  Helpers
    // =========================================================

    /// <summary>
    /// Devuelve el candidato actualmente seleccionado (o el bloqueado si está locked).
    /// Útil para FlowController / registro.
    /// </summary>
    public RoutineCandidate GetCurrentChosenCandidate()
    {
        if (_locked) return _lockedCandidate;

        if (_candidates != null && _candidates.Count > 0)
            return _candidates[_index];

        return new RoutineCandidate { id = RoutineId.None, score = 0 };
    }

    public bool IsLocked => _locked;

    private void ApplySessionObjectScope(HashSet<string> labels)
    {
        if (routineEngine == null || routineEngine.Provider is not SentisDetectionProvider sentisProvider)
        {
            return;
        }

        sentisProvider.LockSessionObjects(labels);
    }

    private HashSet<string> BuildSessionScopeLabels(RoutineCandidate chosen)
    {
        var routineRelevant = new HashSet<string>();
        switch (chosen.id)
        {
            case RoutineId.HigieneBucal:
                routineRelevant.Add("cepillo");
                routineRelevant.Add("pasta");
                routineRelevant.Add("vaso");
                break;
            case RoutineId.PonerMesa:
                routineRelevant.Add("tenedor");
                routineRelevant.Add("vaso");
                routineRelevant.Add("servilleta");
                routineRelevant.Add("plato");
                routineRelevant.Add("cuchillo");
                routineRelevant.Add("cuchara");
                break;
        }

        // Solo objetos detectados al inicio y relevantes para la rutina.
        var labels = new HashSet<string>();
        foreach (var label in _lastPresent)
        {
            string normalized = RoutineCatalog.NormalizeLabel(label);
            if (routineRelevant.Contains(normalized))
            {
                labels.Add(normalized);
            }
        }

        Debug.Log($"[DomusViRoutineUI] Session labels = [{string.Join(", ", labels)}]");
        return labels;
    }

    private bool HasPlateInPresent()
    {
        if (_lastPresent == null || _lastPresent.Count == 0)
        {
            return false;
        }

        foreach (var raw in _lastPresent)
        {
            string normalized = RoutineCatalog.NormalizeLabel(raw);
            if (normalized == "plato")
            {
                return true;
            }
        }

        return false;
    }
}
