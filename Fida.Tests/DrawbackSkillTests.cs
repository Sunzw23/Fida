using Fida.Campaign;
using Fida.Logic;
using Fida.UI;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// The drawback cards exist as data only. These tests pin that boundary down: they are named and
/// priced, but they are not selectable, not creatable and not reachable from any loadout.
/// </summary>
public class DrawbackSkillTests
{
    [Theory]
    [InlineData(SkillType.HalfAttack, "Half Attack", "攻击减半", -3)]
    [InlineData(SkillType.HalfHealth, "Half Health", "生命减半", -2)]
    [InlineData(SkillType.Bleed, "Bleed", "失血", -2)]
    public void Drawback_IsNamedAndPricedButNotImplemented(
        SkillType type, string english, string chinese, int cost)
    {
        Assert.Equal(english, SkillCatalog.EnglishName(type));
        Assert.Equal(chinese, SkillCatalog.ChineseName(type));
        Assert.True(SkillCatalog.FromName(chinese) == type);
        Assert.True(SkillCatalog.FromName(english) == type);
        Assert.False(SkillCatalog.CanCreate(type));
        Assert.Throws<System.InvalidOperationException>(() => SkillFactory.CreateSkill(type));
        Assert.Equal(cost, CampaignSkillBalance.Cost(type));
        Assert.True(CampaignSkillBalance.Cost(type) < 0);
        Assert.NotEqual("?", UISkillIconCatalog.Glyph(type));
    }

    [Fact]
    public void Drawbacks_StayOutOfTheSelectablePool()
    {
        Assert.DoesNotContain(SkillType.HalfAttack, SkillFactory.SelectableSkills);
        Assert.DoesNotContain(SkillType.HalfHealth, SkillFactory.SelectableSkills);
        Assert.DoesNotContain(SkillType.Bleed, SkillFactory.SelectableSkills);
    }

    [Fact]
    public void Drawbacks_AreNotRewardedByAnyNode()
    {
        var rewarded = CampaignCatalog.Nodes.SelectMany(node => node.Rewards).ToArray();
        Assert.DoesNotContain(SkillType.HalfAttack, rewarded);
        Assert.DoesNotContain(SkillType.HalfHealth, rewarded);
        Assert.DoesNotContain(SkillType.Bleed, rewarded);
    }
}
