using System.Collections.Generic;

namespace Fida.Campaign.Infrastructure
{
    public interface ICampaignNodeProvider
    {
        IReadOnlyList<CampaignNode> Load();
    }
}
