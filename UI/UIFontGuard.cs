#nullable enable
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace Fida.UI
{
    /// <summary>
    /// Prevents data-driven UI text from crashing SpriteFont rendering when a glyph is absent.
    /// Unsupported characters are replaced at the rendering boundary; source data is unchanged.
    /// </summary>
    public static class UIFontGuard
    {
        private sealed class GlyphSet
        {
            public HashSet<char> Characters { get; }
            public char Replacement { get; }

            public GlyphSet(SpriteFont font)
            {
                Characters = new HashSet<char>(font.Characters);
                Replacement = Characters.Contains('?') ? '?' : Characters.Contains(' ') ? ' ' : '\0';
            }
        }

        private static readonly ConditionalWeakTable<SpriteFont, GlyphSet> Cache = new();

        public static bool Supports(SpriteFont font, string text) => Sanitize(font, text) == text;

        public static string Sanitize(SpriteFont font, string text)
        {
            if (font == null || string.IsNullOrEmpty(text)) return text ?? string.Empty;
            GlyphSet glyphs = Cache.GetValue(font, static value => new GlyphSet(value));
            StringBuilder? result = null;

            for (int i = 0; i < text.Length; i++)
            {
                char character = text[i];
                bool supported = character is '\r' or '\n' || glyphs.Characters.Contains(character);
                if (supported) continue;

                result ??= new StringBuilder(text);
                result[i] = glyphs.Replacement;
            }

            return result?.ToString() ?? text;
        }
    }
}
