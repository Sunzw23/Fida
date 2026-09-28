using Fida.Logic;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// 覆盖 GameText 的命名往返与消息格式化。
/// 命名往返不依赖 gametext.json 是否被加载：
/// SkillName 与 SkillTypeFromString 共享 SkillCatalog 这一数据源，故映射自洽。
/// </summary>
public class GameTextTests
{
    [Theory]
    [InlineData(SkillType.PlusAttack)]
    [InlineData(SkillType.Tong)]
    [InlineData(SkillType.Shen)]
    [InlineData(SkillType.Long)]
    [InlineData(SkillType.Bian)]
    public void SkillNameRoundTrip_IsConsistent(SkillType skill)
    {
        var name = GameText.SkillName(skill);
        Assert.False(string.IsNullOrWhiteSpace(name));
        Assert.Equal(skill, GameText.SkillTypeFromString(name));
    }

    [Fact]
    public void SkillName_ReturnsNonEmptyForEveryType()
    {
        foreach (var skill in Enum.GetValues<SkillType>())
        {
            var name = GameText.SkillName(skill);
            Assert.False(string.IsNullOrWhiteSpace(name));
        }
    }

    [Fact]
    public void RoundTrip_BothEnglishAndChineseNamesForEveryType()
    {
        foreach (var entry in SkillCatalog.All)
        {
            // 英文名往返
            Assert.Equal(entry.Type, GameText.SkillTypeFromString(entry.EnglishName));

            // 中文名往返（存在中文名时）
            if (entry.ChineseName != null)
                Assert.Equal(entry.Type, GameText.SkillTypeFromString(entry.ChineseName));
        }
    }

    [Fact]
    public void SkillTypeFromString_UnknownThrows()
    {
        Assert.Throws<System.ArgumentException>(() => GameText.SkillTypeFromString("not-a-skill"));
    }

    [Fact]
    public void TurnStart_FormatsPlayerName()
    {
        var text = GameText.TurnStart("Alice");
        Assert.Contains("Alice", text);
    }

    [Fact]
    public void NumberChangedBothCheckEffect_FormatsNameAndValue()
    {
        var text = GameText.NumberChangedBothCheckEffect("Alice", 5);
        Assert.Contains("Alice", text);
        Assert.Contains("5", text);
    }

    [Fact]
    public void NumberChangedSingleCheckEffect_FormatsNameOldAndNew()
    {
        var text = GameText.NumberChangedSingleCheckEffect("Alice", 2, 5);
        Assert.Contains("Alice", text);
        Assert.Contains("2", text);
        Assert.Contains("5", text);
    }

    [Fact]
    public void ChooseFirstNumberIndex_FormatsPlayerName()
    {
        Assert.Contains("Bob", GameText.ChooseFirstNumberIndex("Bob"));
    }

    [Fact]
    public void Damaged_FormatsNameAndDamage()
    {
        var text = GameText.Damaged("Bob", 3);
        Assert.Contains("Bob", text);
        Assert.Contains("3", text);
    }
}
