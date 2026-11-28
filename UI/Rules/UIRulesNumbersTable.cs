#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Fida.UI.Rules
{
    /// <summary>
    /// The Numbers page. The table is the page: a row's description is the thing you click to
    /// switch that row's effect between the printed rule and the variants you own. Two number
    /// cards on top let you try the result without leaving the page.
    /// <para>
    /// The egg only decides whether the rows respond: locked, the same page is a plain reference.
    /// </para>
    /// </summary>
    public sealed class UIRulesNumbersAssembly : UIControl
    {
        private static readonly (string Slot, int Digit, string Default)[] Rows =
        {
            ("7", 7, "attack 1"),
            ("8", 8, "attack 1"),
            ("9", 9, "heal 1"),
            ("0", 0, "shield"),
            ("1 + 6", -1, "attack 2"),
            ("5 + 5", -1, "attack 3"),
            ("6 + 6", -1, "attack 5"),
            ("0 + 0", -1, "heal 10")
        };

        /// <summary>Slightly darker cream, so a switched row reads as a slip pasted on the page.</summary>
        private static readonly Color SlipStock = new(224, 214, 193);

        private readonly ResourceManager _resources;
        private readonly bool _editable;
        private readonly int _digitA;
        private readonly int _digitB;

        private Rectangle[] _rowBounds = [];
        private Rectangle[] _digitBounds = [];

        /// <summary>
        /// Shared across rebuilds: every click rebuilds the page, so an instance field would
        /// forget that the button is still held and fire again on the next frame.
        /// </summary>
        private static bool _wasDown;

        /// <summary>Raised with the row index whose description was clicked (editable mode only).</summary>
        public Action<int>? RowTapped { get; set; }

        /// <summary>Raised with 0 or 1 when one of the two number cards is clicked.</summary>
        public Action<int>? DigitTapped { get; set; }

        public UIRulesNumbersAssembly(ResourceManager resources, bool editable, int digitA, int digitB)
            : base(-58)
        {
            _resources = resources;
            _editable = editable;
            _digitA = digitA;
            _digitB = digitB;
        }

        /// <summary>The digit a row switches, or -1 for the pair rows (which have no variants).</summary>
        public static int DigitOf(int row) => row >= 0 && row < Rows.Length ? Rows[row].Digit : -1;

        private static int RowHeight(UIRulesLayout layout) => layout.D(layout.Portrait ? 84 : 38);

        private static int TableTop(UIRulesLayout layout) =>
            layout.Inner.Top + layout.D(layout.Portrait ? 210 : 180);

        /// <summary>Rects for the eight table rows, in table order.</summary>
        public static Rectangle[] RowBounds(UIRulesLayout layout)
        {
            Rectangle inner = layout.Inner;
            int height = RowHeight(layout);
            int top = TableTop(layout);
            var bounds = new Rectangle[Rows.Length];
            for (int i = 0; i < Rows.Length; i++)
                bounds[i] = new Rectangle(inner.Left, top + i * height, inner.Width, height - layout.D(2));
            return bounds;
        }

        /// <summary>Rects for the two number cards (left, right).</summary>
        public static Rectangle[] DigitBounds(UIRulesLayout layout)
        {
            Rectangle inner = layout.Inner;
            int size = layout.D(layout.Portrait ? 96 : 76);
            int top = inner.Top + layout.D(layout.Portrait ? 78 : 64);
            return
            [
                new Rectangle(inner.Left, top, size, size),
                new Rectangle(inner.Left + size + layout.D(18), top, size, size)
            ];
        }

        public override void Draw(SpriteBatch batch, GameTime time)
        {
            var layout = new UIRulesLayout(batch.GraphicsDevice.Viewport);
            _rowBounds = RowBounds(layout);
            _digitBounds = DigitBounds(layout);

            UIPaper.Sheet(batch, layout.Sheet, UIPaper.Face, 4, 10);
            var inner = layout.Inner;
            float scale = layout.Scale;
            bool portrait = layout.Portrait;

            UIRulesText.Line(batch, _resources.CaptionFont,
                _editable ? "NUMBERS 02 - TAP A ROW TO SWITCH IT" : "NUMBERS 02",
                new Rectangle(inner.Left, inner.Top, inner.Width, layout.D(26)), 24, scale,
                UIPaper.MutedInk);

            DrawSimulator(batch, layout, inner, scale);
            DrawTable(batch, layout, inner, scale);
        }

        private void DrawSimulator(SpriteBatch batch, UIRulesLayout layout, Rectangle inner, float scale)
        {
            bool portrait = layout.Portrait;
            UIRulesText.Line(batch, _resources.CaptionFont, "TRY IT",
                new Rectangle(inner.Left, inner.Top + layout.D(34), layout.D(200), layout.D(24)),
                22, scale, UIPaper.MutedInk);

            var digits = DigitBounds(layout);
            for (int i = 0; i < digits.Length; i++)
            {
                UIPaper.Sheet(batch, digits[i], UIPaper.Face, 2, 8);
                UIRulesText.Line(batch, _resources.TitleFont, (i == 0 ? _digitA : _digitB).ToString(),
                    digits[i], 50, scale, UIPaper.Ink, centered: true);
            }

            var outcome = CombinationTable.Match([_digitA, _digitB], CombinationLoadout.Active);
            string text = outcome.IsNone ? "no effect" : Describe(outcome);
            var area = new Rectangle(digits[1].Right + layout.D(26), digits[0].Top,
                inner.Right - digits[1].Right - layout.D(26), layout.D(44));
            UIRulesText.Line(batch, _resources.Font, text, area, portrait ? 30 : 28, scale,
                outcome.IsNone ? UIPaper.MutedInk : UIPaper.Ink);
            UIRulesText.Line(batch, _resources.CaptionFont, "tap a number to add 1",
                new Rectangle(area.Left, area.Top + layout.D(42), area.Width, layout.D(24)), 20, scale,
                UIPaper.MutedInk);
        }

        private void DrawTable(SpriteBatch batch, UIRulesLayout layout, Rectangle inner, float scale)
        {
            bool portrait = layout.Portrait;
            int numberWidth = layout.D(portrait ? 220 : 210);

            for (int i = 0; i < Rows.Length; i++)
            {
                var (slot, digit, fallback) = Rows[i];
                Rectangle row = _rowBounds[i];
                var installed = digit >= 0
                    ? CombinationLoadout.Variants(digit)
                        .FirstOrDefault(card => CombinationLoadout.IsEquipped(card.Id))
                    : null;

                if (installed != null)
                {
                    // A switched row is a slip pasted over the printed rule.
                    UIPaper.Sheet(batch, row, SlipStock, 1, 4, shadow: false);
                    UIRulesText.Line(batch, _resources.TitleFont, slot,
                        new Rectangle(row.Left + layout.D(8), row.Top, numberWidth - layout.D(8), row.Height),
                        portrait ? 30 : 28, scale, UIPaper.MutedInk);
                    UIRulesText.Line(batch, _resources.LabelFont, Describe(installed),
                        new Rectangle(row.Left + numberWidth, row.Top, row.Width - numberWidth,
                            row.Height), portrait ? 30 : 26, scale, UIPaper.Ink);
                }
                else
                {
                    UIRulesText.Line(batch, _resources.TitleFont, slot,
                        new Rectangle(row.Left, row.Top, numberWidth, row.Height),
                        portrait ? 30 : 28, scale, UIPaper.Ink);
                    UIRulesText.Line(batch, _resources.Font, fallback,
                        new Rectangle(row.Left + numberWidth, row.Top, row.Width - numberWidth,
                            row.Height), portrait ? 28 : 24, scale, UIPaper.MutedInk);
                }

                UIRulesPaint.Hairline(batch, _resources.Pixel,
                    new Rectangle(row.Left, row.Bottom, row.Width, 1));
            }
        }

        private static string Describe(CombinationCard card)
        {
            string effect = card.Rules[0].Kind == CombinationKind.Heal
                ? $"heal {card.Rules[0].Amount}"
                : $"attack {card.Rules[0].Amount}";
            string pairs = string.Join(" / ", card.Rules.Select(rule => $"{rule.A} & {rule.B}"));
            return $"{pairs} = {effect}";
        }

        private static string Describe(CombinationOutcome outcome)
        {
            string action = outcome.Kind == CombinationKind.Heal
                ? $"heal {outcome.Amount}"
                : $"attack {outcome.Amount}";
            string source = outcome.SourceId == CombinationTable.BaseSource ? "" : " (your row)";
            return $"{action}{source}";
        }

        /// <summary>
        /// Click handling lives here because the page is a drawing surface, not a stack of buttons.
        /// Geometry comes from the last Draw, so the hit boxes are always the painted ones.
        /// </summary>
        public override void Update(MouseState mouseState)
        {
            bool down = mouseState.LeftButton == ButtonState.Pressed;
            bool clicked = down && !_wasDown;
            _wasDown = down;
            if (!clicked) return;

            // The two number cards are a teaching tool: they answer "what would these do?"
            // whether or not the effect-table egg has been found.
            for (int i = 0; i < _digitBounds.Length; i++)
            {
                if (!_digitBounds[i].Contains(mouseState.X, mouseState.Y)) continue;
                DigitTapped?.Invoke(i);
                return;
            }

            if (!_editable) return;
            for (int i = 0; i < _rowBounds.Length; i++)
            {
                if (!_rowBounds[i].Contains(mouseState.X, mouseState.Y)) continue;
                if (DigitOf(i) < 0) return;
                RowTapped?.Invoke(i);
                return;
            }
        }
    }
}
