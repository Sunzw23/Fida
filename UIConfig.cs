using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    // UIConfig.cs
    public class UIConfig
    {
        // 字体和基础样式
        public static Color TextColor => Color.White;
        public static Color HighlightColor => Color.White;
        public static Color BackgroundColor => Color.Black;
        public static Color ButtonTextColor => Color.White;

        // 通用间距
        public static int Padding => 10;
        public static int Spacing => 5;

        // 玩家信息区域
        public static Vector2 NamePosition => new Vector2(200, 50);
        public static Rectangle HealthArea => new Rectangle(120, 120, 160, 80);

        public static List<Rectangle> NumberAreas => [new Rectangle(20, 120, 80, 80), new Rectangle(300, 120, 80, 80)];


        // 消息显示区域
        public static Rectangle GameMessageArea => new Rectangle(10, 10, 400, 30);
        public static Vector2 GameMessagePosition => new Vector2(200, 30);

        // 决策和选择信息区域
        public static Rectangle DecisionInfoArea => new Rectangle(10, 100, 400, 30);


        // 决策按钮区域
        public static Rectangle YesButtonArea => new Rectangle(60, 160, 100, 60);
        public static Rectangle NoButtonArea => new Rectangle(220, 160, 100, 60);




        // 数字选择区域
        public static Rectangle NumberSelectionArea => new Rectangle(10, 100, 260, 110);
        public static int NumberButtonWidth => 50;
        public static int NumberButtonHeight => 50;
        public static int NumberColumns => 5;

        // 玩家 UI 区域偏移
        public static Point PlayerNameOffset => new Point(0, 0);
        public static Point PlayerHpOffset => new Point(0, 20);
    }

    public class UIText
    {
        public static string NameSpace(string name) => name;
        public static string HealthSpace(int health) => $"Hp: {health}";
        public static string NumberSpace(int number) => number.ToString();

        public static string UseDoubleAttack => "使用双倍攻击吗?";
        public static string UseMo => "使用技能: 魔吗?";
        public static string UseTong => "使用技能: 同变吗?";
        public static string UseLong => "使用技能: 龙吗?";
        public static string ChooseNumberPrompt => "选择数字:";
        public static string Yes => "yes";
        public static string No => "no";
    }
}
