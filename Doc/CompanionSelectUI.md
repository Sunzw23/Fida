# Companion roster screen

The companion screen is the battle-preparation step that owns the party. It sits between
the campaign map and the skill loadout: map → **companions** → loadout → battle. A node
without companions skips it, and a zero-SP lesson goes companions → battle directly,
because there is nothing to equip but still someone to invite (or not).

## Layout

`CompanionSelectLayout` mirrors `SkillSelectionLayout` so the route, roster and loadout
screens share one Gum-authored header and back affordance.

| Band | Desktop | Portrait |
|---|---|---|
| Roster | 4 columns × 2 rows, under the header | 2 columns × 2 rows, fills the space above the sheet |
| Party bar | under the roster: `PARTY n / limit`, chips, pager | none: invited cards turn warm gold |
| Detail sheet | right column: large art, name, stats, description, state | under the roster: compact horizontal card |
| Continue | right column, below the sheet | footer, full width |

A short roster keeps its cards large instead of leaving a hole: `CompanionSelectLayout`
takes the card count, chooses 2 columns for one or two cards, and grows the used rows
towards a poster proportion (capped by the band). A full page falls back to the equal
4 × 2 grid. Tests that only need geometry can build the layout without a count and get
the full grid.

## Cards

`UICampaignCompanionSelect` draws exactly one paper card per roster entry: the screen shows
companions, never a placeholder. A card carries art, the name and one caption, `HP n`.
Invitation is a colour, not a separate summary row: an invited card turns warm gold and
carries a check, while an inviteable card stays cream with a plus. The focused card keeps
the cyan outline and drives the detail sheet. A full party dims the cards that can no
longer be invited. The phone therefore has no party band or count; the cards above are
the party. The desktop keeps its compact band under the roster for the chips and pager.

"Travel alone" is a state, not a card: the party starts empty, clicking an invited card
dismisses that member. On a phone the invited card itself is the state - gold face, check,
dismiss on click - so there is no `PARTY 0 / limit` row to repeat it. The desktop band
keeps the `PARTY n / limit` line and the removable chips.

The detail sheet is the 立绘 showcase: `UICompanionArt` draws `artKey`/`artPath` art
bottom-centred (cut-paper figures are cropped at the waist) or falls back to the shared
roster atlas, so a companion with no dedicated asset still appears. On a phone the bust
takes about a third of the sheet and the copy keeps the rest, instead of a full-height
square that squeezed the text into a narrow column. The stats line carries the mechanical
identity (`HP`, skills, `Gate ward`, `Ink Echo`); the description is flavour and is clamped
to three lines on a phone (four on desktop), ending on a finished sentence or an ellipsis.
The roster title stands alone; only an online draft adds a status line, so the party cap is
not repeated in the header, the bar and the sheet at once.

## Input and contract

- Mouse/touch: click a card to invite or dismiss, click a party chip to dismiss that
  member, wheel or the pager to change page, `CONTINUE`, or the header back arrow.
- Keyboard: arrows/WASD move focus, Enter/Space invites or dismisses, Page Up/Down or Tab
  change page, F5 confirms, Escape returns to the map.
- Gamepad: D-pad/left stick move, A toggles, shoulders page, Start confirms, B/Back leaves.

The control never mutates campaign state. It reports `TryConsumeToggle(id)`,
`ConsumeBackRequest()` and `ConsumeContinueRequest()`;
`CompanionSelectState` applies them through `GameSetup` and mirrors the authoritative party
back into the screen every frame. Paging is internal and never changes the party.

**Input wiring.** A state only receives input when two things line up: `Game1.Update` routes
the state to `MenuInputHandler`, and the handler has an entry for it. Missing either half
leaves the screen rendered but frozen - the briefing card covers everything and no click
reaches it, which is easy to mistake for a UI bug. `MenuInputHandler.HandledStates` exposes
the registered states and `Fida.Tests/InputRoutingTests.cs` fails if any state (other than
`Playing`/`OnlinePlaying`) is missing, so add the state to both lists together.

The before-battle briefing plays once per node on this screen, before the cards accept
input. `GameSetup.NodeBriefingShown` prevents a replay when the player returns from the
loadout or retries a failed battle. While the card is visible, `StateBase.HandleStoryInput`
polls only the overlay, so click/Enter/Space advance pages and Escape skips them.

## How this scales

- More companions: add entries to `Content/campaign/companions.json`. Cards, columns,
  centring and pages follow the roster; nothing in the screen names a specific companion.
- More ally slots: raise `CampaignCompanions.PartyLimit`. The battle card layout draws up
  to four units per side, so three companions plus the local player is the current ceiling.
- Per-battle restrictions: give a node a `companions` array of ids. The screen is built
  from that filtered roster, so a companion who cannot join simply has no card here.
- Online: the party is a solo-route decision. Cooperative battles keep the second slot for
  the human partner, so the roster screen is only reached on an independent route.

## Validation

```powershell
dotnet build tools/UiPreview/UiPreview.csproj -m:1 -p:UseSharedCompilation=false
dotnet tools/UiPreview/bin/Debug/net8.0/UiPreview.dll 1600 1000 artifacts/companion-screen --companions-only
dotnet tools/UiPreview/bin/Debug/net8.0/UiPreview.dll 720 1280 artifacts/companion-screen-portrait --companions-only
```

`--companions-only` renders the empty party, the invited party, and runs the real click
sequence through the control: one card per roster entry and nothing else, card hit regions
stay inside the roster band, a toggle is delivered exactly once, a selected card can be
dismissed again, and the screen's limit matches campaign data. `Fida.Tests/CompanionTests.cs` covers the party rules, the factory
seating and the layout bands at six resolutions without a graphics device.
