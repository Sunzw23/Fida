
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
        public static SceneLoader PlayersScene(int i)
        {
            if (i == 0)
            {
                return SceneReader.LoadSceneFromCsv("Scenes/DefaultScene.csv");
            }
            else
            {
                return SceneReader.LoadSceneFromCsv($"Scenes/Scene{i}.csv");
            }

        }
    }
}
