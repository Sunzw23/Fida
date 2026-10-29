using Fida.Campaign;
using Fida.Logic;
using Fida.UI.Campaign;
using Microsoft.Xna.Framework;
using Xunit;

namespace Fida.Tests;

public class SkillGridTests
{
    [Fact]
    public void EverySkillIsReachableAcrossPagesAndCategories()
    {
        var grid = new SkillGridModel(SkillFactory.SelectableSkills);
        var reached = new HashSet<SkillType>();
        for (int page=0; page<grid.PageCount; page++)
        {
            reached.UnionWith(grid.PageItems);
            grid.ChangePage(1);
        }
        Assert.Equal(SkillFactory.SelectableSkills.Count,reached.Count);
        grid.Filter(SkillCategory.Offense);
        Assert.All(grid.PageItems,skill => Assert.Equal(SkillCategory.Offense,SkillGridModel.CategoryOf(skill)));
        Assert.Contains(grid.Focused,grid.PageItems);
        grid.Filter(SkillCategory.All);
        Assert.True(grid.Focus(SkillType.SuperEmperor));
        Assert.Equal(grid.PageCount-1,grid.Page);
        grid.SetPageSize(9);
        Assert.Contains(SkillType.SuperEmperor,grid.PageItems);
    }

    [Fact]
    public void DirectionalFocusDoesNotWrapAcrossRowEdges()
    {
        var grid = new SkillGridModel(SkillFactory.SelectableSkills,SkillType.PlusAttack);
        grid.MoveSpatial(-1,0,4);
        Assert.Equal(SkillType.PlusAttack,grid.Focused);
        grid.MoveSpatial(0,1,4);
        Assert.Equal(SkillType.DoubleHealth,grid.Focused);
        grid.MoveSpatial(0,-1,4);
        Assert.Equal(SkillType.PlusAttack,grid.Focused);
        grid.Move(3);
        var rightEdge = grid.Focused;
        grid.MoveSpatial(1,0,4);
        Assert.Equal(rightEdge,grid.Focused);
    }

    [Theory]
    [InlineData(1920,1200)]
    [InlineData(1600,1000)]
    [InlineData(1280,720)]
    [InlineData(1024,768)]
    [InlineData(1080,2400)]
    [InlineData(720,1280)]
    public void GridDetailsAndFooterHaveSeparateVisibleHitRegions(int width,int height)
    {
        var layout = new SkillSelectionLayout(width,height);
        var screen = new Rectangle(0,0,width,height);
        Assert.True(screen.Contains(layout.Grid));
        Assert.True(screen.Contains(layout.Details));
        Assert.True(screen.Contains(layout.Start));
        Assert.True(layout.Details.Contains(layout.Action));
        Assert.False(layout.Grid.Intersects(layout.Details));
        Assert.False(layout.Grid.Intersects(layout.Footer));
        Assert.False(layout.Details.Intersects(layout.Footer));
        var tiles = Enumerable.Range(0,layout.PageSize).Select(layout.Tile).ToArray();
        foreach (var tile in tiles)
        {
            Assert.True(layout.Grid.Contains(tile));
            Assert.True(tile.Width>=64 && tile.Height>=64);
            Assert.DoesNotContain(tiles,other => other!=tile && other.Intersects(tile));
        }
    }
}
