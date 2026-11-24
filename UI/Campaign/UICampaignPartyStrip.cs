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
/// Read-only party summary on the skill loadout screen: who is coming, and one way back to
/// the companion screen that owns the choice. Companion selection itself lives on
/// <see cref="UICampaignCompanionSelect"/>; this strip never edits the party.
/// </summary>
public sealed class UICampaignPartyStrip : UIControl
{
    private readonly GraphicsDevice _device;
    private readonly Texture2D _pixel;
    private readonly ResourceManager _resources;
    private readonly SpriteFont _font;
    private readonly SpriteFont _captionFont;
    private readonly CampaignNode _node;
    private readonly Dictionary<Rectangle, (float Hover, float Press, bool Enabled)> _motion = new();
    private IReadOnlyList<CampaignCompanion> _party = Array.Empty<CampaignCompanion>();
    private KeyboardState _previousKeyboard;
    private GamePadState _previousGamePad;
    private MouseState _previousMouse;
    private bool _inputInitialized;
    private string _pressedTarget = "";
    private bool _changeRequested;

    public UICampaignPartyStrip(int id, GraphicsDevice graphicsDevice, ResourceManager resources,
        CampaignNode node, SpriteFont font, SpriteFont? captionFont = null, int partyLimit = -1) : base(id)
    {
        _device = graphicsDevice;
        _resources = resources;
        _pixel = resources.Pixel;
        _font = font;
        _captionFont = captionFont ?? font;
        _node = node;
        _partyLimit = partyLimit > 0 ? partyLimit : CampaignCompanions.PartyLimitFor(node);
        Name = "party_strip";
    }

    private readonly int _partyLimit;

    public bool InputBlocked { get; set; }
    /// <summary>Co-op hides the choice, so the strip says who actually joins instead.</summary>
    public string StatusOverride { get; set; } = "";

    public Rectangle BarBounds => new SkillSelectionLayout(
        _device.Viewport.Width, _device.Viewport.Height).Party;

    public IReadOnlyList<CampaignCompanion> Party => _party;

    public override Rectangle? GetScreenBounds(Viewport viewport) => IsVisible
        ? new SkillSelectionLayout(viewport.Width, viewport.Height).Party
        : null;

    public void SetParty(IEnumerable<string>? companionIds)
    {
        _party = (companionIds ?? Array.Empty<string>())
            .Select(CampaignCompanions.Get).OfType<CampaignCompanion>().ToArray();
    }

    public bool TryConsumeChangeRequest()
    {
        bool requested = _changeRequested;
        _changeRequested = false;
        return requested;
    }

