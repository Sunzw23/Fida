#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

public partial class UIScreenBuilder
{
    private void BuildReferenceOnline(bool connected)
    {
        var layout = new UIReferenceLayout(_resourceManager.GraphicsDevice.Viewport);
        _uiControls.Add(new UIReferenceSurface(_resourceManager, connected ? UIReferenceScreen.Connection : UIReferenceScreen.Online) { Name = "online_art" });
        EditorialBack("backToMain");
        if (connected)
            EditorialButton("disconnect", "Disconnect", layout.SessionAction);
        else
        {
            EditorialButton("hostGame", "Create room", layout.HostAction, primary: true);
            var input = new UIRoomCodeInput(95, layout.Code, "Room code", _resourceManager.Pixel, _resourceManager.Font)
            { Name = "roomCodeInput", MinimalStyle = true };
            _uiControls.Add(input);
            var join = EditorialButton("joinGame", "Join room", layout.JoinAction, primary: true);
            join.Outlined = true;
            join.Disable();
            join.EnabledChecker = () => input.Value.Length == UIRoomCodeInput.MaxLen;
        }
        EditorialFocus();
    }

    private void BuildReferenceWaiting(bool hosting, string statusText, string roomCode = "")
    {
        var layout = new UIReferenceLayout(_resourceManager.GraphicsDevice.Viewport);
        var l = layout.Canvas;
        bool p = layout.Portrait;
        _uiControls.Add(new UIReferenceSurface(_resourceManager, hosting ? UIReferenceScreen.Hosting : UIReferenceScreen.Connecting) { Name = "connection_art" });
        EditorialBack("waitingBack");
        string[] lines = (statusText ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (hosting)
        {
            ReferenceLabel("roomCodeLabel", string.IsNullOrEmpty(roomCode) ? "----" : roomCode,
                p ? l.Rect(80, 430, 740, 190) : l.Rect(520, 430, 880, 210), _resourceManager.TitleFont);
            ((UILabel)_uiControls[^1]).PaperCodeBounds = p ? l.Rect(80,430,740,190) : l.Rect(520,430,880,210);
        }
        ReferenceLabel("statusLabel", lines.Length > 0 ? hosting ? lines[0] : (statusText ?? string.Empty) : hosting ? "Waiting for opponent" : "Connecting...",
            p ? l.Rect(80, 720, 740, 160) : l.Rect(400, 700, 1120, 110), _resourceManager.Font, UITextRole.Body);
        ReferenceLabel("addressLabel", lines.Length > 1 ? string.Join("  ", lines[1..]) : "",
            p ? l.Rect(80, 1070, 740, 90) : l.Rect(400, 875, 1120, 40), _resourceManager.CaptionFont, UITextRole.Caption);
        EditorialButton(hosting ? "cancelHost" : "cancelConnect", "Cancel", layout.SessionAction,primary:true);
        EditorialFocus();
    }

    private void ReferenceLabel(string name, string text, Rectangle bounds, SpriteFont font, UITextRole? role = null, bool wrap = false)
    {
        UILabel label = wrap ? new UIParagraphLabel(_uiControls.Count + 100, text, bounds, font)
            : new UILabel(_uiControls.Count + 100, text, bounds.Center.ToVector2(), font);
        label.Name = name;
        label.MaximumWidth = bounds.Width;
        label.MaximumHeight = bounds.Height;
        label.TypographyRole = role;
        _uiControls.Add(label);
    }
}
