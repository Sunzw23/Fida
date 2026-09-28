#nullable enable
using System.Collections.Generic;
using Fida.Logic;

namespace Fida.Network;

public enum CampaignOnlinePhase : byte { Map, Loadout, Battle }

/// <summary>Latest public Campaign activity for one connected player.</summary>
public sealed record CampaignPresence(int PlayerId, CampaignOnlinePhase Phase, string NodeId, bool Ready,
    IReadOnlyCollection<SkillType>? Skills = null, string SessionId = "",
    IReadOnlyCollection<string>? CompletedNodes = null);
