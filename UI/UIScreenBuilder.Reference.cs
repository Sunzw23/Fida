#nullable enable
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI;

public partial class UIScreenBuilder
{
    private void BuildReferenceOnline(bool connected, bool isHost, string detailLabel, string detailValue, bool direct = false)
    {
        var layout = new UIReferenceLayout(_resourceManager.GraphicsDevice.Viewport);
        _uiControls.Add(new UIReferenceSurface(_resourceManager, connected ? UIReferenceScreen.Connection : UIReferenceScreen.Online)
        {
            Name = "online_art",
            IsHost = isHost,
            Direct = direct,
            RoomCodeRequired = !direct,
            DetailLabel = string.IsNullOrWhiteSpace(detailLabel) ? "Server" : detailLabel,
            DetailValue = detailValue ?? string.Empty
        });
        EditorialBack("backToMain");
        if (connected)
        {
            EditorialButton("disconnect", "Disconnect", layout.DisconnectAction, primary: true);
        }
        else
        {
            EditorialButton("hostGame", direct ? "Create game" : "Create room", layout.HostAction, primary: true);
            if (direct)
            {
                // Serverless play: the guest finds a beacon on this network, or types the
                // address the host reads out. Both paths need no rendezvous service.
                var field = new Rectangle(layout.Code.X, layout.Code.Center.Y - 55, layout.Code.Width, 110);
                _uiControls.Add(new UIAddressField(95, field, "IP:PORT OR BLANK",
                    _resourceManager.Pixel, _resourceManager.Font)
                { Name = "addressInput" });
                var join = EditorialButton("joinGame", "Join game", layout.JoinAction, primary: true);
                join.Outlined = true;
            }
            else
            {
                var input = new UIRoomCodeInput(95, layout.Code, "Room code", _resourceManager.Pixel, _resourceManager.Font)
                { Name = "roomCodeInput", MinimalStyle = true };
                _uiControls.Add(input);
                var join = EditorialButton("joinGame", "Join room", layout.JoinAction, primary: true);
                join.Outlined = true;
                join.Disable();
                join.EnabledChecker = () => input.Value.Length == UIRoomCodeInput.MaxLen;
            }
        }
        EditorialFocus();
    }

    private void BuildReferenceWaiting(bool hosting, string statusText, string roomCode = "", bool direct = false)
    {
        var layout = new UIReferenceLayout(_resourceManager.GraphicsDevice.Viewport);
        var l = layout.Canvas;
        bool p = layout.Portrait;
        _uiControls.Add(new UIReferenceSurface(_resourceManager, hosting ? UIReferenceScreen.Hosting : UIReferenceScreen.Connecting)
        { Name = "connection_art", Direct = direct, RoomCodeRequired = !direct });
        EditorialBack("waitingBack");
        string[] lines = (statusText ?? "").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (hosting)
        {
            if (direct)
            {
                // A direct host shares an address, not a four-character code: print it as one
                // fitted line instead of the room-code slots.
                ReferenceLabel("roomCodeLabel", string.IsNullOrEmpty(roomCode) ? "----" : roomCode,
                    p ? l.Rect(50, 360, 800, 170) : l.Rect(320, 360, 960, 170), _resourceManager.TitleFont);
            }
            else
            {
                ReferenceLabel("roomCodeLabel", string.IsNullOrEmpty(roomCode) ? "----" : roomCode,
                    p ? l.Rect(80, 430, 740, 190) : l.Rect(520, 430, 880, 210), _resourceManager.TitleFont);
                ((UILabel)_uiControls[^1]).PaperCodeBounds = p ? l.Rect(80,430,740,190) : l.Rect(520,430,880,210);
            }
        }
        ReferenceLabel("statusLabel", lines.Length > 0 ? lines[0] : hosting ? "Waiting for opponent" : "Connecting...",
            p ? l.Rect(80, 720, 740, 160) : l.Rect(400, 700, 1120, 110), _resourceManager.Font, UITextRole.Body);
        ReferenceLabel("addressLabel", lines.Length > 1 ? string.Join("\n", lines[1..]) : "",
            p ? l.Rect(80, 1070, 740, 130) : l.Rect(400, 830, 1120, 96), _resourceManager.CaptionFont, UITextRole.Caption, wrap: true);
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
