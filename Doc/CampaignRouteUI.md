# Campaign route reference implementation

The route page follows the supplied Chapter Route image. The sheet header sits
behind a square-edged detail card with two offset backing cards. The four Chapter I
missions use the reference's diamond composition and elbow paths. This presentation
does not rewrite campaign JSON or persistence.

## Where to edit

- `Content/Gum/Screens/CampaignRouteLandscape.gusx`: landscape geometry on a
  1600 x 960 canvas. Every header, detail content slot, divider, action, camera
  viewport and zoom hit region is named here.
- `Content/Gum/Screens/CampaignRoutePortrait.gusx`: 900 x 1600 stacked adaptation.
- `UI/Campaign/CampaignMapLayout.cs`: reads the Gum bounds. Both input and drawing
  use this adapter; do not add a separate set of hit coordinates.
- `UI/UIPaperFrame.cs`: reusable paper silhouettes, grain, edges and shadows.
- `UI/Campaign/UICampaignRouteView.cs`: typography, node cards, detail content,
  clipped elbow paths and zoom controls. Accepts read-only display values; has no
  access to progress stores, state machines or input dispatch.
- The chapter browser uses the same paper stack and detail component. The folded
  backing sheet is drawn underneath chapter/mission cards, with separately lit
  crease and underside planes. All paper faces use fine fibers plus subtle pulp
  variation; texture aspect ratios prevent stretched horizontal grain. Shadows
  stay close to the sheet, following the earlier chapter browser's shallow style.
- `UI/Campaign/CampaignRouteDiagram.cs`: Chapter I presentation positions, card
  width ratios and decorative links. Other chapters retain their generated layout.
- `UI/Campaign/CampaignMissionList.cs`: the phone mission rows (open / cleared /
  locked) and the replay rule. A cleared mission keeps its row; a locked one prints
  as grey stock instead of being dropped from the page.
- `UI/Campaign/CampaignMissionLayout.cs`: the phone mission sheet geometry. Drawing
  and hit testing share it, so the paper card and its ticket cannot drift apart.
- `UI/Campaign/UICampaignMap.cs`: camera, selection, input and intent adapter.
- `UI/Campaign/UICampaignRouteView.cs`: also draws the phone mission rows and sheet.
  Both reuse the chapter sheet, difficulty strip, reward seal and action ticket.
- `Content/paper/route_locations_concept.png`: four-cell, 2 x 2 scene atlas.

The scene atlas is an image_gen reconstruction, not a pixel-identical extraction.
The built-in tool and exact prompt are recorded in the sibling `.prompt.txt` file.
The runtime prepares slanted detail previews and transparent-sky miniatures once
when resources load. Existing mission art is retained as a fallback for other places.

## Verification

```powershell
dotnet build tools/UiPreview/UiPreview.csproj -c UiReview --no-restore -m:1 -p:BuildInParallel=false
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1600 960 artifacts/route-reference --route-only
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 1280 720 artifacts/route-reference --route-only
dotnet tools/UiPreview/bin/UiReview/net8.0/UiPreview.dll 720 1280 artifacts/route-reference --route-only
```

The route preview uses live controls without reading or writing user saves. It
checks all four diamond card hit regions, inspect-versus-enter intents, disjoint
map/detail areas, zoom/fit alignment and the existing locked-mission/back checks.
The top `03 · CHAPTER ROUTE` label belongs to the reference sheet, not game UI.
