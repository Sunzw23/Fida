#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI.Campaign;

/// <summary>Grid presentation and input; reports intent through the existing loadout/draft contract.</summary>
public sealed partial class UICampaignSkillGrid : UIControl
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font, _titleFont, _captionFont, _labelFont;
    private readonly CampaignNode _node;
    private readonly CampaignProgress _progress;
    private IReadOnlyCollection<SkillType> _selected;
    private readonly SkillGridModel _grid;
    private KeyboardState _previousKeyboard;
    private GamePadState _previousGamePad;
    private MouseState _previousMouse;
    private bool _inputInitialized;
    private string _pressedTarget = "";
    private SkillType? _pendingToggle;
    private bool _backRequested, _startRequested;
    private int _chipOffset;
    private readonly Dictionary<Rectangle, (float Hover, float Press, bool Enabled)> _actionMotion = new();
    public SkillType FocusedSkill => _grid.Focused;
    public int FocusedIndex => _grid.FocusedIndex;
    public string HeaderText { get; set; }
    public string StatusText { get; set; } = "";
    public string AvailableActionText { get; set; } = "EQUIP SKILL";
    public string SelectedActionText { get; set; } = "REMOVE SKILL";
    public string StartActionText { get; set; } = "START BATTLE";
    public bool StartActionEnabled { get; set; } = true;
    public bool SelectionEnabled { get; set; } = true;
    public bool InputBlocked { get; set; }
    public bool StartActionTogglesSelection { get; set; }
    public bool UseUnifiedActionButton { get; set; }
    public bool ShowSkillPoints { get; set; } = true;
    public Func<SkillType, CampaignSkillAvailability> AvailabilityOverride { get; set; } = null!;
    public Func<SkillType, string> AvailabilityLabelOverride { get; set; } = null!;
    public SkillSelectionLayout Layout => new(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);
    public IReadOnlyList<SkillType> VisibleSkills { get { SyncLayout(); return _grid.PageItems; } }
    public SkillCategory Category => _grid.Category;
    public int CurrentPage => _grid.Page;
    public int PageCount => _grid.PageCount;
    public IReadOnlyList<(SkillType Skill, Rectangle Bounds)> EquippedBounds =>
        EquippedChips(SyncLayout()).Select(chip => (chip.Skill,chip.Bounds)).ToArray();
    private SkillSelectionLayout SyncLayout()
    { var layout = Layout; _grid.SetPageSize(layout.PageSize); return layout; }

    public UICampaignSkillGrid(int id, GraphicsDevice device, Texture2D pixel, SpriteFont font,
        SpriteFont titleFont, CampaignNode node, CampaignProgress progress,
        IReadOnlyCollection<SkillType> selected, IReadOnlyList<SkillType> skills,
        SpriteFont? captionFont = null, SpriteFont? labelFont = null) : base(id)
    {
        _graphicsDevice = device; _pixel = pixel; _font = font; _titleFont = titleFont;
        _captionFont = captionFont ?? font; _labelFont = labelFont ?? font;
        _node = node; _progress = progress; _selected = selected ?? Array.Empty<SkillType>(); HeaderText = node.Name;
        SkillType? initial = _selected.Cast<SkillType?>().FirstOrDefault() ?? skills
            .Where(s => CampaignLoadout.Evaluate(progress, node, _selected, s) == CampaignSkillAvailability.Available)
            .Cast<SkillType?>().FirstOrDefault();
        _grid = SkillGridModel.ForUnlocked(skills, progress.UnlockedSkills, initial);
    }
    public void SetSelection(IReadOnlyCollection<SkillType> selected) => _selected = selected ?? Array.Empty<SkillType>();
    public void Move(int steps) { SyncLayout(); _grid.Move(steps); }
    public bool FocusSkill(SkillType skill) => _grid.Focus(skill);
    public void SetCategory(SkillCategory category) { SyncLayout(); _grid.Filter(category); }
    public void ChangePage(int delta) { SyncLayout(); _grid.ChangePage(delta); }
    public bool TryConsumeToggle(out SkillType skill)
    { skill = _pendingToggle.GetValueOrDefault(); bool value = _pendingToggle.HasValue; _pendingToggle = null; return value; }
    public bool ConsumeBackRequest() { bool value = _backRequested; _backRequested = false; return value; }
    public bool ConsumeStartRequest() { bool value = _startRequested; _startRequested = false; return value; }
    private CampaignSkillAvailability Availability(SkillType skill) => AvailabilityOverride?.Invoke(skill) ??
        CampaignLoadout.Evaluate(_progress, _node, _selected, skill);
    private bool CanToggle(SkillType skill) => SelectionEnabled && !InputBlocked &&
        Availability(skill) is CampaignSkillAvailability.Available or CampaignSkillAvailability.Selected;
    private void Toggle(SkillType skill) { if (_grid.HasFocus && _grid.All.Contains(skill) && CanToggle(skill)) _pendingToggle = skill; }
    private void Start()
    {
        if (!StartActionEnabled || InputBlocked) return;
        if (StartActionTogglesSelection) Toggle(FocusedSkill); else _startRequested = true;
    }
    public override void Update(GameTime gameTime)
    {
        foreach (var bounds in _actionMotion.Keys.ToArray())
        {
            var state = _actionMotion[bounds];
            bool hover = state.Enabled && !InputBlocked && bounds.Contains(_previousMouse.Position);
            bool press = hover && _previousMouse.LeftButton == ButtonState.Pressed &&
                _pressedTarget.Length > 0 && _pressedTarget == HitTarget(_previousMouse.Position);
            _actionMotion[bounds] = (UIMotion.Approach(state.Hover, hover ? 1 : 0, gameTime.ElapsedGameTime.TotalSeconds),
                UIMotion.Approach(state.Press, press ? 1 : 0, gameTime.ElapsedGameTime.TotalSeconds, 24), state.Enabled);
        }
        var keyboard = Keyboard.GetState(); var pad = GamePad.GetState(PlayerIndex.One); var layout = SyncLayout();
        if (_inputInitialized && !InputBlocked)
        {
            int dx = Pressed(keyboard, Keys.Left) || Pressed(pad, Buttons.DPadLeft) ? -1 :
                Pressed(keyboard, Keys.Right) || Pressed(pad, Buttons.DPadRight) ? 1 : 0;
            int dy = Pressed(keyboard, Keys.Up) || Pressed(pad, Buttons.DPadUp) ? -1 :
                Pressed(keyboard, Keys.Down) || Pressed(pad, Buttons.DPadDown) ? 1 : 0;
            if (pad.IsConnected)
            {
                if (pad.ThumbSticks.Left.X > .55f && _previousGamePad.ThumbSticks.Left.X <= .55f) dx = 1;
                if (pad.ThumbSticks.Left.X < -.55f && _previousGamePad.ThumbSticks.Left.X >= -.55f) dx = -1;
                if (pad.ThumbSticks.Left.Y > .55f && _previousGamePad.ThumbSticks.Left.Y <= .55f) dy = -1;
                if (pad.ThumbSticks.Left.Y < -.55f && _previousGamePad.ThumbSticks.Left.Y >= -.55f) dy = 1;
            }
            _grid.MoveSpatial(dx, dy, layout.Columns);
            if (Pressed(keyboard, Keys.Tab) || Pressed(keyboard, Keys.E) || Pressed(pad, Buttons.RightShoulder)) CycleCategory(1);
            if (Pressed(keyboard, Keys.Q) || Pressed(pad, Buttons.LeftShoulder)) CycleCategory(-1);
            if (Pressed(keyboard, Keys.PageUp)) ChangePage(-1);
            if (Pressed(keyboard, Keys.PageDown)) ChangePage(1);
            if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space) || Pressed(pad, Buttons.A)) Toggle(FocusedSkill);
            if (Pressed(keyboard, Keys.Escape) || Pressed(pad, Buttons.B) || Pressed(pad, Buttons.Back)) _backRequested = true;
            if (Pressed(keyboard, Keys.F5) || Pressed(pad, Buttons.Start)) Start();
        }
        _previousKeyboard = keyboard; _previousGamePad = pad;
    }
    public override void Update(MouseState mouse)
    {
        if (!_inputInitialized || InputBlocked)
        {
            _previousMouse = mouse; _previousKeyboard = Keyboard.GetState();
            _previousGamePad = GamePad.GetState(PlayerIndex.One); _inputInitialized = true; _pressedTarget = ""; return;
        }
        var layout = SyncLayout(); int wheel = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
        if (wheel != 0 && layout.Grid.Contains(mouse.Position)) ChangePage(wheel > 0 ? -1 : 1);
        if (mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released)
            _pressedTarget = HitTarget(mouse.Position);
        if (mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed)
        {
            string target = HitTarget(mouse.Position);
            if (target.Length > 0 && target == _pressedTarget) Activate(target);
            _pressedTarget = "";
        }
        _previousMouse = mouse;
    }
    private void CycleCategory(int delta) => SetCategory((SkillCategory)(((int)Category + delta + 4) % 4));
    private bool Pressed(KeyboardState current, Keys key) => current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
    private bool Pressed(GamePadState current, Buttons button) => current.IsConnected && current.IsButtonDown(button) && !_previousGamePad.IsButtonDown(button);
    private string HitTarget(Point point)
    {
        var layout = SyncLayout();
        if (layout.Back.Contains(point)) return "back";
        if (layout.Start.Contains(point)) return "start";
        if (layout.Action.Contains(point) && !StartActionTogglesSelection) return "toggle";
        for (int i = 0; i < 4; i++) if (layout.Tab(i).Contains(point)) return $"tab:{i}";
        var skills = _grid.PageItems;
        for (int i = 0; i < skills.Count; i++) if (layout.Tile(i).Contains(point)) return $"skill:{(int)skills[i]}";
        if (layout.Previous.Contains(point)) return "previous";
        if (layout.Next.Contains(point)) return "next";
        foreach (var chip in EquippedChips(layout)) if (chip.Bounds.Contains(point)) return $"remove:{(int)chip.Skill}";
        if (ChipOverflow(layout).Contains(point)) return "chips";
        return "";
    }
    private void Activate(string target)
    {
        if (target == "back") _backRequested = true;
        else if (target == "start") Start();
        else if (target == "toggle") Toggle(FocusedSkill);
        else if (target == "previous") ChangePage(-1);
        else if (target == "next") ChangePage(1);
        else if (target == "chips") _chipOffset = (_chipOffset + Math.Max(1, EquippedChips(SyncLayout()).Count)) % Math.Max(1, _selected.Count);
        else if (target.StartsWith("tab:") && int.TryParse(target.AsSpan(4), out int category)) SetCategory((SkillCategory)category);
        else if (target.StartsWith("skill:") && int.TryParse(target.AsSpan(6), out int skill)) FocusSkill((SkillType)skill);
        else if (target.StartsWith("remove:") && int.TryParse(target.AsSpan(7), out int removed) && _selected.Contains((SkillType)removed)) Toggle((SkillType)removed);
    }
    private record EquippedChip(SkillType Skill, Rectangle Bounds);
    private IReadOnlyList<EquippedChip> EquippedChips(SkillSelectionLayout layout)
    {
        var selected = _grid.All.Where(_selected.Contains).ToArray();
        if (selected.Length == 0) return Array.Empty<EquippedChip>();
        _chipOffset %= selected.Length;
        var result = new List<EquippedChip>(); int x = layout.Chips.Left;
        int right = layout.Chips.Right - Math.Max(48, (int)(70 * layout.Scale));
        float scale = UITypography.Scale(_captionFont, UITextRole.Small, _graphicsDevice.Viewport);
        for (int i = _chipOffset; i < selected.Length; i++)
        {
            string label = $"{GameText.SkillName(selected[i])}  {CampaignSkillBalance.Cost(selected[i])} SP";
            int width = Math.Clamp((int)(_captionFont.MeasureString(UIFontGuard.Sanitize(_captionFont,label)).X * scale) +
                (int)(96 * layout.Scale), 110, Math.Max(112, (int)(290 * layout.Scale)));
            if (x + width > right) break;
            result.Add(new EquippedChip(selected[i], new Rectangle(x, layout.Chips.Y, width, layout.Chips.Height)));
            x += width + Math.Max(6, (int)(14 * layout.Scale));
        }
        return result;
    }
    private Rectangle ChipOverflow(SkillSelectionLayout layout)
    {
        if (_selected.Count <= EquippedChips(layout).Count) return Rectangle.Empty;
        int width = Math.Max(48, (int)(60 * layout.Scale));
        return new Rectangle(layout.Chips.Right - width, layout.Chips.Y, width, layout.Chips.Height);
    }
    public override void Draw(SpriteBatch batch, GameTime time)
    { if (IsVisible) DrawGridScreen(batch, SyncLayout()); }
}
