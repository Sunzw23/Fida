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
using var preview = new PreviewGame(width, height, destination, args.Contains("--skills-only"));
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
    private UICampaignSkillWheel _loadout;
    private HashSet<SkillType> _loadoutSelected;

    public PreviewGame(int width, int height, string destination, bool skillsOnly)
    {
        _width = width; _height = height; _destination = destination;
        _skillsOnly = skillsOnly;
        if (skillsOnly) _screen = 4;
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
            case 3: _ui.BuildLevelSelect(); break;
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
        if (_screen >= 5 && _frame == 20)
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
            string name = new[] { "menu", "settings", "rules", "campaign", "loadout", "battle-3v3", "battle-4v4", "dialogue", "type-specimen" }[_screen];
            Save(name);
            if (_screen==4) SaveSkillVariants(time);
            if (_screen is 5 or 6) VerifyRenderedMotion(time, name);
            _screen++;
            if (_skillsOnly || _screen > 8) Exit(); else Build();
        }
        base.Draw(time);
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
