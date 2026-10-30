using Fida;
using Fida.Campaign;
using Fida.Logic;
using Fida.UI;
using Fida.UI.Campaign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

int width = args.Length > 0 ? int.Parse(args[0]) : 1600;
int height = args.Length > 1 ? int.Parse(args[1]) : 1000;
string destination = Path.GetFullPath(args.Length > 2 ? args[2] : "artifacts/cold-ui");
Directory.CreateDirectory(destination);
using var preview = new PreviewGame(width, height, destination, args.Contains("--skills-only"), args.Contains("--menus-only"), args.Contains("--reference-only"));
preview.Run();

sealed class PreviewGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly int _width, _height;
    private readonly string _destination;
    private ResourceManager _resources;
    private UIManager _ui;
    private int _screen, _frame;
    private double _firstDraw = double.NaN;
    private GameManager _battle;
    private readonly bool _skillsOnly;
    private readonly bool _menusOnly;
    private readonly bool _referenceOnly;
    private UICampaignSkillWheel _loadout;
    private HashSet<SkillType> _loadoutSelected;

    public PreviewGame(int width, int height, string destination, bool skillsOnly, bool menusOnly, bool referenceOnly)
    {
        _width = width; _height = height; _destination = destination;
        _skillsOnly = skillsOnly;
        _menusOnly = menusOnly;
        _referenceOnly = referenceOnly;
        if (skillsOnly) _screen = 4;
        if (referenceOnly) _screen = 2;
        _graphics = new GraphicsDeviceManager(this)
        { PreferredBackBufferWidth = width, PreferredBackBufferHeight = height, GraphicsProfile = GraphicsProfile.HiDef };
        Content.RootDirectory = Path.Combine(AppContext.BaseDirectory, "Content");
        Window.Title = "Fida UI verification";
        IsMouseVisible = true;
    }

    protected override void LoadContent()
    {
        // Preview never loads or writes the user's display settings or campaign save.
        DisplaySettingsManager.Current.Width = _width;
        DisplaySettingsManager.Current.Height = _height;
        DisplaySettingsManager.Current.ReduceMotion = false;
        _resources = new ResourceManager(GraphicsDevice);
        _resources.LoadContent(Content);
        _ui = new UIManager(GraphicsDevice, _resources);
        VerifyPageTransitions();
        Build();
    }

    private void Build()
    {
        _frame = 0;
        _firstDraw = double.NaN;
        _ui.Clear();
        switch (_screen)
        {
            case 0: _ui.BuildMainMenu(); break;
            case 1: _ui.BuildSettingsScreen(); break;
            case 2: _ui.BuildRulesScreen(1); break;
            case 3: _ui.BuildLevelSelect(); VerifyCampaignActions(); break;
            case 4:
                var selected = new HashSet<SkillType> { SkillType.Xue,SkillType.Shi };
                var progress = new CampaignProgress { UnlockedSkills = SkillFactory.SelectableSkills.Where(skill => skill!=SkillType.Shen).ToHashSet() };
                var node = new CampaignNode { Name="PALACE / 01",SkillBudget=8 };
                var loadout = new UICampaignSkillWheel(0,GraphicsDevice,_resources.Pixel,_resources.Font,
                    _resources.TitleFont,node,progress,selected,SkillFactory.SelectableSkills,null,_resources.CaptionFont,_resources.LabelFont);
                _ui.GetControls().Add(loadout);
                _loadout = loadout;
                _loadoutSelected = selected;
                loadout.GridBrowser.FocusSkill(SkillType.DoubleAttack);
                VerifySkillGrid(loadout,selected);
                break;
            case 9: _ui.BuildOnlineLobby(true); break;
            case 10: _ui.BuildMainMenu(true); break;
            case 11: _ui.BuildOnlineMenu(); VerifyRoomCodeInput(); break;
            case 12: _ui.BuildOnlineMenu(true); break;
            case 13: _ui.BuildHostingScreen("Waiting for opponent", "A7K2"); break;
            case 14: _ui.BuildConnectingScreen("Connecting to session..."); break;
            case 15: _ui.BuildPauseMenu(); break;
            case 16:
            case 17:
                var resultControls = _ui.GetControls();
                new UICampaignScreenBuilder(_resources, resultControls, new UIFocusManager(resultControls, new UIInputHandler(resultControls)))
                    .BuildResult(_screen == 16, _screen == 16 ? "The road ahead is open." : "Adjust your loadout and try again.", CampaignCatalog.Nodes.First(n => n.Id == "A1"));
                break;
            case 18:
                _ui.BuildDuelDraft();
                var draft = _ui.GetControls().OfType<UICampaignSkillWheel>().First();
                draft.UseUnifiedActionButton = true; draft.ShowSkillPoints = false;
                draft.HeaderText = "BAN"; draft.StatusText = "CHOOSE ONE SKILL";
                draft.AvailableActionText = "BAN"; draft.SelectedActionText = "BANNED";
                draft.StartActionText = "BAN"; draft.StartActionTogglesSelection = true;
                draft.AvailabilityOverride = _ => CampaignSkillAvailability.Available;
                break;
            case 19: _ui.BuildDuelResult(true, "Your opponent has been defeated."); break;
            case 8:
                _ui.GetControls().Add(new UIPanel(0, _resources.Pixel));
                int row = 0;
                foreach (UITextRole role in Enum.GetValues<UITextRole>())
                {
                    SpriteFont font = role == UITextRole.Name ? _resources.LabelFont :
                        role == UITextRole.Heading ? _resources.TitleFont :
                        role == UITextRole.Body ? _resources.Font : _resources.CaptionFont;
                    _ui.GetControls().Add(new UILabel(row + 1, $"{role}: Il1 / O0 / 8B / 5S — 0123456789",
                        UIDesktopLayout.MapPoint(new Vector2(960, 310 + row * 130), _width, _height), font)
                    { TypographyRole = role, MaximumWidth = _width * 0.65f });
                    row++;
                }
                break;
            default:
                int count = _screen == 6 ? 4 : 3;
                string[] names = { "ARITH", "MARA", "SENTINEL", "ALLY", "WITHERED KING", "WARDEN", "ECHO", "GUARD" };
                string[] keys = { "arithmetist", "mara", "sentinel", "sentinel", "withered_king", "warden", "echo", "warden" };
                var allies = Enumerable.Range(0, count).Select(i => new Player(i, names[i], 11, []) { PortraitKey = keys[i] }).ToList();
                var enemies = Enumerable.Range(0, count).Select(i => new Player(i + count, names[i + 4], 11, []) { PortraitKey = keys[i + 4] }).ToList();
                allies[0].Skills.AddRange(new[] { SkillType.DoubleAttack, SkillType.Shi, SkillType.Xue }.Select(SkillFactory.CreateSkill));
                enemies[0].Skills.Add(SkillFactory.CreateSkill(SkillType.PlusDefence));
                for (int i = 0; i < count; i++)
                {
                    allies[i].Numbers[0] = i == 0 ? 6 : i * 2;
                    allies[i].Numbers[1] = i == 0 ? 6 : i * 2 + 1;
                    enemies[i].Numbers[0] = i + 2;
                    enemies[i].Numbers[1] = i + 3;
                }
                _battle = new GameManager { LocalPlayerId = 0 };
                _battle.Initialize(allies.Concat(enemies).ToList(), [new Group(0, "ALLY", allies), new Group(1, "ENEMY", enemies)]);
                _ui.BuildPlayingUI(_battle);
                _battle.TransitionTo(new ChooseNumberIndexState(_battle, 0));
                _ui.UpdateGameMessage("Choose your number, then another number to add.");
                var player = _ui.GetUIPlayer(0);
                player.Number0Button.SetSelected(true);
                VerifyNumberInput(player);
                if (_screen == 7)
                    _ui.AddTutorialDialogue(new CampaignNode
                    {
                        BeforeStory = [new CampaignStoryPage { Speaker = "Mara Venn", Text =
                            "The road is quiet now. Take a moment to look at the numbers before you move. " +
                            "Choose one of your own numbers, then choose another number to add to it. " +
                            "You can use an ally's number as well as an enemy's. We have time to decide." }]
                    });
                break;
        }
    }

    private void VerifySkillGrid(UICampaignSkillWheel wrapper,HashSet<SkillType> selected)
    {
        var grid = wrapper.GridBrowser;
        MouseState State(Point p,ButtonState button) => new(p.X,p.Y,0,button,ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);
        void Click(Rectangle bounds)
        {
            wrapper.Update(State(bounds.Center,ButtonState.Released));
            wrapper.Update(State(bounds.Center,ButtonState.Pressed));
            wrapper.Update(State(bounds.Center,ButtonState.Released));
        }
        void NoToggle(string message) { if (wrapper.TryConsumeToggle(out _)) throw new Exception(message); }
        wrapper.Update(State(new Point(0,0),ButtonState.Released));
        Click(grid.Layout.Tile(1));
        if (wrapper.FocusedSkill!=SkillType.DoubleAttack) throw new Exception("Skill tile did not focus.");
        NoToggle("Browsing equipped a skill without confirmation.");
        Click(grid.Layout.Action);
        if (!wrapper.TryConsumeToggle(out var skill) || skill!=SkillType.DoubleAttack) throw new Exception("Equip action failed.");
        selected.Add(skill);
        NoToggle("Equip action was delivered twice.");
        Click(grid.Layout.Action);
        if (!wrapper.TryConsumeToggle(out skill) || skill!=SkillType.DoubleAttack) throw new Exception("Selected skill could not be removed.");
        selected.Remove(skill);
        grid.FocusSkill(SkillType.Long); Click(grid.Layout.Action); NoToggle("Over-budget skill was equipped.");
        grid.FocusSkill(SkillType.Shen); Click(grid.Layout.Action); NoToggle("Locked skill was equipped.");
        selected.Clear(); selected.Add(SkillType.DoubleHealth);
        grid.FocusSkill(SkillType.TripleHealth); Click(grid.Layout.Action); NoToggle("Conflicting skill was equipped.");
        selected.Clear(); selected.Add(SkillType.Xue); selected.Add(SkillType.Shi);
        var chip = grid.EquippedBounds.First(); Click(chip.Bounds);
        if (!wrapper.TryConsumeToggle(out skill) || skill!=chip.Skill) throw new Exception("Equipped chip removal failed.");
        wrapper.StartActionEnabled=false; Click(grid.Layout.Start);
        if (wrapper.ConsumeStartRequest()) throw new Exception("Disabled start fired.");
        wrapper.StartActionEnabled=true; Click(grid.Layout.Start);
        if (!wrapper.ConsumeStartRequest() || wrapper.ConsumeStartRequest()) throw new Exception("Start should fire once.");
        Click(grid.Layout.Tab(1));
        if (grid.Category!=SkillCategory.Offense) throw new Exception("Category click failed.");
        Click(grid.Layout.Tab(0)); Click(grid.Layout.Next);
        if (grid.CurrentPage!=1) throw new Exception("Second skill page is unreachable.");
        grid.ChangePage(-1); grid.FocusSkill(SkillType.DoubleAttack);
        wrapper.SelectionEnabled=false; Click(grid.Layout.Action); NoToggle("Locked selection accepted input.");
        wrapper.SelectionEnabled=true;
        wrapper.InputBlocked=true; Click(grid.Layout.Action); NoToggle("Blocked overlay accepted input.");
        wrapper.InputBlocked=false;
        wrapper.StartActionTogglesSelection=true; Click(grid.Layout.Start);
        if (!wrapper.TryConsumeToggle(out skill) || skill!=SkillType.DoubleAttack || wrapper.ConsumeStartRequest())
            throw new Exception("Duel ban action lost its selection semantics.");
        wrapper.StartActionTogglesSelection=false;
        // Cancel a drag between different tiles.
        wrapper.Update(State(grid.Layout.Tile(0).Center,ButtonState.Pressed));
        wrapper.Update(State(grid.Layout.Tile(2).Center,ButtonState.Released));
        if (grid.FocusedSkill!=SkillType.DoubleAttack) throw new Exception("Dragging activated another tile.");
        Console.WriteLine("Verified grid browsing, equip/remove, restrictions, paging and draft compatibility.");
    }

    private void VerifyNumberInput(UIPlayer player)
    {
        Rectangle original = player.Number0Bounds;
        Point p = original.Center;
        player.Number0Button.EnabledChecker = () => true;
        player.Number0Button.Update(new MouseState(p.X, p.Y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released));
        player.Number0Button.Update(new MouseState(p.X, p.Y, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released));
        player.Number0Button.Update(new MouseState(p.X, p.Y, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released));
        if (!player.Number0Button.IsClicked() || player.Number0Button.IsClicked()) throw new Exception("Number click must fire exactly once.");
        player.VisualOffset = new Vector2(20, -12);
        player.VisualScale = 1.06f;
        if (player.Number0Button.LayoutBounds != original) throw new Exception("Animation moved the number hitbox.");
        foreach (var unit in _ui.GetControls().OfType<UIPlayer>())
        {
            var name = unit.NameLabel.GetScreenBounds(GraphicsDevice.Viewport)!.Value;
            var health = unit.HealthLabel.GetScreenBounds(GraphicsDevice.Viewport)!.Value;
            if (name.Height < 22 || health.Height < 20) throw new Exception("Combat metadata fell below readable line-box sizes.");
            if (name.Intersects(health) || health.Intersects(unit.Number0Bounds) || health.Intersects(unit.Number1Bounds))
                throw new Exception("Readable text overlaps another combat control.");
        }
    }

    protected override void Update(GameTime time)
    {
        _ui.Update(time);
        _frame++;
        if (_screen is >= 5 and <= 7 && _frame == 20)
        {
            _ui.GetUIPlayer(0).VisualOffset = Vector2.Zero;
            _ui.GetUIPlayer(0).VisualScale = 1;
        }
        base.Update(time);
    }

    protected override void Draw(GameTime time)
    {
        if (double.IsNaN(_firstDraw)) _firstDraw = time.TotalGameTime.TotalSeconds;
        _ui.Draw(time);
        if (_frame >= 30 && time.TotalGameTime.TotalSeconds - _firstDraw >= 0.7)
        {
            string name = new[] { "menu", "settings", "rules", "campaign", "loadout", "battle-3v3", "battle-4v4", "dialogue", "type-specimen", "lobby", "menu-connected", "online", "connection", "hosting", "connecting", "paused", "victory", "defeat", "duel-draft", "duel-result" }[_screen];
            Save(name);
            if (_screen == 2)
            {
                for (int page = 0; page < UIManager.RulesPageCount; page++)
                {
                    _ui.BuildRulesScreen(page);
                    _ui.Update(new GameTime(time.TotalGameTime, TimeSpan.FromSeconds(1)));
                    _ui.Draw(time);
                    _ui.Draw(new GameTime(time.TotalGameTime + TimeSpan.FromSeconds(1), TimeSpan.Zero));
                    Save($"rules-{page + 1}");
                }
            }
            if (_screen == 3)
            {
                var map = (UICampaignMap)_ui.GetControls().Single(c => c.Name == "campaign_map");
                var bounds = new CampaignMapLayout(_width, _height).Map;
                ClickMap(map, new Point(bounds.Left + bounds.Width / 4, bounds.Center.Y));
                map.Update(new GameTime(time.TotalGameTime, TimeSpan.FromSeconds(1)));
                _ui.Draw(time); Save("campaign-route");
            }
            if (_screen == 9)
            {
                var art = _ui.GetControlByName<UIEditorialSurface>("lobby_art");
                art.LocalReady = true;
                _ui.GetControlByName<UIButton>("startPvP").Update("Waiting for opponent");
                _ui.GetControlByName<UIButton>("startPvP").Disable();
                _ui.Draw(time); Save("lobby-ready");
            }
            if (_screen==4) SaveSkillVariants(time);
            if (_screen is 5 or 6) VerifyRenderedMotion(time, name);
            _screen = _referenceOnly ? _screen switch { 2 => 11, 11 => 12, 12 => 13, 13 => 14, _ => 20 }
                : _menusOnly ? _screen switch { 0 => 1, 1 => 2, 2 => 3, 3 => 9, _ => _screen + 1 } : _screen + 1;
            if (_skillsOnly || _screen > 19) Exit(); else Build();
        }
        base.Draw(time);
    }

    private static MouseState MapMouse(Point p, ButtonState button) => new(p.X,p.Y,0,button,
        ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);

    private void VerifyPageTransitions()
    {
        using var ui = new UIManager(GraphicsDevice, _resources);
        GameTime At(double seconds, double step = .1) => new(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(step));
        Color[] Pixels() { var pixels = new Color[_width * _height]; GraphicsDevice.GetBackBufferData(pixels); return pixels; }
        ui.BuildMainMenu(); ui.Update(At(0)); ui.Draw(At(0)); ui.Update(At(1,1)); ui.Draw(At(1));
        var old = Pixels();
        ui.BuildRulesScreen();
        if (!ui.IsPageTransitionActive) throw new Exception("New page did not guard pending input.");
        ui.Update(At(2)); ui.Draw(At(2));
        var start = Pixels();
        if (!old.SequenceEqual(start)) throw new Exception("Page transition did not preserve the previous frame.");
        ui.Update(At(2.1)); ui.Draw(At(2.1)); var middle = Pixels();
        ui.Update(At(2.4,.3)); ui.Draw(At(2.4)); var end = Pixels();
        if (middle.SequenceEqual(old) || middle.SequenceEqual(end) || ui.IsPageTransitionActive)
            throw new Exception("Crossfade did not progress and settle.");
        DisplaySettingsManager.Current.ReduceMotion = true;
        ui.BuildSettingsScreen(); ui.Update(At(3)); ui.Draw(At(3)); var reduced = Pixels();
        ui.Update(At(4,1)); ui.Draw(At(4));
        if(ui.IsPageTransitionActive || !reduced.SequenceEqual(Pixels())) throw new Exception("Reduced motion did not settle immediately.");
        DisplaySettingsManager.Current.ReduceMotion = false;
        Console.WriteLine("Verified page crossfade, previous-frame retention, input guard and reduced motion.");
    }
    private void VerifyRoomCodeInput()
    {
        var input = _ui.GetTextBoxByName("roomCodeInput");
        var join = _ui.GetButtonByName("joinGame");
        var layout = new UIReferenceLayout(_width,_height);
        int clicked = 0, submitted = 0;
        void Clicked(UIButton _) => clicked++;
        void Submitted(string value) { if (value != "AB34") throw new Exception("Wrong submitted room code."); submitted++; }
        join.Clicked += Clicked; input.Submitted += Submitted;
        void ClickJoin()
        {
            join.Update(MapMouse(layout.JoinAction.Center,ButtonState.Released));
            join.Update(MapMouse(layout.JoinAction.Center,ButtonState.Pressed)); join.IsClicked();
            join.Update(MapMouse(layout.JoinAction.Center,ButtonState.Released)); join.IsClicked();
        }
        void Key(Keys key) { input.UpdateKeyboard(new KeyboardState(key)); input.UpdateKeyboard(new KeyboardState()); }
        ClickJoin();
        if (clicked != 0) throw new Exception("Incomplete code joined a room.");
        input.Update(MapMouse(layout.Code.Center,ButtonState.Pressed));
        Key(Keys.A); Key(Keys.B); Key(Keys.D3); Key(Keys.Enter);
        if (submitted != 0) throw new Exception("Partial code was submitted.");
        ClickJoin();
        if (clicked != 0) throw new Exception("Partial code enabled Join.");
        input.Update(MapMouse(layout.Code.Center,ButtonState.Pressed));
        Key(Keys.D4); Key(Keys.D5);
        if(input.Value != "AB34") throw new Exception("Code length or normalization failed.");
        Key(Keys.Enter);
        ClickJoin();
        if(clicked != 1 || submitted != 1) throw new Exception("Valid code did not dispatch exactly once.");
        input.Update(MapMouse(layout.Code.Center,ButtonState.Pressed));
        Key(Keys.Back); ClickJoin();
        if(clicked != 1) throw new Exception("Deleting a code character did not disable Join.");
        input.Clear(); input.UpdateKeyboard(new KeyboardState());
        join.Update(MapMouse(Point.Zero,ButtonState.Released));
        join.Clicked -= Clicked; input.Submitted -= Submitted;
        Console.WriteLine("Verified room-code entry, length limit, Enter submission and Join gating.");
    }
    private static void ClickMap(UICampaignMap map, Point p)
    {
        map.Update(MapMouse(p,ButtonState.Released));
        map.Update(MapMouse(p,ButtonState.Pressed));
        map.Update(MapMouse(p,ButtonState.Released));
    }
    private void VerifyCampaignActions()
    {
        foreach (bool locked in new[] { false, true })
        {
            var node = new CampaignNode { Id="fixture", Name="Fixture", MapX=0, MapY=0,
                Parents = locked ? new[] { "missing" } : Array.Empty<string>() };
            var map = new UICampaignMap(0,GraphicsDevice,_resources.Pixel,_resources.AntialiasedLine,
                _resources.MapFont,_resources.TitleFont,new[] { node },new CampaignProgress());
            var l = new CampaignMapLayout(_width,_height);
            ClickMap(map,l.Map.Center);
            if(map.TryConsumeSelectedNode(out _)) throw new Exception("Inspecting a node started the mission.");
            // Releasing on the action after a press in the map must not confirm.
            map.Update(MapMouse(l.Map.Center,ButtonState.Pressed));
            map.Update(MapMouse(l.Action.Center,ButtonState.Released));
            if(map.TryConsumeSelectedNode(out _)) throw new Exception("Cross-target drag confirmed a mission.");
            ClickMap(map,l.Action.Center);
            bool entered = map.TryConsumeSelectedNode(out string id);
            if (entered == locked || (entered && id != node.Id)) throw new Exception("Mission availability was not enforced.");
            if(map.TryConsumeSelectedNode(out _)) throw new Exception("Mission intent was consumed twice.");
            ClickMap(map,l.Back.Center);
            if(!map.ConsumeBackRequest() || map.ConsumeBackRequest()) throw new Exception("Map back action did not dispatch once.");
        }
        Console.WriteLine("Verified map inspect/confirm, locked missions, cross-target release and back.");
    }

    private void SaveSkillVariants(GameTime time)
    {
        var grid = _loadout.GridBrowser;
        void Render(string name) { _ui.Draw(time); Save(name); }
        grid.ChangePage(1); Render("loadout-page-2");
        grid.SetCategory(SkillCategory.Offense); Render("loadout-offense");
        grid.SetCategory(SkillCategory.All); grid.FocusSkill(SkillType.DoubleAttack);
        _loadoutSelected.Add(SkillType.DoubleAttack); Render("loadout-equipped");
        _loadoutSelected.Remove(SkillType.DoubleAttack);
        _loadout.SelectionEnabled=false; _loadout.StartActionEnabled=false; _loadout.StartActionText="WAIT";
        Render("loadout-ready");
        _loadout.SelectionEnabled=true; _loadout.StartActionEnabled=true; _loadout.StartActionText="START BATTLE";
        _loadoutSelected.Clear();
        _loadout.UseUnifiedActionButton=true; _loadout.StartActionTogglesSelection=true;
        _loadout.ShowSkillPoints=false; _loadout.HeaderText="BAN"; _loadout.StartActionText="BAN";
        _loadout.AvailabilityOverride=_=>CampaignSkillAvailability.Available;
        Render("loadout-ban");
        _loadout.AvailabilityOverride=skill=>skill==SkillType.DoubleAttack ? CampaignSkillAvailability.Selected : CampaignSkillAvailability.Locked;
        _loadout.SelectionEnabled=false; _loadout.StartActionEnabled=false; _loadout.StartActionText="WAIT";
        _loadout.SelectedActionText="BANNED"; _loadout.StartActionTogglesSelection=false;
        Render("loadout-ban-wait");
        _loadout.SelectionEnabled=true; _loadout.StartActionEnabled=true; _loadout.SelectedActionText="REMOVE SKILL";
        _loadout.UseUnifiedActionButton=false; _loadout.StartActionTogglesSelection=false;
        _loadout.ShowSkillPoints=true; _loadout.HeaderText="PALACE / 01"; _loadout.StartActionText="START BATTLE";
        _loadout.AvailabilityOverride=null;
        _loadoutSelected.Add(SkillType.Xue); _loadoutSelected.Add(SkillType.Shi);
    }

    private void VerifyRenderedMotion(GameTime time, string name)
    {
        var player = _ui.GetUIPlayer(0);
        var before = player.Number0Button.LayoutBounds;
        // Sample actual pixels: idle artwork must change while numeric targets stay still.
        var first = new Color[_width * _height];
        GraphicsDevice.GetBackBufferData(first);
        _ui.Draw(new GameTime(time.TotalGameTime + TimeSpan.FromSeconds(1), TimeSpan.Zero));
        var second = new Color[first.Length];
        GraphicsDevice.GetBackBufferData(second);
        int changed = 0;
        foreach (var point in Points(player.PortraitBounds))
            if (first[point.Y * _width + point.X] != second[point.Y * _width + point.X]) changed++;
        if (changed < 5) throw new Exception("Idle portrait did not visibly animate.");
        foreach (var point in Points(before))
            if (first[point.Y * _width + point.X] != second[point.Y * _width + point.X])
                throw new Exception("Idle animation changed a number button after the page reveal settled.");
        int targetId = _screen == 6 ? 4 : 3;
        _ui.PlayCardAction(0, targetId, true);
        var target = _ui.GetUIPlayer(targetId);
        target.Player.Health -= 5;
        target.UpdateHealth();
        var step = new GameTime(time.TotalGameTime + TimeSpan.FromSeconds(0.30), TimeSpan.FromSeconds(0.30));
        _ui.Update(step);
        _ui.Draw(step);
        Save(name + "-impact");
        if (player.Number0Button.LayoutBounds != before) throw new Exception("Rendered attack changed input geometry.");
        DisplaySettingsManager.Current.ReduceMotion = true;
        _ui.Update(new GameTime(step.TotalGameTime, TimeSpan.Zero));
        if (_ui.IsBlockingPresentationActive || player.VisualOffset != Vector2.Zero) throw new Exception("Reduced motion failed to settle.");
        DisplaySettingsManager.Current.ReduceMotion = false;
    }

    private static IEnumerable<Point> Points(Rectangle rectangle)
    {
        for (int y = rectangle.Top; y < rectangle.Bottom; y++)
        for (int x = rectangle.Left; x < rectangle.Right; x++) yield return new Point(x, y);
    }

    private void Save(string name)
    {
        var pixels = new Color[_width * _height];
        GraphicsDevice.GetBackBufferData(pixels);
        using var texture = new Texture2D(GraphicsDevice, _width, _height);
        texture.SetData(pixels);
        using var stream = File.Create(Path.Combine(_destination, $"{name}-{_width}x{_height}.png"));
        texture.SaveAsPng(stream, _width, _height);
        Console.WriteLine($"Rendered {name} at {_width}x{_height}");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) { _ui?.Dispose(); _resources?.Dispose(); }
        base.Dispose(disposing);
    }
}
