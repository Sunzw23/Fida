using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Fida.Logic;

namespace Fida.Scene
{
    public class SceneLoader
    {
        public List<Player> Players { get; private set; } = [];

        public SceneLoader(List<Player> players)
        {
            Players = players;
        }
    }

    public class SceneConfig
    {
        /// <summary>顶部行 Y 坐标（敌方组）</summary>
        public const int RowTopY = 180;

        /// <summary>底部行 Y 坐标（本地玩家所在组）</summary>
        public const int RowBottomY = 700;

        public static SceneLoader PlayersScene1 => new([new Player(0, "YOU"), new Player(1, "ENEMY")]);
    }
}
