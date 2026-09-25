using Fida.Campaign;
using Fida.Network;
using Xunit;
using Fida.Logic;

namespace Fida.Tests;

public class OnlineModeRulesTests
{
    [Fact]
    public void DuelPreset_IsSymmetricalEightPointBaseline()
    {
        Assert.Equal(OnlineModeRules.DuelSkillBudget,
            CampaignSkillBalance.TotalCost(OnlineModeRules.DuelDefaultLoadout));
    }

    [Fact]
    public void DuelLoadout_MustSpendExactlyBudgetAndRespectBans()
    {
        SkillType[] exact = [SkillType.PlusAttack, SkillType.DoubleAttack, SkillType.Shen];
        Assert.True(OnlineModeRules.IsLegalDuelLoadout(exact, []));
        Assert.False(OnlineModeRules.IsLegalDuelLoadout([SkillType.Shen], []));
        Assert.False(OnlineModeRules.IsLegalDuelLoadout(exact, [SkillType.Shen]));
    }

    [Fact]
    public void DuelLoadout_RejectsConflictingDamageReplacements()
    {
        Assert.False(OnlineModeRules.IsLegalDuelLoadout(
            [SkillType.Long, SkillType.Bian], []));
    }

    [Fact]
    public void ConfiguredDuelLoadouts_AreStoredPerPlayer()
    {
        SkillType[] first = [SkillType.PlusAttack, SkillType.DoubleAttack, SkillType.Shen];
        SkillType[] second = [SkillType.DoubleHealth, SkillType.DoubleAttack,
            SkillType.Tong];
        OnlineModeRules.ConfigureDuelLoadouts(first, second);

        Assert.Equal(first, OnlineModeRules.GetDuelLoadout(0));
        Assert.Equal(second, OnlineModeRules.GetDuelLoadout(1));
    }
}
