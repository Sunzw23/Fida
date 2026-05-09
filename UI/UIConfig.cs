using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fida.UI
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

        // 玩家卡片布局（黄金比例 1.618:1，高：宽）
        public static int CardWidth => 300;
        public static int CardHeight => 485; // 300 * 1.618 ≈ 485
        public static int CardBorderThickness => 2;

        // 头像（卡片顶部居中，正方形黑白画）
        // 头像大小接近底部元素总长度：3*70 + 2*12 = 234
        public static int PortraitSize => 220;
        // 竖直等间距计算：(469 - 220 - 30 - 45) / 4 ≈ 43
        public static int PortraitTopMargin => 43; // 内框顶部到头像的间距

        // 名称（头像下方，居中）
        public static int NameTopMargin => 43; // 头像底部到名字的间距
        
        // 名称文字高度（预留空间）
        public static int NameHeight => 30;

        // 底部三个元素：数字 0、生命值、数字 1（水平并排）
        // 间距计算：内框宽度 284 - 3 个元素宽度 210 = 74，4 个等间距 = 74/4 ≈ 18
        public static int BottomElementWidth => 70;
        public static int BottomElementHeight => 70;
        public static int BottomElementTopMargin => 43; // 名字底部到底部元素的间距（竖直等间距）
        public static int BottomElementSpacing => 18; // 等间距：左右边距 + 元素间 2 个间距 = 4 个相等间距

        // 内框边距（双层方框的内框）
        public static int InnerBorderMargin => 8;
        public static int InnerBorderThickness => 1;

        // 底部内边距
        public static int CardBottomPadding => 15;


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
}
