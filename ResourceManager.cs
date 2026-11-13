#nullable enable
// ResourceManager.cs 文件
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content; // 需要ContentManager
using System.Collections.Generic; // 需要Dictionary

namespace Fida
{
    /// <summary>
    /// ResourceManager 类负责加载和管理游戏所需的各种资源，如字体和纹理。
    /// </summary>
    public class ResourceManager
    {
        private GraphicsDevice _graphicsDevice;

        // 纹理字典，用于存储和管理所有加载的Texture2D
        // 在 LoadContent 中初始化；在此之前不允许使用。
        private Dictionary<string, System.Lazy<Texture2D>> _textures = null!;

        // 游戏字体，用于UI文本
        // 由此类在 LoadContent() 中赋值，所有 UI 代码都把 Fida 字体当作非 null 使用。
        public SpriteFont Font { get; private set; } = null!;
        public SpriteFont CaptionFont { get; private set; } = null!;
        public SpriteFont LabelFont { get; private set; } = null!;

        // IBM Plex Sans SemiBold for editorial headings; fall back to body font.
        public SpriteFont TitleFont { get; private set; } = null!;

        // IBM Plex Sans Regular used by the zoomable campaign UI.
        public SpriteFont MapFont { get; private set; } = null!;

        // 单像素纹理，用于绘制矩形边框和填充
        public Texture2D Pixel { get; private set; } = null!;

        /// <summary>Procedural paper tooth, applied once per frame by the UI renderer.</summary>
        public Effect PaperGrainEffect { get; private set; } = null!;

        /// <summary>Feathered strip for smooth rotated map connections.</summary>
        public Texture2D AntialiasedLine { get; private set; } = null!;

        /// <summary>获取图形设备实例</summary>
        public GraphicsDevice GraphicsDevice => _graphicsDevice;

        /// <summary>
        /// ResourceManager 的构造函数。
        /// </summary>
        /// <param name="graphicsDevice">图形设备实例，用于创建纹理。</param>
        public ResourceManager(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
        }

        /// <summary>
        /// 加载所有UI和游戏所需的资源。
        /// </summary>
        /// <param name="contentManager">游戏的内容管理器。</param>
        public void LoadContent(ContentManager contentManager)
        {
            // Both platforms bundle the same IBM Plex Sans family.
            Font = contentManager.Load<SpriteFont>("source_han_sans");
            if (Font==null)
            {
                throw new ContentLoadException("Failed to load font.");
            }

            CaptionFont = contentManager.Load<SpriteFont>("ui_caption");
            LabelFont = contentManager.Load<SpriteFont>("ui_label");

            // Paper grain is generated in the shader, so no grain texture is kept in memory.
            PaperGrainEffect = contentManager.Load<Effect>("paper_grain");

            // Load the matching sans-serif title face; fall back safely if unavailable.
            try
            {
                TitleFont = contentManager.Load<SpriteFont>("source_han_title");
                if (!UI.UIFontGuard.Supports(TitleFont, "Fida Aa-Zz 0-9"))
                {
                    System.Diagnostics.Debug.WriteLine("Title font lacks required Latin glyphs, falling back to body font.");
                    TitleFont = Font;
                }
            }
            catch (ContentLoadException)
            {
                System.Diagnostics.Debug.WriteLine("Title font not found, falling back to body font.");
                TitleFont = Font;
            }

            try
            {
                MapFont = contentManager.Load<SpriteFont>("source_han_sans");
            }
            catch (ContentLoadException)
            {
                MapFont = Font;
            }

            // 创建一个单像素纹理，用于绘制矩形边框和填充
            // Create a single-pixel texture for drawing rectangle outlines and fills
            Pixel = new Texture2D(_graphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });
            AntialiasedLine = CreateAntialiasedLineTexture();
            _textures = new Dictionary<string, System.Lazy<Texture2D>>();
            LoadTexture(contentManager, "cold_roster", "portraits/paper_roster");
            LoadTexture(contentManager, "dialogue_mara", "portraits/dialogue_mara_concept");
            LoadTexture(contentManager, "paper_chapters", "paper/chapters");
            UI.UIPaper.RegisterScenes(_graphicsDevice, () => GetTexture("paper_chapters"));
            LoadTexture(contentManager, "paper_missions", "paper/mission_locations_v2");
            UI.UIPaper.RegisterMissionScenes(_graphicsDevice, () => GetTexture("paper_missions"));
            LoadTexture(contentManager, "campaign_cutpaper", "paper/campaign_cutpaper_locations");
            LoadTexture(contentManager, "campaign_cutpaper_extra", "paper/campaign_cutpaper_locations_extra");
            UI.UIPaper.RegisterCutpaperScenes(_graphicsDevice, () => GetTexture("campaign_cutpaper"), () => GetTexture("campaign_cutpaper_extra"));
            var campaignNodeAtlases = new System.Lazy<Texture2D>[5];
            for (int i = 0; i < campaignNodeAtlases.Length; i++)
            {
                string key = $"campaign_nodes_{i + 1}";
                LoadTexture(contentManager, key, $"paper/{key}");
                campaignNodeAtlases[i] = _textures[key];
            }
            UI.UIPaper.RegisterCampaignNodes(_graphicsDevice, campaignNodeAtlases);
            LoadTexture(contentManager, "route_locations", "paper/route_locations_concept");
            UI.UIPaper.RegisterRouteScenes(_graphicsDevice, () => GetTexture("route_locations"));
            LoadTexture(contentManager, "paper_skins", "paper/skins");
            LoadTexture(contentManager, "paper_background", "paper/background");
            LoadTexture(contentManager, "paper_main_art", "paper/main_journey");
            LoadTexture(contentManager, "paper_battle", "paper/battle");
            LoadTexture(contentManager, "victory_road", "paper/victory_road_concept");
            UI.UIPaper.RegisterMaterials(_graphicsDevice, GetTexture("paper_skins"), () => GetTexture("paper_background"), () => GetTexture("paper_battle"));

        }

