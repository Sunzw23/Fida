# Skill selection grid

The cold UI loadout concept is implemented with a four-column grid, fixed detail
panel, cost preview and removable equipped chips. The complete 20-skill catalog
uses two desktop pages; the All/Offense/Defense/Control filters retain focus when
possible. A portrait viewport uses three columns and a compact inspector.

White outlines indicate the skill being inspected; cyan outlines and checkmarks
indicate equipped skills. The page speaks in marks rather than labels: one diamond is
one SP (a tile shows its cost as a diamond row, the budget is a diamond plus
`spent / budget`, the inspector repeats a diamond and the number), page turning is a
row of page diamonds instead of `1 / 3`, the equipped count is a check plus the
number, and the category tabs are glyphs (a dot grid for All, then the strike, crest
and dial the three families already use). Locked, over-budget and conflicting skills
are dimmed instead of carrying a sentence, and the inspector explains the reason once
for the focused skill. Clicking a tile inspects it; the inspector's ticket equips or
removes it - a plus/minus stamp on a phone, a labeled ticket on desktop. Equipped
chips print only the skill name and a remove mark, and an overflow button exposes
additional equipped entries without introducing a slot limit. On a phone the start
ticket reads `CONTINUE`; the ban and ready modes keep their `BAN` / `WAIT` words.

On desktop the budget sits at the right of the header, the equip ticket closes the
detail card, and the equipped chips share the footer with the start action. On a
phone the whole page is one vertical column: header with the budget and a thin
spent/budget meter, tabs, a grid that takes every remaining pixel (a filtered
category centres its used rows), a compact inspector with the equip ticket in its
header and the equipped chips at its foot, and a bottom row of the party summary and
the start ticket. The party ticket itself is the "change party" affordance (tap it or
the swap mark) and returns to the companion step, so there is no separate back button;
the `CONTINUE` ticket starts the battle. Both respect the Android gesture-bar inset. A node
with no companion step drops the party ticket and the start ticket owns the whole row.
The old phone page stretched the detail card to the bottom and left a tall empty
middle between the effect sentence and the equip ticket; the inspector now fits its
content and the grid gets the space instead.

Budget costs, unlocks and conflict checks use `CampaignLoadout` and
`CampaignSkillBalance`. The screen reports intent; campaign and network states
still own the selected loadout. Numerical effect examples are marked as base and
result values. Geometric icons are antialiased masks generated at runtime.

## Party strip

`SkillSelectionLayout.Party` is the left segment of the phone's bottom row (next to the
back and start tickets); on desktop it is the band under the grid. `UICampaignPartyStrip`
draws it as one paper ticket: one diamond per party slot (filled for each companion
already invited), the invited companions as compact name chips with their art, and a
two-way swap mark on the right that reopens the roster. Health stays on the roster
screen; the strip is a one-line summary, not a second character sheet. An online draft
replaces the swap mark with its `CO-OP · ALLY JOINS` status text.

The ticket is a single target, not a control cluster: a click anywhere on it, `C`, gamepad
`Y`, or Back returns to `UICampaignCompanionSelect`, which owns the party choice (roster
cards, portraits, and the limit). The strip therefore duplicates no rules - it mirrors
`GameSetup.SelectedCompanionIds` every frame and reports one "change party" intent.
Companion selection itself is documented in `Doc/CompanionSelectUI.md`.

## Skill marks

`UI/Campaign/UISkillArt.cs` holds the whole icon set: one 100 x 100 polygon mask per
`SkillType`, built once into a single atlas with 2 x 2 supersampling and drawn tinted, so the
marks cost no texture asset and every screen shares them. Fill and cut polygons are painted in
order, which is how the holes in the crest, the heart and the dial are made.

The vocabulary is deliberately one idea per mark, cut as paper shapes rather than pictographic
detail, because the marks are read at 30-60 px on a card:

- offense is a strike - a bold diagonal cut, doubled for Double Attack, a fang for Dragon;
- defense is a pointed crest, split cleanly through the middle for Double Defense;
- vitality is a heart, banded into three for Triple Health, and a drop for Blood;
- control uses sigils - a dial for Transform, a diamond for Immortal, a crown for Emperor,
  a heavy hexagon for God, chevrons for the extra-step skills, two number tiles for Same;
- the super tier is the base sigil plus one small punch mark in the corner.

The rules codex used to print a two-letter code (`A+`, `DR`, `TR`) where an icon belongs, so the
same skill looked different in the loadout and in the index. `UISkillCodexTile`, the codex medal
and the codex watermark now draw the shared mark. The letter codes survive only in the compact
hover list on a battle card, where they act as short bullets.

Iterate shapes with `tools/comfy/skill_icon_lab.py`, which reproduces the rasteriser exactly
(same 100 x 100 space, same fill/cut order, same supersampling) and renders the set at any size, so
a shape can be judged without rebuilding the game; then paste the points back into the C# switch.

## Compatibility and controls

`UICampaignSkillWheel` retains the established public state-machine contract and
delegates the live UI to `UICampaignSkillGrid`. Its previous implementation is
preserved. Duel ban, pick, lock and cooperative ready flags use the same contract;
disabled Start and frozen selections are enforced before input is dispatched.

- Mouse: click a tile, then Equip/Remove; wheel and arrows change pages.
- Keyboard: arrows navigate by grid position; Enter/Space equip or remove;
  Q/E or Tab change category; Page Up/Down change page; F5 starts; Escape returns.
- Gamepad: D-pad/left stick navigate; A equips/removes; shoulders change category;
  Start confirms; B/Back returns.

## Validation

```powershell
dotnet test Fida.Tests/Fida.Tests.csproj -c UiReview --no-restore -m:1 -p:BuildInParallel=false
dotnet build tools/UiPreview/UiPreview.csproj -c UiReview -m:1 -p:BuildInParallel=false
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1600 1000 artifacts/skill-grid --skills-only
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1280 720 artifacts/skill-grid --skills-only
dotnet build tools/UiPreview/UiPreview.csproj -c UiReview -p:DefineConstants=FIDA_ANDROID -p:SkipDotnetToolRestore=true -m:1
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1080 2400 artifacts/android-loadout --skills-only --tolerant
```

UiReview keeps verification separate from a game running in Visual Studio's
Debug output. The preview copies compiled game content after project references
have built, so a fresh configuration can be verified in one build.

The preview drives real click sequences through the compatibility facade and
checks inspect/equip/remove, duplicate events, unlocks, budget, conflicts, disabled
start, frozen selection, modal blocking, category/page navigation, drag cancellation
and ban-versus-start semantics. It saves normal, second-page, filtered, budget-full,
ready and ban views. Pure tests also cover spatial navigation and desktop/portrait
hit-region containment. Android builds are checked; physical touch/gamepad hardware
has not been exercised by the automated preview.
