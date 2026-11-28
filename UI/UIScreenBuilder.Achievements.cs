using System;
using Fida.Campaign;
using Fida.UI.Rules;

namespace Fida.UI
{
    public partial class UIScreenBuilder
    {
        /// <summary>The achievement sheet: derived progress plus the effect-table counter.</summary>
        public void BuildAchievementsScreen(int page = 0)
        {
            Achievements.EnsureLoaded();
            Clear();
            var layout = new UIRulesLayout(_resourceManager.GraphicsDevice.Viewport);
            int pages = UIAchievementsPage.PageCount(layout);
            page = ((page % pages) + pages) % pages;

            _uiControls.Add(new UIAchievementsPage(_resourceManager, CampaignRuntime.Progress, page)
            {
                Name = "achievements_page"
            });
            EditorialBack("back");

            if (pages > 1)
            {
                _uiControls.Add(new UIIconButton(91, layout.Previous, UIIconButton.IconKind.PreviousPage,
                    _resourceManager.Pixel, _resourceManager.Font)
                { Name = "prevPage", FocusIndex = 100 });
                _uiControls.Add(new UIIconButton(92, layout.Next, UIIconButton.IconKind.NextPage,
                    _resourceManager.Pixel, _resourceManager.Font)
                { Name = "nextPage", FocusIndex = 101 });
            }

            EditorialFocus();
            Achievements.Save();
        }
    }
}
