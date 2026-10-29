namespace Fida.Agent;

/// <summary>按评分损失过滤，再用稳定的 softmax 采样。评分器应使用 [-1,1] 尺度。</summary>
public sealed class ActionSelector
{
    private readonly Random _random;
    public AgentProfile Profile { get; }

    public ActionSelector(AgentProfile profile)
    {
        profile.Validate();
        Profile = profile;
        _random = profile.Seed is int seed ? new Random(seed) : new Random();
    }

    public int Select(IReadOnlyList<double> scores)
    {
        if (scores.Count == 0) throw new ArgumentException("没有合法动作。", nameof(scores));
        if (scores.Any(s => !double.IsFinite(s))) throw new ArgumentException("动作评分必须有限。", nameof(scores));
        if (Profile.Strength == 0) return _random.Next(scores.Count);
        var best = scores.Max();
        if (Profile.Strength == 100) return Enumerable.Range(0, scores.Count).First(i => scores[i] == best);
        var weights = scores.Select(s => best - s <= Profile.MaxActionLoss + 1e-9
            ? Math.Exp((s - best) / Profile.Temperature) : 0).ToArray();
        var sample = _random.NextDouble() * weights.Sum();
        for (int i = 0; i < weights.Length; i++)
        {
            sample -= weights[i];
            if (sample < 0) return i;
        }
        return Array.FindLastIndex(weights, w => w > 0);
    }
}
