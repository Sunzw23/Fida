
using Microsoft.Xna.Framework;
using System.Collections.Generic;


namespace Fida
{
    public class SceneLoader
    {
        public List<Player> Players { get; private set; } = [];
        public List<Vector2> Positions { get; private set; } = [];

        public SceneLoader(List<Player> players, List<Vector2> positions)
        {
            Players = players;
            Positions = positions;
        }
    }
    public class SceneConfig
    {
        public static SceneLoader PlayersScene1 => new([new Player(0, "你"), new Player(1, "敌人")], [new Vector2(100, 800), new Vector2(100, 500)]);
    }
}
