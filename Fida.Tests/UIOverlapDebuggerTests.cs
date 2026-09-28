using Fida.UI;
using Microsoft.Xna.Framework;
using Xunit;

namespace Fida.Tests;

/// <summary>
/// 布局重叠检测器的纯几何核心测试：
/// 相交必须报出，包含/贴边/分离不得误报。
/// </summary>
public class UIOverlapDebuggerTests
{
    [Fact]
    public void FindCollisions_FlagsIntersectingPair()
    {
        var items = new List<(string, Rectangle)>
        {
            ("title", new Rectangle(100, 100, 400, 60)),
            ("label", new Rectangle(250, 120, 400, 60)) // 与 title 横向侵入
        };

        var collisions = UIOverlapDebugger.FindCollisions(items);

        var collision = Assert.Single(collisions);
        Assert.Equal("title", collision.A);
        Assert.Equal("label", collision.B);
        // 相交区域 = 横向重叠 [250..500) × 纵向重叠 [120..160)
        Assert.Equal(new Rectangle(250, 120, 250, 40), collision.Intersect);
    }

    [Fact]
    public void FindCollisions_IgnoresContainedPair()
    {
        // 按钮完全位于面板内部是合法布局（内容置于底衬之上），不得报出
        var items = new List<(string, Rectangle)>
        {
            ("panel", new Rectangle(0, 0, 800, 600)),
            ("button", new Rectangle(100, 100, 200, 60))
        };

        Assert.Empty(UIOverlapDebugger.FindCollisions(items));
    }

    [Fact]
    public void FindCollisions_IgnoresEdgeContainedPair()
    {
        // 贴边位于容器内部（共享一条边）同样视为包含，不得报出
        var items = new List<(string, Rectangle)>
        {
            ("panel", new Rectangle(0, 0, 800, 600)),
            ("button", new Rectangle(0, 0, 200, 60))
        };

        Assert.Empty(UIOverlapDebugger.FindCollisions(items));
    }

    [Fact]
    public void FindCollisions_IgnoresEdgeTouchingPair()
    {
        // 共享一条边但互不包含 → 贴边，不算重叠
        var items = new List<(string, Rectangle)>
        {
            ("left", new Rectangle(0, 0, 100, 100)),
            ("right", new Rectangle(100, 0, 100, 100))
        };

        Assert.Empty(UIOverlapDebugger.FindCollisions(items));
    }

    [Fact]
    public void FindCollisions_ReportsOnlyRealCollisionsAmongMany()
    {
        var items = new List<(string, Rectangle)>
        {
            ("a", new Rectangle(0, 0, 100, 100)),
            ("b", new Rectangle(90, 0, 100, 100)),   // a<->b 相交
            ("c", new Rectangle(0, 100, 100, 100)),  // 与 a/b 仅贴边
            ("d", new Rectangle(0, 0, 200, 200))     // a、b 完全被 d 包含
        };

        var collisions = UIOverlapDebugger.FindCollisions(items);

        // d 包含 a 与 b（合法）；c 贴边（合法）；唯一真冲突是 a<->b
        var collision = Assert.Single(collisions);
        Assert.Contains("a", new[] { collision.A, collision.B });
        Assert.Contains("b", new[] { collision.A, collision.B });
    }

    [Fact]
    public void FindCollisions_FlagsIdenticalRectangles()
    {
        // 完全相同的矩形 = 完全重叠（如两个控件画在同一位置），必须报出
        var items = new List<(string, Rectangle)>
        {
            ("a", new Rectangle(0, 0, 100, 100)),
            ("b", new Rectangle(0, 0, 100, 100))
        };

        Assert.Single(UIOverlapDebugger.FindCollisions(items));
    }

    [Theory]
    [InlineData(0, 0, 100, 100, 100, 0, 100, 100)]   // 贴边
    [InlineData(0, 0, 300, 300, 50, 50, 100, 100)]   // 包含
    [InlineData(0, 0, 100, 100, 200, 200, 100, 100)] // 分离
    public void IsOverlapping_ReturnsFalseForNonCollidingRectangles(
        int ax, int ay, int aw, int ah, int bx, int by, int bw, int bh)
    {
        Assert.False(UIOverlapDebugger.IsOverlapping(
            new Rectangle(ax, ay, aw, ah), new Rectangle(bx, by, bw, bh)));
    }

    [Fact]
    public void IsContained_DistinguishesContainmentFromOverlap()
    {
        var inner = new Rectangle(10, 10, 50, 50);
        var outer = new Rectangle(0, 0, 100, 100);

        Assert.True(UIOverlapDebugger.IsContained(outer, inner));
        Assert.True(UIOverlapDebugger.IsContained(outer, new Rectangle(0, 0, 100, 50))); // 贴边包含
        Assert.False(UIOverlapDebugger.IsContained(inner, outer));
        Assert.False(UIOverlapDebugger.IsContained(outer, outer)); // 自身不算包含
    }
}
