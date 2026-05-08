
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Fida.Logic;

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
        public static SceneLoader PlayersScene1 => new([new Player(0, "YOU"), new Player(1, "ENEMY")], [new Vector2(100, 700), new Vector2(100, 180)]);
    }
}
