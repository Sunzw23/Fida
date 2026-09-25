namespace Fida.UI.Campaign
{
    /// <summary>Visual configuration for alternate skill-wheel layouts and themes.</summary>
    public sealed class CampaignSkillWheelOptions
    {
        public int VisibleNeighborCount { get; init; } = 2;
        public int WheelCenterOffsetFromPanelTop { get; init; } = 600;
        public float FocusedWidthRatio { get; init; } = 0.46f;
        public int FocusedHeight { get; init; } = 126;
        public int NeighborSpacing { get; init; } = 112;
        public float FocusedTextScale { get; init; } = 0.66f;
        public float NearTextScale { get; init; } = 0.46f;
        public float FarTextScale { get; init; } = 0.42f;

        public static CampaignSkillWheelOptions Mobile() => new()
        {
            VisibleNeighborCount = 1,
            WheelCenterOffsetFromPanelTop = 580,
            FocusedWidthRatio = 0.86f,
            FocusedHeight = 150,
            NeighborSpacing = 132,
            FocusedTextScale = 0.72f,
            NearTextScale = 0.58f,
            FarTextScale = 0.52f
        };
    }
}
