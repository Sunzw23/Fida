#nullable enable
using System;
using System.Collections.Generic;
using Fida.Campaign;
using Microsoft.Xna.Framework;

namespace Fida.UI.Campaign
{
    /// <summary>Spatial node navigation shared by keyboard and controller input.</summary>
    public static class CampaignMapNavigator
    {
        public static int FindNext(IReadOnlyList<CampaignNode>? nodes, int currentIndex, Vector2 direction,
            Func<CampaignNode,Vector2>? getPosition = null)
        {
            if (nodes == null || nodes.Count == 0) return -1;
            if (currentIndex < 0 || currentIndex >= nodes.Count) return 0;
            if (direction.LengthSquared() < 0.01f) return currentIndex;

            direction.Normalize();
            Vector2 origin = getPosition?.Invoke(nodes[currentIndex]) ?? Position(nodes[currentIndex]);
            int bestIndex = currentIndex;
            float bestScore = float.MaxValue;

            for (int i = 0; i < nodes.Count; i++)
            {
                if (i == currentIndex) continue;
                Vector2 delta = (getPosition?.Invoke(nodes[i]) ?? Position(nodes[i])) - origin;
                float forward = Vector2.Dot(delta, direction);
                if (forward <= 0f) continue;

                float distanceSquared = delta.LengthSquared();
                float perpendicularSquared = Math.Max(0f, distanceSquared - forward * forward);
                float score = forward + perpendicularSquared / Math.Max(1f, forward) * 2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static Vector2 Position(CampaignNode node) => new(node.MapX, node.MapY);
    }
}
