using System.Collections.Generic;

public enum RoutineId
{
    None,
    HigieneBucal,
    PonerMesa
}

public enum RoutineVersion
{
    Basica,
    Completa
}

public class RoutineCandidate
{
    public RoutineId id;
    public RoutineVersion version;
    public int score;
    public List<string> reasons; // para debug / profesional
}

public static class RoutineCatalog
{
    // Labels “base”
    public const string CEPILLO = "cepillo";
    public const string PASTA = "pasta";

    /// <summary> Normaliza un label raw (ej. "cepillo de dientes" → "cepillo"). </summary>
    public static string NormalizeLabel(string rawLabel)
    {
        if (string.IsNullOrEmpty(rawLabel)) return rawLabel ?? "";
        rawLabel = rawLabel.Trim().ToLowerInvariant();
        if (rawLabel.StartsWith("cepillo")) return CEPILLO;
        if (rawLabel.StartsWith("pasta")) return PASTA;
        return rawLabel;
    }

    public static HashSet<string> Normalize(HashSet<string> rawLabels)
    {
        var norm = new HashSet<string>();

        foreach (var l in rawLabels)
        {
            norm.Add(NormalizeLabel(l));
        }
        return norm;
    }

    public static RoutineCandidate Evaluate(HashSet<string> normalizedPresent)
    {
        var higiene = EvalHigiene(normalizedPresent);
        var mesa = EvalMesa(normalizedPresent);

        // Decide mejor candidata
        if (higiene == null && mesa == null) return new RoutineCandidate { id = RoutineId.None, score = 0 };

        if (higiene != null && mesa == null) return higiene;
        if (mesa != null && higiene == null) return mesa;

        // empate o ambas válidas: devolvemos la de mayor score; si empate, marcamos “None” para pedir confirmación en paso 2
        if (higiene.score > mesa.score) return higiene;
        if (mesa.score > higiene.score) return mesa;

        // empate real
        return new RoutineCandidate
        {
            id = RoutineId.None,
            score = higiene.score,
            reasons = new List<string> { "Empate: higiene_bucal y poner_mesa válidas" }
        };
    }

    private static RoutineCandidate EvalHigiene(HashSet<string> p)
    {
        bool hasCepillo = p.Contains(CEPILLO);
        bool hasVaso = p.Contains("vaso");
        bool hasPasta = p.Contains(PASTA);

        if (!hasCepillo || !hasVaso) return null; // mínimos

        int score = 0;
        var reasons = new List<string>();

        score += 2; reasons.Add("Cepillo detectado");
        score += 2; reasons.Add("Vaso detectado");
        if (hasPasta) { score += 1; reasons.Add("Pasta detectada"); }

        return new RoutineCandidate
        {
            id = RoutineId.HigieneBucal,
            version = hasPasta ? RoutineVersion.Completa : RoutineVersion.Basica,
            score = score,
            reasons = reasons
        };
    }

    private static RoutineCandidate EvalMesa(HashSet<string> p)
    {
        bool hasTenedor = p.Contains("tenedor");
        bool hasVaso = p.Contains("vaso");
        bool hasServ = p.Contains("servilleta");

        if (!hasTenedor || !hasVaso || !hasServ) return null; // mínimos

        int score = 0;
        var reasons = new List<string>();

        score += 2; reasons.Add("Tenedor detectado");
        score += 2; reasons.Add("Vaso detectado");
        score += 2; reasons.Add("Servilleta detectada");

        if (p.Contains("plato")) { score += 1; reasons.Add("Plato detectado"); }
        if (p.Contains("cuchillo")) { score += 1; reasons.Add("Cuchillo detectado"); }
        if (p.Contains("cuchara")) { score += 1; reasons.Add("Cuchara detectada"); }

        return new RoutineCandidate
        {
            id = RoutineId.PonerMesa,
            version = (p.Contains("plato") || p.Contains("cuchillo") || p.Contains("cuchara"))
                ? RoutineVersion.Completa
                : RoutineVersion.Basica,
            score = score,
            reasons = reasons
        };
    }
    public static List<RoutineCandidate> EvaluateAll(HashSet<string> normalizedPresent)
    {
    var list = new List<RoutineCandidate>(2);

    var higiene = EvalHigiene(normalizedPresent);
    if (higiene != null) list.Add(higiene);

    var mesa = EvalMesa(normalizedPresent);
    if (mesa != null) list.Add(mesa);

    // Orden: mayor score primero
    list.Sort((a, b) => b.score.CompareTo(a.score));

    return list;
    }

}
