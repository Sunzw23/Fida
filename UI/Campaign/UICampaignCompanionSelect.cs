#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Campaign;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI.Campaign;

/// <summary>
/// Full companion preparation screen: a roster of character cards with art, a detail panel
/// for the focused companion, and the party the player is taking into the battle.
///
/// It owns presentation and input only. Every intent is a single id (empty means the
/// "travel alone" card) and the state applies it through
/// <see cref="Fida.Logic.GameSetup.ToggleCampaignCompanion"/>. Because the roster, the party
/// limit and the node's allowed ids all come from campaign data, meeting more companions
/// adds cards and pages without touching this class.
/// </summary>
public sealed partial class UICampaignCompanionSelect : UIControl
{

    private readonly GraphicsDevice _device;
    private readonly Texture2D _pixel;
    private readonly ResourceManager _resources;
    private readonly SpriteFont _font;
    private readonly SpriteFont _titleFont;
    private readonly SpriteFont _captionFont;
    private readonly CampaignNode _node;
    private readonly IReadOnlyList<CampaignCompanion> _roster;

    /// <summary>Roster ids in display order; only real companions get a card.</summary>
    private readonly List<string> _cards = new();
    private readonly List<string> _selected = new();
    private readonly Dictionary<Rectangle, (float Hover, float Press, bool Enabled)> _actionMotion = new();
    private KeyboardState _previousKeyboard;
    private GamePadState _previousGamePad;
    private MouseState _previousMouse;
    private bool _inputInitialized;
    private string _pressedTarget = "";
    private int _focus;
    private string? _pendingToggle;
    private bool _backRequested;
    private bool _continueRequested;

    public UICampaignCompanionSelect(int id, GraphicsDevice graphicsDevice, ResourceManager resources,
        CampaignNode node, IReadOnlyList<CampaignCompanion> roster, SpriteFont font,
        SpriteFont titleFont, SpriteFont? captionFont = null) : base(id)
    {
        _device = graphicsDevice;
        _resources = resources;
        _pixel = resources.Pixel;
        _font = font;
        _titleFont = titleFont;
        _captionFont = captionFont ?? font;
        _node = node;
        _roster = roster ?? Array.Empty<CampaignCompanion>();
        _cards.AddRange(_roster.Select(companion => companion.Id));
        HintText = PartyLimit <= 1 ? "ONE COMPANION" : $"UP TO {PartyLimit} COMPANIONS";
        Name = "companion_select";
    }

    /// <summary>Caption under the continue action, e.g. "Continue" or "Start battle".</summary>
    public string ContinueActionText { get; set; } = "CONTINUE";
    public string HeaderText { get; set; } = "Companions";
    public string HintText { get; set; } = "";
    /// <summary>Replaces the header hint while an online draft owns the ally slot.</summary>
    public string StatusText { get; set; } = "";
    public bool InputBlocked { get; set; }

    public CompanionSelectLayout Layout => new(_device.Viewport.Width, _device.Viewport.Height,
        null, null, _cards.Count);
    public IReadOnlyList<string> CardIds => _cards;
    public IReadOnlyList<string> SelectedIds => _selected;
    public string FocusedCardId => _cards.Count == 0 ? "" : _cards[Math.Clamp(_focus, 0, _cards.Count - 1)];
    public CampaignCompanion? FocusedCompanion => _roster.FirstOrDefault(companion =>
        string.Equals(companion.Id, FocusedCardId, StringComparison.OrdinalIgnoreCase));
    public int FocusedIndex => Math.Clamp(_focus, 0, Math.Max(0, _cards.Count - 1));
    public int Page => Layout.PageSize <= 0 ? 0 : FocusedIndex / Layout.PageSize;
    public int PageCount => Layout.PageSize <= 0
        ? 1
        : Math.Max(1, (_cards.Count + Layout.PageSize - 1) / Layout.PageSize);
    // The roster passed in is already filtered (node choice + who the player has met).
    public int PartyLimit => Math.Min(CampaignCompanions.PartyLimit, _roster.Count);

    /// <summary>Cards on the current page, left-to-right, top-to-bottom.</summary>
    public IReadOnlyList<string> VisibleCardIds
    {
        get
        {
            var layout = Layout;
            int start = Page * layout.PageSize;
            return _cards.Skip(start).Take(layout.PageSize).ToArray();
        }
    }