    public override void Update(GameTime gameTime)
    {
        double seconds = gameTime.ElapsedGameTime.TotalSeconds;
        foreach (var bounds in _motion.Keys.ToArray())
        {
            var state = _motion[bounds];
            bool hover = Enabled && bounds.Contains(_previousMouse.Position);
            bool press = hover && _previousMouse.LeftButton == ButtonState.Pressed &&
                _pressedTarget.Length > 0 && _pressedTarget == HitTarget(_previousMouse.Position);
            _motion[bounds] = (UIMotion.Approach(state.Hover, hover ? 1f : 0f, seconds),
                UIMotion.Approach(state.Press, press ? 1f : 0f, seconds, 24), state.Enabled);
        }

        KeyboardState keyboard = Keyboard.GetState();
        GamePadState gamePad = GamePad.GetState(PlayerIndex.One);
        if (_inputInitialized && Enabled)
        {
            if (Pressed(keyboard, Keys.C) || Pressed(gamePad, Buttons.Y)) _changeRequested = true;
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
        if (mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released)
            _pressedTarget = HitTarget(mouse.Position);
        if (mouse.LeftButton == ButtonState.Released && _previousMouse.LeftButton == ButtonState.Pressed)
        {
            string target = HitTarget(mouse.Position);
            if (target.Length > 0 && target == _pressedTarget && Enabled) _changeRequested = true;
            _pressedTarget = "";
        }
        _previousMouse = mouse;
    }

    private bool Enabled => !InputBlocked;

    private string HitTarget(Point point) => BarBounds.Contains(point) ? "change" : "";

    public override void Draw(SpriteBatch batch, GameTime gameTime)
    {
        if (!IsVisible) return;
        Viewport viewport = _device.Viewport;
        Rectangle bar = BarBounds;
        if (bar.Width < 24 || bar.Height < 16) return;
        float scale = ScaleFor(viewport);
        int pad = Math.Max(8, (int)(18 * scale));
        int gutter = Math.Max(16, (int)(26 * scale));

        // The whole ticket is the "change party" affordance, so it carries the hover/press.
        Surface(batch, bar, UIPaper.Face, true, 7);
        var labelArea = new Rectangle(bar.Left + pad, bar.Top + pad,
            Math.Max(1, LabelWidth(bar, scale)), Math.Max(1, bar.Height - pad * 2));
        Text(batch, _captionFont, "PARTY", labelArea, UIPaper.MutedInk, true);
        var counterArea = new Rectangle(labelArea.Right, labelArea.Top,
            Math.Max(1, (int)(80 * scale)), labelArea.Height);
        Text(batch, _captionFont, $"{_party.Count} / {_partyLimit}",
            counterArea, UIPaper.MutedInk, true);
        int divider = counterArea.Right + pad / 2;
        batch.Draw(_pixel, new Rectangle(divider, bar.Top + pad, 1, Math.Max(1, bar.Height - pad * 2)),
            UITheme.Line * 0.7f);

        int right = bar.Right - pad;
        string status = string.IsNullOrWhiteSpace(StatusOverride) ? "CHANGE" : StatusOverride;
        int statusWidth = Math.Min((int)(260 * scale), Math.Max(1, right - divider - gutter));
        var changeArea = new Rectangle(right - statusWidth, bar.Center.Y - gutter / 2,
            statusWidth, gutter * 2 > bar.Height - pad * 2 ? Math.Max(1, bar.Height - pad * 2) : gutter * 2);
        Text(batch, _captionFont, status, changeArea, UIPaper.MutedInk);

        int x = divider + gutter;
        int limit = changeArea.Left - gutter;
        int height = Math.Max(24, bar.Height - pad * 2);
        int top = bar.Center.Y - height / 2;
        if (_party.Count == 0)
        {
            Text(batch, _font, "No companion joins this battle", new Rectangle(x, top,
                Math.Max(1, limit - x), height), UIPaper.Ink, true);
        }
        foreach (var companion in _party)
        {
            if (_party.Count > 0 && x + gutter >= limit) break;
            string label = $"{companion.Name}  ·  HP {companion.Health}";
            float measured = _captionFont.MeasureString(UIFontGuard.Sanitize(_captionFont, label)).X;
            float textScale = Math.Min(1f, Math.Max(1, height) * 0.45f / Math.Max(1f, _captionFont.LineSpacing));
            int width = Math.Clamp((int)(measured * textScale) + gutter + pad * 3,
                Math.Max(72, (int)(130 * scale)), Math.Max(90, (int)(340 * scale)));
            width = Math.Min(width, Math.Max(1, limit - x));
            var chip = new Rectangle(x, top, width, height);
            // Chips are a summary, not targets: the whole ticket opens the roster screen.
            UIPaper.Sheet(batch, chip, UITheme.PanelLight, 1, 5, shape: UIPaperShape.Ticket);
            var art = new Rectangle(chip.Left + 6, chip.Center.Y - (height - 12) / 2,
                Math.Max(8, height - 12), Math.Max(8, height - 12));
            UICompanionArt.Draw(batch, _resources, companion, art);
            Text(batch, _captionFont, label, new Rectangle(art.Right + 8, chip.Top + 2,
                Math.Max(1, chip.Width - (art.Right - chip.Left) - pad), Math.Max(1, chip.Height - 4)),
                UITheme.Ink, true);
            x += width + Math.Max(6, (int)(14 * scale));
        }
    }

    private void Surface(SpriteBatch batch, Rectangle bounds, Color color, bool enabled, int cut)
    {
        if (_motion.TryGetValue(bounds, out var state))
            UIPaper.Button(batch, bounds, color, state.Hover, state.Press, enabled && !InputBlocked, cut: cut);
        else
            UIPaper.Button(batch, bounds, color, 0, 0, enabled && !InputBlocked, cut: cut);
    }

    private static int LabelWidth(Rectangle bar, float scale) =>
        Math.Clamp(bar.Width * 14 / 100, (int)(80 * scale), (int)(220 * scale));

    private static float ScaleFor(Viewport viewport) => viewport.Height > viewport.Width
        ? Math.Min(viewport.Width / 1080f, viewport.Height / 1920f)
        : UIDesktopLayout.Scale(viewport);

    private void Text(SpriteBatch batch, SpriteFont font, string text, Rectangle area, Color color,
        bool left = false)
    {
        if (area.Width <= 0 || area.Height <= 0) return;
        string safe = UIFontGuard.Sanitize(font, text);
        Vector2 size = font.MeasureString(safe);
        if (size.X <= 0f) return;
        float preferred = area.Height * 0.58f / Math.Max(1f, font.LineSpacing);
        float scale = UITextRenderer.FitScale(size, area.Width, area.Height, preferred);
        Vector2 position = new(left ? area.Left : area.Center.X - size.X * scale / 2f,
            area.Center.Y - size.Y * scale / 2f);
        UITextRenderer.DrawString(batch, font, safe, position, color, scale: scale);
    }

    private bool Pressed(KeyboardState current, Keys key) =>
        current.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);

    private bool Pressed(GamePadState current, Buttons button) =>
        current.IsConnected && current.IsButtonDown(button) && !_previousGamePad.IsButtonDown(button);
}
