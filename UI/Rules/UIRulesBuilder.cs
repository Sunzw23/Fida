#nullable enable
using System;
using System.Collections.Generic;
using Fida.Campaign;
using Fida.Logic;
using Fida.UI.Rules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

public partial class UIScreenBuilder
{
    private void BuildRulesPage(int page, int indexPage, bool marginsEditable, int digitA, int digitB)
    {
        var book = RulesBook.Current();
        page = ClampPage(page, book.TotalPages);
        var layout = new UIRulesLayout(_resourceManager.GraphicsDevice.Viewport);

        _uiControls.Add(new UIRulesSurface(_resourceManager, page, book.TotalPages) { Name = "rules_surface" });

        for (int i = 0; i < RulesBook.Chapters.Length; i++)
        {
            bool active = page < RulesBook.ReferencePages ? page == i : i == RulesBook.ReferencePages;
            var chip = EditorialButton($"rulesChapter{i}", RulesBook.Chapters[i], layout.Chip(i), active: active, left: true);
            chip.Navigation = true;
            chip.TextLineHeight = layout.Portrait ? 30 : 34;
            chip.FocusIndex = 1 + i;
        }

        if (page == RulesBook.TimingPage)
        {
            _uiControls.Add(new UIRulesTimingPage(_resourceManager, book) { Name = "rules_timing" });
        }
        else if (page == 1)
        {
            // The Numbers page is always the live table; the egg only decides if rows respond.
            bool editable = marginsEditable && Achievements.EasterEggUnlocked(CampaignRuntime.Progress);
            _uiControls.Add(new UIRulesNumbersAssembly(_resourceManager, editable,
                digitA, digitB) { Name = "rules_numbers" });
        }
        else if (page < RulesBook.ReferencePages)
        {
            _uiControls.Add(new UIRulesReferencePage(_resourceManager, page) { Name = "rules_reference" });
        }
        else if (page == RulesBook.IndexPage)
        {
            BuildSkillIndex(book, indexPage, layout);
        }
        else if (book.SkillAtPage(page) is { } skill)
        {
            _uiControls.Add(new UISkillCodexPage(_resourceManager, skill, book.Owns(skill), SkillCodex.IsNew(skill))
            {
                Name = "rules_skill"
            });
        }

        if (page == RulesBook.TimingPage)
            BuildTimingLinks(book, layout);

        EditorialBack("back");

        _uiControls.Add(new UIIconButton(91, layout.Previous, UIIconButton.IconKind.PreviousPage,
            _resourceManager.Pixel, _resourceManager.Font)
        { Name = "prevPage", FocusIndex = 100 });
        _uiControls.Add(new UIIconButton(92, layout.Next, UIIconButton.IconKind.NextPage,
            _resourceManager.Pixel, _resourceManager.Font)
        { Name = "nextPage", FocusIndex = 101 });

        EditorialFocus();
    }

    private void BuildSkillIndex(RulesBook book, int indexPage, UIRulesLayout layout)
    {
        int perPage = layout.IndexColumns * layout.IndexRows;
        int pageCount = Math.Max(1, (book.Entries.Count + perPage - 1) / perPage);
        indexPage = Math.Clamp(indexPage, 0, pageCount - 1);
        _uiControls.Add(new UIRulesIndexPage(_resourceManager, book, indexPage, pageCount) { Name = "rules_index" });

        int start = indexPage * perPage;
        for (int slot = 0; slot < perPage && start + slot < book.Entries.Count; slot++)
        {
            int entry = start + slot;
            var skill = book.Entries[entry];
            _uiControls.Add(new UISkillCodexTile(300 + entry, layout.Tile(slot), skill,
                book.Owns(skill), SkillCodex.IsNew(skill), _resourceManager)
            {
                Name = $"rulesTile{entry}",
                FocusIndex = 10 + slot
            });
        }

        if (pageCount <= 1) return;
        _uiControls.Add(new UIIconButton(93, layout.IndexPrevious, UIIconButton.IconKind.PreviousPage,
            _resourceManager.Pixel, _resourceManager.Font)
        { Name = "indexPrev", FocusIndex = 90 });
        _uiControls.Add(new UIIconButton(94, layout.IndexNext, UIIconButton.IconKind.NextPage,
            _resourceManager.Pixel, _resourceManager.Font)
        { Name = "indexNext", FocusIndex = 91 });
    }

    private void BuildTimingLinks(RulesBook book, UIRulesLayout layout)
    {
        int order = 0;
        foreach (var (skill, bounds) in UIRulesTimingPage.Links(book, layout, _resourceManager.CaptionFont))
        {
            int entry = book.IndexOf(skill);
            if (entry < 0) continue;
            _uiControls.Add(new UIRulesTimingLink(400 + order, bounds, GameText.SkillName(skill),
                _resourceManager, !book.Owns(skill))
            {
                Name = $"rulesLink{entry}",
                FocusIndex = 20 + order
            });
            order++;
        }
    }

    private static int ClampPage(int page, int total)
    {
        int count = Math.Max(1, total);
        return (page % count + count) % count;
    }
}
