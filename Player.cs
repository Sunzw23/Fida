// Player.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Globalization;

using System;

namespace Fida
{

    public enum SkillType
    {
        DoubleAttack,
        DoubleDefence,
        TripleHealth,
        Shen,
        Xian,
        Mo,
        Gui,
        Tong,
        Di,
    }


    public class Skill
    {
        public SkillType SkillType { get; set; }
        public String SkillName { get; set; }
    }


    /// <summary>
    /// 玩家类, 存储玩家的数据和行为
    /// </summary>
    public class Player
    {
        public int PlayerId { get; private set; } // 玩家ID
        public List<int> Numbers { get; private set; } // 玩家拥有的两个数字
        public int Health { get; set; } // 玩家生命值
        public List<Skill> Skills { get; private set; } // 玩家拥有的技能


        public int MaxHealth {get; set;} // 生命值上限

        // 构造函数, 初始化玩家
        public Player(int id, int health, List<Skill> skills)
        {
            PlayerId = id;
            Numbers = [1, 1]; // 初始数字为1, 1
            MaxHealth = health; // 最大生命值为 health
            Health = health; // 初始生命值为 health
            Skills = skills; // 玩家拥有技能
        }

        public Player(int id, int health)
        {
            PlayerId = id;
            Numbers = [1, 1]; // 初始数字为1, 1
            MaxHealth = health; // 最大生命值为 health
            Health = health; // 初始生命值为 health
            Skills = []; // 玩家没有技能
        }

        /// <summary>
        /// 玩家受到伤害
        /// </summary>
        /// <param name="damage"></param>
        /// <returns></returns>
        public string TakeDamage(int damage)
        {

            Health = CalNormalDamage(damage, Health); // 计算反弹后的生命值

            return $"玩家 {PlayerId} 受到 {damage} 点伤害. 当前生命值: {Health}";
        }


        /// <summary>
        /// 玩家获得治疗
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        public string Heal(int amount)
        {
            Health += amount;
            Health = Math.Clamp(Health, 0, MaxHealth); // 确保生命值不超出范围
            return $"玩家 {PlayerId} 恢复 {amount} 点生命值. 当前生命值: {Health}";
        }


        /// <summary>
        /// 设置玩家的某个数字
        /// </summary>
        /// <param name="index"></param>
        /// <param name="value"></param>
        public void SetNumber(int index, int value)
        {
            Numbers[index] = value;
        }


        /// <summary>
        /// 重置所有数字为1 (用于某些特殊效果)
        /// </summary>
        public void ResetAllNumbersToOne()
        {
            Numbers[0] = 1;
            Numbers[1] = 1;
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

        public bool HasSkill(SkillType skillType)
        {
            return Skills.Find(s => s.SkillType == skillType) != null;
        }



    }
}