    public IReadOnlyList<(string CardId, Rectangle Bounds)> CardBounds => VisibleCardIds
        .Select((cardId, index) => (cardId, Layout.Card(index))).ToArray();

    public override Rectangle? GetScreenBounds(Viewport viewport) => IsVisible
        ? new CompanionSelectLayout(viewport.Width, viewport.Height).Roster
        : null;

    /// <summary>Mirrors the authoritative party from the campaign state.</summary>
    public void SetSelection(IEnumerable<string>? companionIds)
    {
        _selected.Clear();
        foreach (string id in CampaignLoadout.SanitizeCompanions(_node, companionIds))
            _selected.Add(id);
        if (_focus >= _cards.Count) _focus = Math.Max(0, _cards.Count - 1);
    }

    public void FocusCard(string? cardId)
    {
        int index = _cards.FindIndex(id => string.Equals(id, cardId ?? "", StringComparison.OrdinalIgnoreCase));
        if (index >= 0) _focus = index;
    }

    public bool IsSelected(string cardId) => _selected.Any(id =>
        string.Equals(id, cardId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Dismissing the last invited companion is the "travel alone" state, so a selected
    /// card always stays toggleable.
    /// </summary>
    public bool CanToggle(string cardId) => CampaignLoadout.CanSelectCompanion(_node, _selected, cardId);

    public bool TryConsumeToggle(out string cardId)
    {
        cardId = _pendingToggle ?? "";
        bool consumed = _pendingToggle != null;
        _pendingToggle = null;
        return consumed;
    }

    public bool ConsumeBackRequest()
    {
        bool requested = _backRequested;
        _backRequested = false;
        return requested;
    }

    public bool ConsumeContinueRequest()
    {
        bool requested = _continueRequested;
        _continueRequested = false;
        return requested;
    }

    public override void Update(GameTime gameTime)
    {
        UpdateMotion(gameTime);
        KeyboardState keyboard = Keyboard.GetState();
        GamePadState gamePad = GamePad.GetState(PlayerIndex.One);
        var layout = Layout;
        if (_inputInitialized && !InputBlocked)
        {
            int dx = Pressed(keyboard, Keys.Left) || Pressed(keyboard, Keys.A) ||
                     Pressed(gamePad, Buttons.DPadLeft) ? -1
                : Pressed(keyboard, Keys.Right) || Pressed(keyboard, Keys.D) ||
                  Pressed(gamePad, Buttons.DPadRight) ? 1 : 0;
            int dy = Pressed(keyboard, Keys.Up) || Pressed(keyboard, Keys.W) ||
                     Pressed(gamePad, Buttons.DPadUp) ? -1
                : Pressed(keyboard, Keys.Down) || Pressed(keyboard, Keys.S) ||
                  Pressed(gamePad, Buttons.DPadDown) ? 1 : 0;
            if (gamePad.IsConnected)
            {
                if (gamePad.ThumbSticks.Left.X > .55f && _previousGamePad.ThumbSticks.Left.X <= .55f) dx = 1;
                if (gamePad.ThumbSticks.Left.X < -.55f && _previousGamePad.ThumbSticks.Left.X >= -.55f) dx = -1;
                if (gamePad.ThumbSticks.Left.Y > .55f && _previousGamePad.ThumbSticks.Left.Y <= .55f) dy = -1;
                if (gamePad.ThumbSticks.Left.Y < -.55f && _previousGamePad.ThumbSticks.Left.Y >= -.55f) dy = 1;
            }
            if (dx != 0 || dy != 0) Move(dx, dy, layout);
            if (Pressed(keyboard, Keys.PageUp) || Pressed(gamePad, Buttons.LeftShoulder)) ChangePage(-1, layout);
            if (Pressed(keyboard, Keys.PageDown) || Pressed(gamePad, Buttons.RightShoulder)) ChangePage(1, layout);
            if (Pressed(keyboard, Keys.Enter) || Pressed(keyboard, Keys.Space) ||
                Pressed(gamePad, Buttons.A)) Toggle(FocusedCardId);
            if (Pressed(keyboard, Keys.Escape) || Pressed(gamePad, Buttons.B) ||
                Pressed(gamePad, Buttons.Back)) _backRequested = true;
            if (Pressed(keyboard, Keys.F5) || Pressed(gamePad, Buttons.Start)) _continueRequested = true;
        }
        _previousKeyboard = keyboard;
        _previousGamePad = gamePad;
    }

    public override void Update(MouseState mouse)
    {
        if (!_inputInitialized)
        {
            _previousMouse = mouse;
            _previousKeyboard = Keyboard.GetState();
            _previousGamePad = GamePad.GetState(PlayerIndex.One);
            _inputInitialized = true;
            _pressedTarget = "";
            return;
        }

        var layout = Layout;
        int wheel = mouse.ScrollWheelValue - _previousMouse.ScrollWheelValue;
        if (wheel != 0 && layout.Roster.Contains(mouse.Position)) ChangePage(wheel > 0 ? -1 : 1, layout);
        if (mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released)
            _pressedTarget = HitTarget(mouse.Position, layout);
        if (mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed)
        {
            string target = HitTarget(mouse.Position, layout);
            if (target.Length > 0 && target == _pressedTarget) Activate(target, layout);
            _pressedTarget = "";
        }
        _previousMouse = mouse;
    }

    private void Move(int dx, int dy, CompanionSelectLayout layout)
    {
        if (_cards.Count == 0) return;
        int pageSize = Math.Max(1, layout.PageSize);
        int pageStart = FocusedIndex / pageSize * pageSize;
        int pageEnd = Math.Min(_cards.Count - 1, pageStart + pageSize - 1);
        int local = FocusedIndex - pageStart;
        int row = local / layout.Columns;
        int column = local % layout.Columns;
        column = Math.Clamp(column + dx, 0, layout.Columns - 1);
        row = Math.Clamp(row + dy, 0, Math.Max(0, layout.Rows - 1));
        int next = pageStart + row * layout.Columns + column;
        _focus = Math.Clamp(next, pageStart, pageEnd);
    }

    private void ChangePage(int delta, CompanionSelectLayout layout)
    {
        int pageSize = Math.Max(1, layout.PageSize);
        int page = Math.Clamp(Page + delta, 0, PageCount - 1);
        _focus = Math.Min(page * pageSize, _cards.Count - 1);
    }

    private void Toggle(string cardId)
    {
        if (!CanToggle(cardId)) return;
        _pendingToggle = cardId;
        Fida.Audio.AudioService.Feedback(Fida.Audio.SoundCue.Select);
    }

    private string HitTarget(Point point, CompanionSelectLayout layout)
    {
        if (layout.Back.Contains(point)) return "back";
        if (layout.Continue.Contains(point)) return "continue";
        IReadOnlyList<string> visible = VisibleCardIds;
        for (int i = 0; i < visible.Count; i++)
            if (layout.Card(i).Contains(point)) return "card:" + i;
        if (PageCount > 1)
        {
            if (layout.Previous.Contains(point)) return "previous";
            if (layout.Next.Contains(point)) return "next";
        }
        foreach (var (chipId, bounds) in PartyChips(layout))
            if (bounds.Contains(point)) return "remove:" + chipId;
        return "";
    }

    private void Activate(string target, CompanionSelectLayout layout)
    {
        if (target == "back") _backRequested = true;
        else if (target == "continue") _continueRequested = true;
        else if (target == "previous") ChangePage(-1, layout);
        else if (target == "next") ChangePage(1, layout);
        else if (target.StartsWith("card:", StringComparison.Ordinal) &&
                 int.TryParse(target.AsSpan(5), out int index))
        {
            IReadOnlyList<string> visible = VisibleCardIds;
            if (index < 0 || index >= visible.Count) return;
            FocusCard(visible[index]);
            Toggle(visible[index]);
        }
        else if (target.StartsWith("remove:", StringComparison.Ordinal))
        {
            string cardId = target["remove:".Length..];
            FocusCard(cardId);
            Toggle(cardId);
        }
    }

    private bool Pressed(KeyboardState current, Keys key) =>
        current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    private bool Pressed(GamePadState current, Buttons button) =>
        current.IsConnected && current.IsButtonDown(button) && !_previousGamePad.IsButtonDown(button);
}
