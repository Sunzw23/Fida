using System;
using System.IO;
using System.Text.Json;

namespace Fida.Logic
{
    // 用于存储从JSON加载的文本数据的类
    public class GameTextData
    {
        public Messages Messages { get; set; } = new Messages();
    }

    public class Messages
    {
        public string GameStart { get; set; } = "";
        public string TurnStart { get; set; } = "";
        public string SkillActivated { get; set; } = "";
        public string ShieldBreak { get; set; } = "";
        public string Damaged { get; set; } = "";
        public string Healed { get; set; } = "";
        public string TrueDamaged { get; set; } = "";
        public string GameEndWithWinner { get; set; } = "";
        public string GameEndWithDraw { get; set; } = "";
        public string GameEnd { get; set; } = "";
        public string ChooseFirstNumberIndex { get; set; } = "";
        public string ChooseSecondNumberIndex { get; set; } = "";
        public string ChooseTargetForDamaging { get; set; } = "";
        public string ChooseTargetForHealing { get; set; } = "";
        public string SkillActivatedUseIf { get; set; } = "";
        public string SkillActivatedNumberTurnFrom { get; set; } = "";
        public string SkillActivatedNumberTurnInto { get; set; } = "";
        public string SkillActivatedChooseTarget { get; set; } = "";
        public string NumberChangedBothCheckEffect { get; set; } = "";
        public string NumberChangedSingleCheckEffect { get; set; } = "";
        public string AttackTriggeredBySingleNumber { get; set; } = "";
        public string AttackTriggeredByNumberPair { get; set; } = "";
        public string HealTriggeredBySingleNumber { get; set; } = "";
        public string HealTriggeredByNumberPair { get; set; } = "";
        public string MoSkillDamageReturn { get; set; } = "";
        public string SkillSelectionStatus { get; set; } = "";
        public string PlayerNameYou { get; set; } = "";
        public string PlayerNameEnemy1 { get; set; } = "";
        public string PlayerNameEnemy2 { get; set; } = "";
    }

    public static class GameText
    {
        private static GameTextData _textData;

        // 初始化文本数据
        static GameText()
        {
            LoadTextData();
        }

        private static void LoadTextData()
        {
            try
            {
                // 尝试多个可能的路径
                string[] possiblePaths = new[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gametext.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "gametext.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "gametext.json"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Content", "gametext.json"),
                    "gametext.json",
                    "Content/gametext.json",
                    // 添加更多可能的路径
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "gametext.json"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Content", "gametext.json")
                };

                string jsonString = null;
                foreach (var path in possiblePaths)
                {
                    string candidate = ContentSource.ReadText(path);
                    if (candidate != null)
                    {
                        jsonString = candidate;
                        Console.WriteLine($"Loaded gametext.json from: {path}");
                        break;
                    }
                }

                if (jsonString != null)
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _textData = JsonSerializer.Deserialize<GameTextData>(jsonString, options);
                }
                else
                {
                    Console.WriteLine("Failed to open gametext.json, using default data");
                    _textData = CreateDefaultData();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to load text data: {e.Message}");
                _textData = CreateDefaultData();
            }
        }

        private static GameTextData CreateDefaultData()
        {
            return new GameTextData
            {
                Messages = new Messages()
            };
        }

        public static string SkillName(SkillType skill) => SkillCatalog.EnglishName(skill);

        public static SkillType SkillTypeFromString(string skillName)
        {
            var type = SkillCatalog.FromName(skillName);
            if (type == null)
                throw new ArgumentException($"'{skillName}' is not a valid skill name.", nameof(skillName));
            return type.Value;
        }

        public static string GameStart => _textData?.Messages?.GameStart ?? "Game start!";

        public static string TurnStart(string name) =>
            string.Format(_textData?.Messages?.TurnStart ?? "{0}'s turn.", name);

        public static string SkillActivated(string name, SkillType skill) =>
            string.Format(_textData?.Messages?.SkillActivated ?? "{0} activated skill: {1}.", name, SkillName(skill));

        public static string ShieldBreak(string name) =>
            string.Format(_textData?.Messages?.ShieldBreak ?? "{0}'s shield was broken, damage was blocked!", name);

        public static string Damaged(string name, int damageValue) =>
            string.Format(_textData?.Messages?.Damaged ?? "{0} was attacked and lost {1} HP.", name, damageValue);

        public static string Healed(string name, int healValue) =>
            string.Format(_textData?.Messages?.Healed ?? "{0} was healed and recovered {1} HP.", name, healValue);

