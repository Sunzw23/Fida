using Fida;
using Fida.Campaign;
using Fida.Logic;
using Fida.UI;
using Fida.UI.Campaign;
using Fida.UI.Rules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

int width = args.Length > 0 ? int.Parse(args[0]) : 1600;
int height = args.Length > 1 ? int.Parse(args[1]) : 1000;
string destination = Path.GetFullPath(args.Length > 2 ? args[2] : "artifacts/cold-ui");
Directory.CreateDirectory(destination);
using var preview = new PreviewGame(width, height, destination, args.Contains("--skills-only"), args.Contains("--menus-only"), args.Contains("--reference-only"), args.Contains("--concept-only"), args.Contains("--route-only"));
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
    private readonly bool _conceptOnly;
    private readonly bool _routeOnly;
    private UICampaignSkillWheel _loadout;
    private HashSet<SkillType> _loadoutSelected;

    public PreviewGame(int width, int height, string destination, bool skillsOnly, bool menusOnly, bool referenceOnly, bool conceptOnly, bool routeOnly)
    {
        _width = width; _height = height; _destination = destination;
        _skillsOnly = skillsOnly;
        _menusOnly = menusOnly;
        _referenceOnly = referenceOnly;
        _conceptOnly = conceptOnly;
        _routeOnly = routeOnly;
        if (skillsOnly) _screen = 4;
        if (referenceOnly) _screen = 2;
        if (conceptOnly) _screen = 7;
        if (routeOnly) _screen = 3;
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
        VerifyGumModalLayouts();
        VerifyPageTransitions();
        VerifyCampaignLoadoutTransition();
        SeedRulesPreview();
        Build();
    }

    private void VerifyGumModalLayouts()
    {
        var viewport = new Rectangle(0, 0, _width, _height);
        foreach (string screen in new[] { "Pause", "Outcome", "Dialogue" })
        {
            Rectangle panel = UIGumLayout.Bounds(screen, "Panel", _width, _height);
            if (!viewport.Contains(panel)) throw new Exception($"Gum {screen} panel leaves the viewport.");
        }
        var pause = UIGumLayout.Bounds("Pause", "Panel", _width, _height);
        Rectangle? previous = null;
        foreach (string action in new[] { "Resume", "Settings", "Rules", "MainMenu" })
        {
            Rectangle button = UIGumLayout.Bounds("Pause", action, _width, _height);
            if (!pause.Contains(button) || previous?.Intersects(button) == true)
                throw new Exception("Gum pause actions overlap or leave their panel.");
            previous = button;
        }
        Rectangle primary = UIGumLayout.Bounds("Outcome", "PrimaryAction", _width, _height);
        Rectangle secondary = UIGumLayout.Bounds("Outcome", "SecondaryAction", _width, _height);
        if (primary.Intersects(secondary) || !viewport.Contains(primary) || !viewport.Contains(secondary))
            throw new Exception("Gum result actions overlap or leave the viewport.");
        Rectangle body = UIGumLayout.Bounds("Dialogue", "Body", _width, _height);
        Rectangle next = UIGumLayout.Bounds("Dialogue", "Next", _width, _height);
        if (body.Intersects(next) || !viewport.Contains(body) || !viewport.Contains(next))
            throw new Exception("Gum dialogue body collides with its action.");
        Console.WriteLine("Verified Gum modal bounds, parenting and separated action regions.");
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
            case 2: _ui.BuildRulesScreen(); break;
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
            case 15:
                if(_battle == null)
                {
                    var team = new List<Player> { new(0,"ARITH",11,[]){ PortraitKey="arith" },new(1,"MARA",11,[]){PortraitKey="mara"},new(2,"SENTINEL",11,[]){PortraitKey="sentinel"} };
                    var opponents = new List<Player> { new(3,"WITHERED KING",11,[]){PortraitKey="withered_king"},new(4,"WARDEN",11,[]){PortraitKey="warden"},new(5,"ECHO",11,[]){PortraitKey="echo"} };
                    _battle=new GameManager { LocalPlayerId=0 };
                    _battle.Initialize(team.Concat(opponents).ToList(),[new Group(0,"ALLY",team),new Group(1,"ENEMY",opponents)]);
                }
                _ui.BuildPlayingUI(_battle);
                _ui.Update(new GameTime(TimeSpan.FromSeconds(2000),TimeSpan.FromSeconds(1)));
                _ui.Draw(new GameTime(TimeSpan.FromSeconds(2000),TimeSpan.Zero));
                _ui.Draw(new GameTime(TimeSpan.FromSeconds(2001),TimeSpan.Zero));
                _ui.BuildPauseMenu();
                if(_ui.GetControlByName<UIPaperPause>("pause_art").Backdrop==null) throw new Exception("Pause lost its battle backdrop.");
                break;
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
                        BeforeStory = _conceptOnly
                            ? [new CampaignStoryPage { Speaker = "Mara", Text = "Look at the numbers before you move.\nChoose one of your own, then another number to add." },
                               new CampaignStoryPage { Speaker = "Mara", Text = "Choose a number to begin." },
                               new CampaignStoryPage { Speaker = "Mara", Text = "Then choose another number to add." }]
                            : [new CampaignStoryPage { Speaker = "Mara Venn", Text =
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
        if (grid.FocusSkill(SkillType.Shen)) throw new Exception("An unowned skill was reachable in the campaign grid.");
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
            if (_conceptOnly) Console.WriteLine($"UI render-target samples: {_ui.RenderSampleCount}");
            string name = new[] { "menu", "settings", "rules", "campaign", "loadout", "battle-3v3", "battle-4v4", "dialogue", "type-specimen", "lobby", "menu-connected", "online", "connection", "hosting", "connecting", "paused", "victory", "defeat", "duel-draft", "duel-result" }[_screen];
            Save(name);
            if (_screen == 16) SaveButtonStates(time);
            if (_screen == 2) SaveRulesPages(time);

            if (_screen == 3)
            {
                var map = (UICampaignMap)_ui.GetControls().Single(c => c.Name == "campaign_map");
                var bounds = new CampaignMapLayout(_width, _height).Map;
                ClickMap(map, new Point(bounds.Left + bounds.Width / 4, bounds.Center.Y));
                map.Update(new GameTime(time.TotalGameTime, TimeSpan.FromSeconds(1)));
                if (_routeOnly) VerifyReferenceRoute(map, bounds);
                _ui.Draw(time); _ui.Draw(new GameTime(time.TotalGameTime + TimeSpan.FromSeconds(1), TimeSpan.Zero)); Save("campaign-route");
                VerifyLockedChapterContrast();
                VerifyLockedMissionAction();
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
            _screen = _conceptOnly ? _screen switch { 7 => 15, 15 => 16, 16 => 17, _ => 20 }
                : _referenceOnly ? _screen switch { 2 => 11, 11 => 12, 12 => 13, 13 => 14, _ => 20 }
                : _menusOnly ? _screen switch { 0 => 1, 1 => 2, 2 => 3, 3 => 9, _ => _screen + 1 } : _screen + 1;
            if (_skillsOnly || _routeOnly || _screen > 19) Exit(); else Build();
        }
        base.Draw(time);
    }

    private static MouseState MapMouse(Point p, ButtonState button) => new(p.X,p.Y,0,button,
        ButtonState.Released,ButtonState.Released,ButtonState.Released,ButtonState.Released);

    private static readonly SkillType[] PreviewOwned =
    {
        SkillType.PlusAttack, SkillType.DoubleAttack, SkillType.PlusDefence,
        SkillType.DoubleDefence, SkillType.DoubleHealth, SkillType.Xue
    };

    private static readonly SkillType[] PreviewSeen =
    {
        SkillType.Long, SkillType.Bian, SkillType.Shi, SkillType.Tong,
        SkillType.Mo, SkillType.Di, SkillType.Shen, SkillType.Xian
    };

    /// <summary>Deterministic discovery for screenshots; never reads or writes the player's saves.</summary>
    private void SeedRulesPreview()
    {
        string directory = Path.Combine(Path.GetTempPath(), "fida-ui-preview-" + Environment.ProcessId);
        Directory.CreateDirectory(directory);
        CampaignRuntime.UseStore(CampaignRuntime.Local,
            new CampaignProgressStore(Path.Combine(directory, "local.json")));
        CampaignRuntime.UseStore(CampaignRuntime.Online,
            new CampaignProgressStore(Path.Combine(directory, "online.json")));
        foreach (var skill in PreviewOwned) CampaignRuntime.Local.Progress.UnlockedSkills.Add(skill);

        SkillCodex.UseStore(null);
        SkillCodex.RecordSkills(PreviewOwned.Concat(PreviewSeen).Distinct(), "duel");
        foreach (var skill in PreviewOwned) SkillCodex.MarkRead(skill);
        SkillCodex.MarkRead(SkillType.Long);
    }

    private void SaveRulesPages(GameTime time)
    {
        var book = RulesBook.Current();
        var shots = new List<(string Name, int Page, int Index)>
        {
            ("rules-1", 0, 0), ("rules-2", 1, 0), ("rules-3", 2, 0),
            ("rules-4", RulesBook.IndexPage, 0)
        };
        if (book.Entries.Count > 0) shots.Add(("rules-5", RulesBook.PageForIndex(0), 0));
        if (book.Entries.Count > 1) shots.Add(("rules-6", RulesBook.PageForIndex(book.Entries.Count - 1), 0));
        if (book.Entries.Count > 12) shots.Add(("rules-index-2", RulesBook.IndexPage, 1));

        foreach (var (name, page, index) in shots)
        {
            _ui.BuildRulesScreen(page, index);
            _ui.Update(new GameTime(time.TotalGameTime, TimeSpan.FromSeconds(1)));
            _ui.Draw(time);
            _ui.Draw(new GameTime(time.TotalGameTime + TimeSpan.FromSeconds(1), TimeSpan.Zero));
            Save(name);
        }

        VerifyRulesStructure(book);

        // Empty discovery is a real state for a fresh profile; keep its copy honest.
        CampaignRuntime.UseStore(CampaignRuntime.Local,
            new CampaignProgressStore(Path.Combine(Path.GetTempPath(), "fida-ui-empty-" + Environment.ProcessId + ".json")));
        CampaignRuntime.UseStore(CampaignRuntime.Online,
            new CampaignProgressStore(Path.Combine(Path.GetTempPath(), "fida-ui-empty-online-" + Environment.ProcessId + ".json")));
        SkillCodex.UseStore(null);
        _ui.BuildRulesScreen(RulesBook.IndexPage);
        _ui.Update(new GameTime(time.TotalGameTime, TimeSpan.FromSeconds(1)));
        _ui.Draw(time); _ui.Draw(new GameTime(time.TotalGameTime + TimeSpan.FromSeconds(1), TimeSpan.Zero));
        Save("rules-index-empty");
        SeedRulesPreview();
    }

    private void VerifyRulesStructure(RulesBook book)
    {
        int total = _ui.RulesPageCount;
        if (total != RulesBook.IndexPage + 1 + book.Entries.Count)
            throw new Exception("Rules page count does not match discovered skills.");
        var owned = book.Entries.TakeWhile(book.Owns).Count();
        if (book.Entries.Skip(owned).Any(book.Owns))
            throw new Exception("Owned skills must come before only-seen skills.");
        if (book.SkillAtPage(RulesBook.PageForIndex(0)) != book.Entries.FirstOrDefault())
            throw new Exception("Skill page mapping is broken.");

        _ui.BuildRulesScreen(RulesBook.IndexPage);
        int perPage = new UIRulesLayout(_width, _height).IndexColumns * new UIRulesLayout(_width, _height).IndexRows;
        int tiles = _ui.GetControls().OfType<UISkillCodexTile>().Count();
        if (tiles != Math.Min(perPage, book.Entries.Count))
            throw new Exception($"Index page built {tiles} tiles, expected {Math.Min(perPage, book.Entries.Count)}.");
        if (_ui.GetButtonByName("rulesTile0") == null)
            throw new Exception("Index tiles are not addressable by page binding.");
        foreach (int reference in new[] { 0, 1, 2 })
        {
            _ui.BuildRulesScreen(reference);
            for (int chip = 0; chip < RulesBook.Chapters.Length; chip++)
                if (_ui.GetButtonByName($"rulesChapter{chip}") == null)
                    throw new Exception($"Rules page {reference} is missing chapter chip {chip}.");
            if (_ui.GetButtonByName("prevPage") == null || _ui.GetButtonByName("nextPage") == null)
                throw new Exception($"Rules page {reference} is missing page navigation.");
        }
        _ui.BuildRulesScreen(RulesBook.IndexPage);

        _ui.BuildRulesScreen(RulesBook.TimingPage);
        if (!_ui.GetControls().OfType<UIRulesTimingLink>().Any())
            throw new Exception("Timing page has no skill links for discovered skills.");
        if (!_ui.GetControls().OfType<UIRulesTimingLink>().Any(link =>
                link.Name.StartsWith("rulesLink") &&
                int.TryParse(link.Name["rulesLink".Length..], out int entry) &&
                entry >= 0 && entry < book.Entries.Count))
            throw new Exception("Timing links are not addressable by page binding.");

        _ui.BuildRulesScreen(RulesBook.PageForIndex(0));
        if (_ui.GetControlByName<UISkillCodexPage>("rules_skill") == null)
            throw new Exception("Skill pages do not build their own surface.");

        Console.WriteLine($"Verified {total} rules pages for {book.Entries.Count} discovered skills.");
    }

    private void SaveButtonStates(GameTime time)
    {        if (_width < _height) return;
        GraphicsDevice.Clear(UITheme.BgTop);
        using var batch = new SpriteBatch(GraphicsDevice);
        batch.Begin(samplerState: SamplerState.LinearClamp);
        string[] states = { "Normal", "Hover", "Focus", "Pressed", "Disabled" };
        string[] types = { "Button", "Paper", "Primary", "Active", "Back", "Number" };
        float sx = _width / 1280f, sy = _height / 720f;
        void Label(string text, float x, float y) => UITextRenderer.DrawString(batch, _resources.CaptionFont,
            text, new Vector2(x * sx, y * sy), UITheme.Ink, scale: .5f * sx);
        for (int col = 0; col < states.Length; col++) Label(states[col], 200 + col * 210, 40);
        for (int row = 0; row < types.Length; row++)
        {
            Label(types[row], 28, 108 + row * 98);
            for (int col = 0; col < states.Length; col++)
            {
                var bounds = new Rectangle((int)((180 + col * 210) * sx), (int)((80 + row * 98) * sy),
                    (int)(180 * sx), (int)(68 * sy));
                UIControl control = row switch
                {
                    0 => new UIButton(0, bounds, "Action", _resources.Pixel, _resources.LabelFont),
                    4 => new UIIconButton(0, bounds, UIIconButton.IconKind.Back, _resources.Pixel, _resources.LabelFont),
                    5 => new UIAnimatedButton(0, bounds, "6", _resources.Pixel, _resources.LabelFont),
                    _ => new UIEditorialButton(0, bounds, "Action", _resources.Pixel, _resources.LabelFont)
                        { Primary = row == 2, Active = row == 3 }
                };
                void SetState<T>(UIButtonBase<T> button) where T : struct
                {
                    var away = new Point(-100, -100);
                    button.Update(MapMouse(away, ButtonState.Released));
                    if (col == 2) button.SetFocus(true);
                    button.Update(MapMouse(col is 1 or 3 or 4 ? bounds.Center : away, ButtonState.Released));
                    if (col == 3) button.Update(MapMouse(bounds.Center, ButtonState.Pressed));
                    if (col == 4) button.Disable();
                    button.Update(new GameTime(time.TotalGameTime, TimeSpan.FromSeconds(1)));
                }
                if (control is UIButton b) SetState(b);
                else if (control is UIAnimatedButton a) SetState(a);
                control.Draw(batch, time);
            }
        }
        batch.End();
        Save("button-states");
    }

    private void VerifyCampaignLoadoutTransition()
    {
        using var ui = new UIManager(GraphicsDevice, _resources);
        var time = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(.016));
        ui.BuildLevelSelect(); ui.Update(time); ui.Draw(time);
        ui.BuildSkillSelect();
        // State input can build the next page after UI.Update, before the same frame's Draw.
        ui.Draw(time);
        ui.Update(time); ui.Draw(time);
        ui.Clear();
        var scene = new Fida.Scene.PlayingSceneManager(ui.GameManager, ui, null);
        scene.InitializeCampaign(GameSetup.LoadCampaignBattle());
        scene.StartGame();
        ui.Draw(time);
        ui.Update(time); ui.Draw(time);
        Console.WriteLine("Verified campaign map-to-loadout first draw and story overlay.");
    }

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
    private void VerifyReferenceRoute(UICampaignMap map, Rectangle bounds)
    {
        var layout = new CampaignMapLayout(_width, _height);
        if (!new Rectangle(0,0,_width,_height).Contains(layout.Details) || bounds.Intersects(layout.Details))
            throw new Exception("Route map and details overlap or leave the viewport.");
        foreach (var (id, point) in new[] {
            ("A2",new Point(bounds.Left+bounds.Width/4,bounds.Center.Y)),
            ("A3",new Point(bounds.Left+bounds.Width*3/4,bounds.Center.Y)),
            ("AG",new Point(bounds.Center.X,bounds.Bottom-(int)(90*layout.Scale))),
            ("A1",new Point(bounds.Center.X,bounds.Top+(int)(90*layout.Scale))) })
        {
            ClickMap(map,point);
            if(map.SelectedNodeId!=id)throw new Exception($"Reference route click selected {map.SelectedNodeId}, expected {id}.");
            if(map.TryConsumeSelectedNode(out _))throw new Exception("Inspecting a reference card started a mission.");
        }
        map.ZoomIn(); map.FitAll();
        ClickMap(map,new Point(bounds.Center.X,bounds.Top+(int)(90*layout.Scale)));
        if(map.SelectedNodeId!="A1")throw new Exception("Fit lost the route's visual/hit geometry alignment.");
        Console.WriteLine("Verified reference diamond card selection, isolated inspect intents, viewport bounds and zoom/fit alignment.");
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

    /// <summary>
    /// A locked chapter has to be readable as unavailable from its sheet alone: the
    /// top-level page shares the mission card's grey stock, not just a darkened image.
    /// </summary>
    private void VerifyLockedChapterContrast()
    {
        var view = new UICampaignRouteView(_resources.Font, _resources.TitleFont,
            _resources.Pixel, _resources.AntialiasedLine);
        var open = new Rectangle(20, 20, 460, 200);
        var locked = open; locked.Offset(500, 0);
        // Render both sheets into one texture so a single readback holds the comparison.
        using var target = new RenderTarget2D(GraphicsDevice, 1000, 240);
        GraphicsDevice.SetRenderTarget(target);
        GraphicsDevice.Clear(UITheme.BgTop);
        using (var batch = new SpriteBatch(GraphicsDevice))
        {
            batch.Begin(samplerState: SamplerState.LinearClamp);
            view.DrawChapterCard(batch, open, 3, "Chapter I", 0, 4, false, false, 0, 0);
            view.DrawChapterCard(batch, locked, 4, "Chapter II", 0, 4, false, true, 0, 0);
            batch.End();
        }
        GraphicsDevice.SetRenderTarget(null);
        var pixels = new Color[1000 * 240];
        target.GetData(pixels);
        // The band above the chapter artwork is unprinted sheet, so its average is the stock colour.
        Color Stock(Rectangle bounds)
        {
            int red = 0, green = 0, blue = 0, count = 0;
            for (int y = bounds.Top + 3; y < bounds.Top + 7; y++)
            for (int x = bounds.Left + 70; x < bounds.Right - 70; x++)
            {
                Color pixel = pixels[y * 1000 + x];
                red += pixel.R; green += pixel.G; blue += pixel.B; count++;
            }
            return new Color(red / count, green / count, blue / count);
        }
        Color openStock = Stock(open), lockedStock = Stock(locked);
        int Luma(Color color) => (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
        if (Luma(openStock) - Luma(lockedStock) < 40 || Math.Abs(openStock.R - lockedStock.R) < 30)
            throw new Exception($"A locked chapter kept the open sheet's colour: " +
                $"{openStock} vs {lockedStock}.");
        Console.WriteLine($"Verified locked chapters print on grey stock ({openStock} vs {lockedStock}).");
    }

    /// <summary>
    /// A closed mission can still be inspected, but its detail action must not offer the
    /// loadout: the ticket greys out to the locked stock and reads "Locked".
    /// </summary>
    private void VerifyLockedMissionAction()
    {
        var view = new UICampaignRouteView(_resources.Font, _resources.TitleFont,
            _resources.Pixel, _resources.AntialiasedLine);
        var layout = new CampaignMapLayout(1600, 1000);
        Color Action(bool locked, string name)
        {
            using var target = new RenderTarget2D(GraphicsDevice, 1600, 1000);
            GraphicsDevice.SetRenderTarget(target);
            GraphicsDevice.Clear(UITheme.BgTop);
            using (var batch = new SpriteBatch(GraphicsDevice))
            {
                batch.Begin(samplerState: SamplerState.LinearClamp);
                view.DrawDetails(batch, layout, new CampaignRouteDetails("A2", "White Stone Gate",
                    "A guard has not slept since the road closed.", 3, 3, "Story Fragment", null,
                    "Prologue", locked), 0, 0);
                batch.End();
            }
            GraphicsDevice.SetRenderTarget(null);
            using (var stream = File.Create(Path.Combine(_destination, $"{name}.png")))
                target.SaveAsPng(stream, 1600, 1000);
            var pixels = new Color[1600 * 1000];
            target.GetData(pixels);
            // Sample the unprinted top band of the action ticket, clear of its label.
            int red = 0, green = 0, blue = 0, count = 0;
            Rectangle action = layout.Action;
            for (int y = action.Top + 10; y < action.Top + 16; y++)
            for (int x = action.Left + 80; x < action.Right - 80; x++)
            {
                Color pixel = pixels[y * 1600 + x];
                red += pixel.R; green += pixel.G; blue += pixel.B; count++;
            }
            return new Color(red / count, green / count, blue / count);
        }
        Color openAction = Action(false, "campaign-details-open");
        Color lockedAction = Action(true, "campaign-details-locked");
        int Luma(Color color) => (color.R * 299 + color.G * 587 + color.B * 114) / 1000;
        if (Luma(openAction) - Luma(lockedAction) < 25)
            throw new Exception($"A locked mission kept the open action's ticket: " +
                $"{openAction} vs {lockedAction}.");
        Console.WriteLine($"Verified locked detail action greys out ({openAction} vs {lockedAction}).");
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
        var saved = _ui.GetControls().ToArray();
        _ui.GetControls().Clear();
        var owned = new HashSet<SkillType> { SkillType.PlusAttack, SkillType.PlusDefence, SkillType.Xue, SkillType.Shi };
        var inventory = new UICampaignSkillGrid(0, GraphicsDevice, _resources.Pixel, _resources.Font,
            _resources.TitleFont, new CampaignNode { Name = "Rustblade Arsenal", SkillBudget = 8 },
            new CampaignProgress { UnlockedSkills = owned }, new HashSet<SkillType>(), SkillFactory.SelectableSkills,
            _resources.CaptionFont, _resources.LabelFont);
        _ui.GetControls().Add(inventory);
        if (inventory.VisibleSkills.Any(skill => !owned.Contains(skill)) || inventory.FocusSkill(SkillType.DoubleAttack))
            throw new Exception("An unowned skill appeared in the campaign inventory.");
        _ui.Draw(time);
        _ui.Draw(new GameTime(time.TotalGameTime + TimeSpan.FromSeconds(1), TimeSpan.Zero)); Save("loadout-owned");
        inventory.SetCategory(SkillCategory.Control);
        _ui.Draw(new GameTime(time.TotalGameTime + TimeSpan.FromSeconds(1), TimeSpan.Zero)); Save("loadout-owned-control");
        _ui.GetControls().Clear(); _ui.GetControls().AddRange(saved);
        Console.WriteLine("Verified campaign inventory hides unowned skills without changing the duel pool.");
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
