# Skill selection grid

The cold UI loadout concept is implemented with a four-column grid, fixed detail
panel, cost preview and removable equipped chips. The complete 20-skill catalog
uses two desktop pages; the All/Offense/Defense/Control filters retain focus when
possible. A portrait viewport uses three columns and stacked details.

White outlines indicate the skill being inspected; cyan outlines and checkmarks
indicate equipped skills. Locked, over-budget and conflicting states include
text, not just color. Clicking a tile inspects it; the detail action equips or
removes it. Equipped chips remove their skill and an overflow button exposes
additional equipped entries without introducing a slot limit.

Budget costs, unlocks and conflict checks use `CampaignLoadout` and
`CampaignSkillBalance`. The screen reports intent; campaign and network states
still own the selected loadout. Numerical effect examples are marked as base and
result values. Geometric icons are antialiased masks generated at runtime.

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
