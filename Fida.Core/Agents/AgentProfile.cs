using System.Text.Json;
using Fida.Logic;

namespace Fida.Agent;

/// <summary>实力与评分算法分离；0 为随机合法动作，100 为贪心最优。</summary>
public sealed record AgentProfile
{
    public int Strength { get; init; } = 65;
    public int? Seed { get; init; }
    public int SimulationNodeBudget { get; init; } = 256;
    public int SimulationDepth { get; init; } = 8;
    public double Temperature => Strength >= 100 ? 0 : 2 * Math.Pow(1 - Strength / 100.0, 2);
    public double MaxActionLoss => Strength >= 100 ? 0 : 2 * (1 - Math.Pow(Strength / 100.0, 8));

    public void Validate()
    {
        if (Strength is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(Strength));
        if (SimulationNodeBudget is < 1 or > 10000) throw new ArgumentOutOfRangeException(nameof(SimulationNodeBudget));
        if (SimulationDepth is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(SimulationDepth));
    }

    public static AgentProfile Load(string path)
    {
        var text = ContentSource.ReadText(path);
        if (text == null) return new();
        var profile = JsonSerializer.Deserialize<AgentProfile>(text,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        profile.Validate();
        return profile;
    }
}

public static class AgentDefaults
{
    public static AgentProfile Profile { get; set; } = new();
    public static Lazy<CompactActionTable> Table { get; } = new(() =>
    {
        using var stream = typeof(AgentDefaults).Assembly.GetManifestResourceStream("Fida.Agent.base.q.gz");
        return stream == null ? throw new InvalidDataException("缺少 Agent value table，请生成 Content/agents/base.q.gz 后重新构建。") : CompactActionTable.Load(stream);
    });
}