        public static string TrueDamaged(string name, int damageValue) =>
            string.Format(_textData?.Messages?.TrueDamaged ?? "{0} took true damage and lost {1} HP.", name, damageValue);

        public static string GameEndWithWinner(string name) =>
            string.Format(_textData?.Messages?.GameEndWithWinner ?? "Game over, {0} wins!", name);

        public static string GameEndWithDraw => _textData?.Messages?.GameEndWithDraw ?? "Game over, draw!";

        public static string GameEnd => _textData?.Messages?.GameEnd ?? "Game over!";

        public static string ChooseFirstNumberIndex(string name) =>
            string.Format(_textData?.Messages?.ChooseFirstNumberIndex ?? "{0}, please choose the first number:", name);

        public static string ChooseSecondNumberIndex(string name) =>
            string.Format(_textData?.Messages?.ChooseSecondNumberIndex ?? "{0}, please choose the second number:", name);

        public static string ChooseTargetForDamaging =>
            _textData?.Messages?.ChooseTargetForDamaging ?? "Please choose a target to attack:";

        public static string ChooseTargetForHealing =>
            _textData?.Messages?.ChooseTargetForHealing ?? "Please choose a target to heal:";

        public static string SkillActivatedUseIf(string name, SkillType skill) =>
            string.Format(_textData?.Messages?.SkillActivatedUseIf ?? "{0} triggered {1}. Use it?", name, SkillName(skill));

        public static string SkillActivatedNumberTurnFrom(string name, SkillType skill) =>
            string.Format(_textData?.Messages?.SkillActivatedNumberTurnFrom ?? "{0} triggered {1}. Choose a number to transform.", name, SkillName(skill));

        public static string SkillActivatedNumberTurnInto(string name, SkillType skill) =>
            string.Format(_textData?.Messages?.SkillActivatedNumberTurnInto ?? "{0} triggered {1}. Choose the resulting number.", name, SkillName(skill));

        public static string SkillActivatedChooseTarget(string name, SkillType skill) =>
            string.Format(_textData?.Messages?.SkillActivatedChooseTarget ?? "{0} triggered {1}. Choose a target.", name, SkillName(skill));

        public static string NumberChangedBothCheckEffect(string name, int newCalculatedNumber) =>
            string.Format(_textData?.Messages?.NumberChangedBothCheckEffect ?? "Player {0}'s two numbers became {1}. Checking effects...", name, newCalculatedNumber);

        public static string NumberChangedSingleCheckEffect(string name, int oldNumber, int newCalculatedNumber) =>
            string.Format(_textData?.Messages?.NumberChangedSingleCheckEffect ?? "Player {0}'s number {1} became {2}. Checking effects...", name, oldNumber, newCalculatedNumber);

        public static string AttackTriggeredBySingleNumber(string name, int number, int damage) =>
            string.Format(_textData?.Messages?.AttackTriggeredBySingleNumber ?? "Player {0}'s number became {1}, triggering attack! ({2} damage)", name, number, damage);

        public static string AttackTriggeredByNumberPair(string name, int damage) =>
            string.Format(_textData?.Messages?.AttackTriggeredByNumberPair ?? "Player {0}'s number pair triggered attack! ({1} damage)", name, damage);

        public static string HealTriggeredBySingleNumber(string name, int number, int healing) =>
            string.Format(_textData?.Messages?.HealTriggeredBySingleNumber ?? "Player {0}'s number became {1}, triggering heal! ({2} HP restored)", name, number, healing);

        public static string HealTriggeredByNumberPair(string name, int healing) =>
            string.Format(_textData?.Messages?.HealTriggeredByNumberPair ?? "Player {0}'s number pair triggered heal! ({1} HP restored)", name, healing);

        public static string MoSkillDamageReturn(int damageReturned) =>
            string.Format(_textData?.Messages?.MoSkillDamageReturn ?? ", return {0} damage", damageReturned);

        public static string SkillSelectionStatus(int remainingPoints, int maxPoints, string selectedSkills) =>
            string.Format(_textData?.Messages?.SkillSelectionStatus ?? "Available points: {0}/{1}\nSelected: {2}", remainingPoints, maxPoints, selectedSkills);

        public static string PlayerNameYou => _textData?.Messages?.PlayerNameYou ?? "You";
        public static string PlayerNameEnemy1 => _textData?.Messages?.PlayerNameEnemy1 ?? "Enemy 1";
        public static string PlayerNameEnemy2 => _textData?.Messages?.PlayerNameEnemy2 ?? "Enemy 2";
    }
}
