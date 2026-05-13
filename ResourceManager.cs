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
        private Dictionary<string, Texture2D> _textures;

        // 游戏字体，用于UI文本
        public SpriteFont Font { get; private set; }

        // 单像素纹理，用于绘制矩形边框和填充
        public Texture2D Pixel { get; private set; }

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
            // 加载字体。确保你的字体文件名是"Font"
            // Load the font. Ensure your font filename is "Font"
            Font = contentManager.Load<SpriteFont>("source_han_sans");
            if (Font==null)
            {
                throw new ContentLoadException("Failed to load font.");
            }
            // 创建一个单像素纹理，用于绘制矩形边框和填充
            // Create a single-pixel texture for drawing rectangle outlines and fills
            Pixel = new Texture2D(_graphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });
            _textures = new Dictionary<string, Texture2D>();

        }

        private void _LoadTexture(ContentManager _contentManager, string name, string path)
        {
            if (_textures.ContainsKey(name))
            {
                // 可以选择抛出异常或仅输出警告，取决于你的需求
                // Console.WriteLine($"Warning: Texture with name '{name}' already exists. Overwriting.");
            }
            try
            {
                Texture2D texture = _contentManager.Load<Texture2D>(path);
                _textures[name] = texture;
            }
            catch (ContentLoadException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading texture '{path}': {ex.Message}");
                // 你可能希望在这里抛出异常或处理加载失败的情况
                throw new ContentLoadException($"Failed to load texture: {path}", ex);
            }
        }

        public Texture2D GetTexture(string name)
        {
            if (_textures.TryGetValue(name, out Texture2D value))
            {
                return value;
            }
            else
            {
                return null; // 返回 null 而不是抛出异常，允许缺失的纹理
            }
        }

        /// <summary>
        /// 加载玩家头像纹理。如果加载失败，返回 null（不显示头像）。
        /// </summary>
        public Texture2D LoadPortraitTexture(ContentManager contentManager, string path)
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
            Pixel = null;
        }
    }
}
