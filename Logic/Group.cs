using System.Collections.Generic;
using System.Linq;

namespace Fida.Logic
{
	/// <summary>
	/// 组类，用于维护同组玩家并判断组是否仍存活。
	/// </summary>
	public class Group
	{
		public int GroupId { get; }
		public string Name { get; private set; }
		public List<Player> Players { get; } = [];

		public Group(int groupId, string name, IEnumerable<Player>? players = null)
		{
			GroupId = groupId;
			Name = name;
			if (players != null)
			{
				Players.AddRange(players);
			}
		}

		public void AddPlayer(Player player)
		{
			if (!Players.Contains(player))
			{
				Players.Add(player);
			}
		}

		public bool IsAlive()
		{
			return Players.Any(p => p.IsAlive());
		}

		public void ChangeName(string name)
		{
			Name = name;
		}
	}
}
