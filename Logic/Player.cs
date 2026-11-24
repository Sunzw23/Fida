// Player.cs 文件

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 玩家类, 存储玩家的数据和行为
    /// </summary>
    public class Player
    {
        public String Name { get; private set;  } // 玩家名称
        public int PlayerId { get; private set; } // 玩家ID
        /// <summary>
        /// Logical portrait asset key. UI resolves this key through ResourceManager,
        /// keeping gameplay data independent from content file paths.
        /// </summary>
        public string PortraitKey { get; set; } = "";
        public int[] Numbers {get; private set; } = [1, 1];
        public int Health { get; set; } = 11; // 玩家生命值
        public int AttacksReceivedThisTurn { get; private set; }
        /// <summary>Campaign passive: blocks the first incoming attack of a turn.</summary>
        public bool WardsFirstHit { get; set; }
        /// <summary>Campaign passive: the next damaging attack lands twice.</summary>
        public bool DoubleStrikeArmed { get; set; }
        public List<Skill> Skills { get; private set; } = []; // 玩家拥有的技能

        public IEnumerator<Skill> GetSkillsByTrigger(SkillTrigger trigger)
        {
            // Skills are evaluated in the order they appear in the player's loadout.
            return Skills.Where(skill => skill.Trigger == trigger).GetEnumerator();
        }

        /// <summary>
        /// Collects the non-null states for this player's usable skills whose trigger is one of
        /// <paramref name="triggers"/>, in loadout order. Used by the batch "trigger + handle" states
        /// (turn start, game start, attack completed, damage reactions).
        /// </summary>
        public IEnumerable<GameState> CollectTriggeredStates(GameManager gm, SkillContext ctx, params SkillTrigger[] triggers)
        {
            foreach (var skill in Skills)
            {
                if (!triggers.Contains(skill.Trigger)) continue;
                if (!skill.CanUse(gm, this)) continue;
                var state = skill.CreateState(gm, ctx);
                if (state != null) yield return state;
            }
        }


        public int MaxHealth { get; set; } = 11; // 生命值上限

        // 构造函数, 初始化玩家
        public Player(int id, string name, int health, List<Skill> skills)
        {
            PlayerId = id;
            Name = name; // 玩家名称
            MaxHealth = health; // 最大生命值为 health
            Health = health; // 初始生命值为 health
            Skills = skills; // 玩家拥有技能
        }

        public Player(int id, string name)
        {
            PlayerId = id;
            Name = name;
        }

        /// <summary>
        /// 替换玩家拥有的技能列表（用于单机开局按所选技能覆写本地玩家默认技能）。
        /// </summary>
        public void ReplaceSkills(List<Skill> skills)
        {
            Skills = skills ?? new List<Skill>();
        }

        /// <summary>
        /// 玩家受到伤害
        /// </summary>
        /// <param name="damage"></param>
        /// <returns></returns>
        public void TakeDamage(int damage)
        {
            Health = CalNormalDamage(damage, Health); // 计算反弹后的生命值
            HealthChanged?.Invoke(this);
        }


        /// <summary>
        /// 玩家获得治疗
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public void Heal(int amount)
        {
            Health += amount;
            Health = Math.Clamp(Health, 0, MaxHealth); // 确保生命值不超出范围
            HealthChanged?.Invoke(this);
        }


        /// <summary>
        /// 设置玩家的某个数字
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public event Action<Player, int, int, int>? NumberChanged; // player, index, oldValue, newValue
        public event Action<Player>? HealthChanged;

        public void SetNumber(int index, int value)
        {
            int old = Numbers[index];
            Numbers[index] = value;
            NumberChanged?.Invoke(this, index, old, value);
        }


        /// <summary>
        /// 重置所有数字为1 (用于某些特殊效果)
        /// </summary>
        public void ResetAllNumbersToOne()
        {
            SetNumber(0, 1);
            SetNumber(1, 1);
        }

        /// <summary>
        /// 检查玩家是否存活
        /// </summary>
        /// <returns></returns>
        public bool IsAlive()
        {
            return Health != 0;
        }

        /// <summary>
        /// 获取玩家是否具有护盾
        /// </summary>
        /// <returns></returns>
        public (bool, int) HasShield()
        {
            if (Numbers[0] == 0)
            {
                return (true, 0);
            }
            else if (Numbers[1] == 0)
            {
                return (true, 1);
            }
            else
            {
                return (false, -1);
            }
        }

        private static int CalNormalDamage(int damage, int health) 
        {
            return Math.Abs(health - damage);
        }


        public void ResetPerTurnSkills()
        {
            foreach (var skill in Skills.FindAll(s => s.Usage.UsageLimit == SkillUsageLimit.PerTurn))
            {
                skill.Usage.Reset();
            }
        }

        public void RecordAttackReceived() => AttacksReceivedThisTurn++;

        public void ResetAttacksReceivedThisTurn() => AttacksReceivedThisTurn = 0;

        /// <summary>Restores per-turn combat counters when applying a network checkpoint.</summary>
        public void RestoreTurnCounters(int attacksReceived) => AttacksReceivedThisTurn = Math.Max(0, attacksReceived);
    }
}
