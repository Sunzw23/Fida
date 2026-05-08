using System;
using System.Collections.Generic;

namespace Fida.Logic
{
    public static class SkillManager
    {
        public static List<SkillType> GameStartSkills => [SkillType.TripleHealth, SkillType.DoubleHealth];
        public static List<SkillType> TurnStartSkills => [SkillType.Tong, SkillType.Di];
        public static List<SkillType> OrderingAttackSkills => [SkillType.Bian, SkillType.Long, SkillType.DoubleAttack, SkillType.PlusAttack];
        public static List<SkillType> OrderedAttackSkills => [SkillType.Xian];

        public static List<SkillType> DamagingSkills => [SkillType.Xue];
        public static List<SkillType> DamagedSkills => [SkillType.Shen, SkillType.Mo];

        public static int ApplyDefenceSkills(Player targetPlayer, int damage)
        {
            int finalDamage = damage;
            if (targetPlayer.SkillAvailable(SkillType.Shi))
            {
                return 0; // Shi cancels one attack completely per turn
            }
            if (targetPlayer.SkillAvailable(SkillType.PlusDefence))
            {
                finalDamage = Math.Max(0, finalDamage - 1);
            }
            if (targetPlayer.SkillAvailable(SkillType.DoubleDefence))
            {
                finalDamage /= 2;
            }
            return finalDamage;
        }

        public static GameState CreateGameStartSkillState(GameManager gameManager, int playerId, SkillType skillType) =>
            skillType switch
            {
                SkillType.TripleHealth => new TripleHealthSkillState(gameManager, playerId),
                SkillType.DoubleHealth => new DoubleHealthSkillState(gameManager, playerId),
                _ => null
            };

        public static GameState CreateTurnStartSkillState(GameManager gameManager, int playerId, SkillType skillType) =>
            skillType switch
            {
                SkillType.Tong => new TongSkillState(gameManager, playerId),
                _ => null
            };

        public static GameState CreateOrderedAttackSkillState(GameManager gameManager, int playerId, SkillType skillType) =>
            skillType switch
            {
                SkillType.Xian => new XianSkillState(gameManager, playerId),
                _ => null
            };

        public static GameState CreateOrderingAttackSkillState(GameManager gameManager, int playerId, SkillType skillType, int[] numbers, int baseDamage) =>
            skillType switch
            {
                SkillType.Bian => new BianSkillState(gameManager, playerId, baseDamage),
                SkillType.Long => new LongSkillState(gameManager, playerId, numbers, baseDamage),
                SkillType.DoubleAttack => new DoubleAttackSkillState(gameManager, playerId, baseDamage),
                SkillType.PlusAttack => new PlusAttackSkillState(gameManager, playerId, baseDamage),
                _ => null
            };

        public static GameState CreateDamagedSkillState(GameManager gameManager, int targetPlayerId, SkillType skillType, int finalDamage) =>
            skillType switch
            {
                SkillType.Shen => new ShenSkillState(gameManager, targetPlayerId),
                SkillType.Mo => new MoSkillState(gameManager, targetPlayerId, finalDamage),
                _ => null
            };

        public static GameState CreateDamagingSkillState(GameManager gameManager, int sourcePlayerId, SkillType skillType, int finalDamage) =>
            skillType switch
            {
                SkillType.Xue => new XueSkillState(gameManager, sourcePlayerId, finalDamage),
                _ => null
            };
    }

    public enum SkillType
    {
        PlusAttack,
        PlusDefence,
        DoubleHealth,
        DoubleAttack,
        DoubleDefence,
        TripleHealth,
        Shen,
        Xian,
        Mo,
        Gui,
        Xue,
        Tong,
        Di,
        Long,
        Huo,
        Bian,
        Shi,

    }

    public enum SkillUsageLimit
    {
        Unlimited,
        GameStart,
        PerRound,
        PerTurn,
        Limited
    }

    public class Skill: IEquatable<Skill>
    {
        public SkillType Type { get; init; }
        public int Value;
        
        public SkillUsageLimit UsageLimit { get; set; } = SkillUsageLimit.Unlimited;
        public int MaxUses { get; set; } = 1;
        public int RemainingUses { get; set; } = 1;

        public bool IsActivated 
        { 
            get => UsageLimit != SkillUsageLimit.Unlimited && RemainingUses <= 0; 
            set 
            {
                if (value) RemainingUses = 0; 
                else RemainingUses = MaxUses; 
            }
        }

        public bool Equals(Skill other)
        {
            return other != null && Type == other.Type;
        }
        
    }



    public static class SkillLibrary
    {
        public static Skill GetSkill(SkillType skillType)
        {
            return skillType switch
            {
                SkillType.PlusAttack => new Skill { Type = SkillType.PlusAttack, Value = 1 },
                SkillType.PlusDefence => new Skill { Type = SkillType.PlusDefence, Value = 1 },
                SkillType.DoubleHealth => new Skill { Type = SkillType.DoubleHealth, Value = 2, UsageLimit = SkillUsageLimit.GameStart },
                SkillType.DoubleAttack => new Skill { Type = SkillType.DoubleAttack, Value = 2 },
                SkillType.DoubleDefence => new Skill { Type = SkillType.DoubleDefence, Value = 2 },
                SkillType.TripleHealth => new Skill { Type = SkillType.TripleHealth, Value = 2, UsageLimit = SkillUsageLimit.GameStart },
                SkillType.Shen => new Skill { Type = SkillType.Shen, Value = 2, UsageLimit = SkillUsageLimit.PerTurn },
                SkillType.Xian => new Skill { Type = SkillType.Xian, Value = 2, UsageLimit = SkillUsageLimit.PerTurn },
                SkillType.Mo => new Skill { Type = SkillType.Mo, Value = 2 },
                SkillType.Xue => new Skill { Type = SkillType.Xue, Value = 2 },
                SkillType.Tong => new Skill { Type = SkillType.Tong, Value = 2 },
                SkillType.Long => new Skill { Type = SkillType.Long, Value = 3 },
                SkillType.Huo => new Skill { Type = SkillType.Huo, Value = 2 },
                SkillType.Bian => new Skill { Type = SkillType.Bian, Value = 3 },
                SkillType.Shi => new Skill { Type = SkillType.Shi, Value = 2, UsageLimit = SkillUsageLimit.PerTurn },
                _ => null
            };
        }

        

        
    }

}
