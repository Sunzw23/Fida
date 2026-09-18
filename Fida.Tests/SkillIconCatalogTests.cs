using Fida.Logic;
using Fida.UI;
using Xunit;

namespace Fida.Tests;

public class SkillIconCatalogTests
{
    [Fact]
    public void EverySelectableSkillHasAVisibleGlyph()
    {
        foreach (SkillType skill in SkillFactory.SelectableSkills)
        {
            string glyph = UISkillIconCatalog.Glyph(skill);
            Assert.False(string.IsNullOrWhiteSpace(glyph));
            Assert.NotEqual("?", glyph);
        }
    }
}
