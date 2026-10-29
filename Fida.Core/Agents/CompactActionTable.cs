using System.IO.Compression;
using Fida.Logic;

namespace Fida.Agent;

/// <summary>数字排序减少状态数，分值字典与位打包继续无损压缩量化后的评分。</summary>
public sealed class CompactActionTable
{
    public const int RulesVersion = 1;
    public const int PairCount = 55;
    private readonly byte[] _codes;
    private readonly short[] _codebook;
    private readonly int _bits;
    public int MaxHealth { get; }
    public int MemoryBytes => _codes.Length + _codebook.Length * 2;
    private CompactActionTable(int maxHealth, byte[] codes, short[] codebook, int bits)
    { MaxHealth = maxHealth; _codes = codes; _codebook = codebook; _bits = bits; }

    public static int PairIndex(int a, int b)
    {
        if (a is < 0 or > 9 || b is < 0 or > 9) throw new ArgumentOutOfRangeException(nameof(a));
        if (a > b) (a, b) = (b, a);
        return a * 10 - a * (a - 1) / 2 + b - a;
    }

    public static int StateIndex(int hp, int a, int b, int enemyHp, int c, int d, int maxHealth) =>
        (((hp - 1) * PairCount + PairIndex(a, b)) * maxHealth + enemyHp - 1) * PairCount + PairIndex(c, d);

    public static CompactActionTable Load(Stream stream)
    {
        using var gzip = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
        using var reader = new BinaryReader(gzip);
        if (new string(reader.ReadChars(4)) != "FDQ2" || reader.ReadInt32() != RulesVersion)
            throw new InvalidDataException("Agent 表格式或规则版本不匹配。");
        int hp = reader.ReadInt32();
        int count = reader.ReadInt32();
        if (hp is < 1 or > 32 || count != hp * hp * PairCount * PairCount * 6)
            throw new InvalidDataException("Agent 表尺寸无效。");
        int symbols = reader.ReadInt32(), bits = reader.ReadInt32();
        if (symbols is < 1 or > 256 || bits is < 1 or > 8 || symbols > (1 << bits))
            throw new InvalidDataException("Agent 分值字典无效。");
        var codebook = new short[symbols];
        for (int i = 0; i < symbols; i++) codebook[i] = reader.ReadInt16();
        int packedSize = (count * bits + 7) / 8;
        var codes = reader.ReadBytes(packedSize);
        if (codes.Length != packedSize) throw new EndOfStreamException("Agent 表被截断。");
        if (reader.BaseStream.ReadByte() != -1) throw new InvalidDataException("Agent 表包含多余数据。");
        var table = new CompactActionTable(hp, codes, codebook, bits);
        if (symbols != (1 << bits))
            for (int i = 0; i < count; i++)
                if (table.Code(i) >= symbols) throw new InvalidDataException("Agent 分值索引无效。");
        return table;
    }

    public bool Supports(GameManager game, int actorId)
    {
        // 模型只对应无技能的双人、相同生命上限、攻击对手/治疗自己的规则。
        return game.Players.Count == 2 && game.Query.CurrentPlayerId == actorId &&
            game.Players.All(p => p.IsAlive() && p.Skills.Count == 0 && p.MaxHealth == MaxHealth &&
                p.Health >= 1 && p.Health <= MaxHealth && p.Numbers.All(n => n is >= 0 and <= 9)) &&
            !game.AreAllies(game.Players[0].PlayerId, game.Players[1].PlayerId);
    }

    public double Score(Player source, Player enemy, int sourceIndex, int targetId, int targetIndex)
    {
        int first = source.Numbers[0] <= source.Numbers[1] ? sourceIndex : 1 - sourceIndex;
        int second = targetId == source.PlayerId ? 2 :
            enemy.Numbers[0] <= enemy.Numbers[1] ? targetIndex : 1 - targetIndex;
        int state = StateIndex(source.Health, source.Numbers[0], source.Numbers[1],
            enemy.Health, enemy.Numbers[0], enemy.Numbers[1], MaxHealth);
        return _codebook[Code(state * 6 + first * 3 + second)] / 32767.0;
    }

    /// <summary>从行动方视角读取 V=max Q；高于表范围的血量投影到最高档。</summary>
    public double Value(Player source, Player enemy)
    {
        if (!source.IsAlive()) return -1;
        if (!enemy.IsAlive()) return 1;
        int state = StateIndex(Math.Clamp(source.Health, 1, MaxHealth), source.Numbers[0], source.Numbers[1],
            Math.Clamp(enemy.Health, 1, MaxHealth), enemy.Numbers[0], enemy.Numbers[1], MaxHealth);
        double best = -1;
        for (int action = 0; action < 6; action++)
            best = Math.Max(best, _codebook[Code(state * 6 + action)] / 32767.0);
        return best;
    }

    private int Code(int index)
    {
        int bit = index * _bits, offset = bit / 8;
        int word = _codes[offset] | (offset + 1 < _codes.Length ? _codes[offset + 1] << 8 : 0);
        return (word >> (bit % 8)) & ((1 << _bits) - 1);
    }
}