        private Texture2D CreateAntialiasedLineTexture()
        {
            const int width = 64;
            const int height = 7;
            byte[] rowAlpha = { 0, 72, 180, 255, 180, 72, 0 };
            var data = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float endFade = x == 0 || x == width - 1 ? 0f :
                        x == 1 || x == width - 2 ? 0.55f : 1f;
                    data[y * width + x] = Color.White * (rowAlpha[y] / 255f * endFade);
                }
            }
            var texture = new Texture2D(_graphicsDevice, width, height);
            texture.SetData(data);
            return texture;
        }

        // Register assets without decoding every page's artwork at startup.
        // ContentManager still owns each texture and loads it only once.
        private void LoadTexture(ContentManager contentManager, string name, string path)
        {
            _textures[name] = new System.Lazy<Texture2D>(() =>
            {
                try { return contentManager.Load<Texture2D>(path); }
                catch (ContentLoadException ex)
                {
                    throw new ContentLoadException($"Failed to load texture: {path}", ex);
                }
            });
        }

        public Texture2D GetTexture(string name)
        {
            if (_textures.TryGetValue(name, out var value))
            {
                return value.Value;
            }
            else
            {
                // 未注册的纹理属于编程错误：调用方都会直接解引用返回值，
                // 与其把 null 传下去，不如在此显式失败。
                throw new System.InvalidOperationException($"Texture not registered: {name}");
            }
        }

        /// <summary>
        /// Resolves a logical portrait key and falls back to a stable player/enemy asset.
        /// New campaign content only needs to provide a matching portrait key.
        /// </summary>
        public Texture2D GetPortrait(string portraitKey, bool isLocalPlayer)
        {
            if (!string.IsNullOrWhiteSpace(portraitKey) &&
                _textures.TryGetValue($"portrait_{portraitKey}", out var portrait)) return portrait.Value;
            return GetTexture("cold_roster");
        }

        /// <summary>
        /// 加载玩家头像纹理。如果加载失败，返回 null（不显示头像）。
        /// </summary>
        public Texture2D? LoadPortraitTexture(ContentManager contentManager, string path)
        {
            try
            {
                return contentManager.Load<Texture2D>(path);
            }
            catch (ContentLoadException)
            {
                // 头像加载失败时不崩溃，返回 null 表示不显示
                System.Diagnostics.Debug.WriteLine($"Portrait texture not found: {path}, skipping.");
                return null;
            }
        }


        /// <summary>
        /// 清理加载的资源。
        /// </summary>
        public void Dispose()
        {
            // 纹理和字体通常由ContentManager管理，但在某些情况下如果手动创建，则需要手动Dispose
            // Textures and fonts are typically managed by ContentManager.
            // However, if they are created manually, they need to be disposed manually.
            Pixel?.Dispose();
            Pixel = null!;
            AntialiasedLine?.Dispose();
            AntialiasedLine = null!;
        }
    }
}
