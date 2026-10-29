using Fida;
using Fida.Campaign;
using Fida.Logic;
using Fida.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

int width = args.Length > 0 ? int.Parse(args[0]) : 1600;
int height = args.Length > 1 ? int.Parse(args[1]) : 1000;
string destination = Path.GetFullPath(args.Length > 2 ? args[2] : "artifacts/cold-ui");
Directory.CreateDirectory(destination);
using var preview = new PreviewGame(width, height, destination);
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

    public PreviewGame(int width, int height, string destination)
    {
        _width = width; _height = height; _destination = destination;
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
            case 4: _ui.BuildDuelDraft(); break;
            default:
                int count = _screen == 6 ? 4 : 3;
                string[] names = { "ARITH", "MARA", "SENTINEL", "ALLY", "WITHERED KING", "WARDEN", "ECHO", "GUARD" };
                string[] keys = { "arithmetist", "mara", "sentinel", "sentinel", "withered_king", "warden", "echo", "warden" };
                var allies = Enumerable.Range(0, count).Select(i => new Player(i, names[i], 11, []) { PortraitKey = keys[i] }).ToList();
                var enemies = Enumerable.Range(0, count).Select(i => new Player(i + count, names[i + 4], 11, []) { PortraitKey = keys[i + 4] }).ToList();
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
                break;
        }
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
            string name = new[] { "menu", "settings", "rules", "campaign", "loadout", "battle-3v3", "battle-4v4" }[_screen];
            Save(name);
            if (_screen >= 5) VerifyRenderedMotion(time, name);
            _screen++;
            if (_screen > 6) Exit(); else Build();
        }
        base.Draw(time);
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
