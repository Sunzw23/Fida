using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fida.Logic
{
    /// <summary>
    /// 场景库：从 <c>Content/scenes/*.csv</c> 加载关卡（玩家列表）。
    /// CSV 表头：<c>id,name,hp,skills,positionIndex</c>；skills 列使用中文技能名（可为空）。
    /// </summary>
    public static class SceneLibrary
    {
        /// <summary>
        /// 枚举全部可用场景名（不含扩展名），按文件名排序，默认场景（DefaultScene）在前。
        /// </summary>
        public static List<string> SceneNames()
        {
            var dir = SceneDirectory();
            if (dir == null)
                return new List<string> { "DefaultScene" };

            var names = Directory.GetFiles(dir, "*.csv")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .ToList();

            names.Sort((a, b) =>
            {
                if (a == "DefaultScene") return -1;
                if (b == "DefaultScene") return 1;
                return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
            });

            return names.Count > 0 ? names : new List<string> { "DefaultScene" };
        }

        /// <summary>
        /// 获取场景信息（名称 + 预览描述），描述取本地玩家（id 0）的中文技能名作为副标题。
        /// 场景文件不存在时返回名称并给出空描述。
        /// </summary>
        public static SceneInfo GetScene(string name)
        {
            var players = LoadScene(name);
            string description = "";
            if (players.Count > 0)
            {
                var local = players.FirstOrDefault(p => p.PlayerId == 0) ?? players[0];
                if (local.Skills.Count > 0)
                    description = "Skills: " + string.Join(", ", local.Skills.Select(s => GameText.SkillName(s.Type)));
                else
                    description = "No skills";
            }
            return new SceneInfo(name, description);
        }

        /// <summary>
        /// 加载场景的玩家列表。文件不存在或解析失败时返回空列表（调用方据此回退默认场景）。
        /// </summary>
        public static List<Player> LoadScene(string name)
        {
            string dir = SceneDirectory();
            string path = dir != null ? Path.Combine(dir, name + ".csv") : "";
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return new List<Player>();

            var players = new List<Player>();
            foreach (var raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;
                if (line.StartsWith("id,")) // 跳过表头
                    continue;

                string[] parts = line.Split(',');
                if (parts.Length < 3)
                    continue;

                int id;
                int hp;
                if (!int.TryParse(CleanField(parts[0]), out id))
                    continue;

                string pname = CleanField(parts[1]);
                if (!int.TryParse(CleanField(parts[2]), out hp))
                    hp = 11;

                // skills 列可能带引号，如 "龙"；把第 4 列起的所有片段合并还原后去除外层引号
                string skillsField = parts.Length >= 4 ? string.Join(",", parts, 3, parts.Length - 3) : "";
                skillsField = skillsField.Trim().Trim('"').Trim();

                var skills = new List<Skill>();
                foreach (var token in skillsField.Split(','))
                {
                    string t = token.Trim().Trim('"').Trim();
                    if (t.Length == 0)
                        continue;
                    var skill = SkillFactory.CreateSkillFromChineseName(t);
                    if (skill != null)
                        skills.Add(skill);
                }

                players.Add(new Player(id, pname, hp, skills));
            }

            return players;
        }

        private static string SceneDirectory()
        {
            var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "scenes");
            return Directory.Exists(dir) ? dir : null;
        }

        private static string CleanField(string s) => s?.Trim().Trim('"').Trim() ?? "";
    }

    /// <summary>
    /// 场景展示信息：名称 + 副标题（本地玩家默认技能）。
    /// </summary>
    public class SceneInfo
    {
        public string Name { get; }
        public string Description { get; }

        public SceneInfo(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
