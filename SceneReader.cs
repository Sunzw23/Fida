using Fida;
using Microsoft.Xna.Framework; // For Vector2
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// 确保你的 SceneLoader, Player, SkillLibrary 等类都在你的项目中
namespace Fida
{
    public class SceneReader
    {
        /// <summary>
        /// 根据 CSV 文件路径创建一个 SceneLoader 对象。
        /// </summary>
        /// <param name="csvFilePath">CSV 文件的完整路径。</param>
        /// <returns>一个包含玩家和位置数据的 SceneLoader 对象，如果文件不存在则返回 null。</returns>
        public static SceneLoader LoadSceneFromCsv(string csvFilePath)
        {
            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine($"Error: CSV file not found at '{csvFilePath}'");
                return null;
            }

            var players = new List<Player>();
            var positions = new List<Vector2>();

            var lines = File.ReadAllLines(csvFilePath).Skip(1); // 跳过列头

            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length < 5)
                {
                    continue;
                }

                int id = int.Parse(parts[0].Trim());
                string name = parts[1].Trim();
                string hpString = parts[2].Trim();
                string skillsString = parts[3].Trim().Trim('"');
                int positionIndex = int.Parse(parts[4].Trim());

                // 1. 创建 Player 对象
                Player player;
                int hp = 6; // 默认血量
                if (int.TryParse(hpString, out int parsedHp))
                {
                    hp = parsedHp;
                }

                var skills = new List<Skill>();
                if (!string.IsNullOrEmpty(skillsString))
                {
                    var skillTypes = skillsString.Split('|');
                    foreach (var skillType in skillTypes)
                    {
                        // 使用你的 SkillLibrary.GetSkill 方法来获取技能
                        // 确保你的 SkillType 枚举和 SkillLibrary 是可访问的

                        skills.Add(SkillLibrary.GetSkill(GameText.SkillTypeFromString(skillType)));
                    }
                }

                player = new Player(id, name, hp, skills);
                players.Add(player);

                // 2. 创建 Vector2 对象
                // 确保你的 UIConfig.PlayerInfoPosition 方法是可访问的
                positions.Add(UIConfig.PlayerInfoPosition(positionIndex));
            }

            // 3. 返回 SceneLoader 对象
            return new SceneLoader(players, positions);
        }
    }
}