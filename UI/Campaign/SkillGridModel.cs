using System;
using System.Collections.Generic;
using System.Linq;
using Fida.Logic;

namespace Fida.UI.Campaign;

public enum SkillCategory { All, Offense, Defense, Control }

/// <summary>Filtering, paging and spatial focus; independent of graphics and campaign state.</summary>
public sealed class SkillGridModel
{
    private readonly SkillType[] _all;
    private SkillType[] _filtered;
    private int _index;
    public SkillCategory Category { get; private set; }
    public SkillType Focused => _filtered[_index];
    public int FocusedIndex => Array.IndexOf(_all, Focused);
    public int PageSize { get; private set; } = 12;
    public int Page => _index / PageSize;
    public int PageCount => Math.Max(1, (_filtered.Length + PageSize - 1) / PageSize);
    public IReadOnlyList<SkillType> All => _all;
    public IReadOnlyList<SkillType> PageItems => _filtered.Skip(Page * PageSize).Take(PageSize).ToArray();

    public SkillGridModel(IReadOnlyList<SkillType> skills, SkillType? initial = null)
    {
        if (skills == null || skills.Count == 0) throw new ArgumentException("A skill grid needs skills.", nameof(skills));
        SkillType[] order = { SkillType.PlusAttack,SkillType.DoubleAttack,SkillType.PlusDefence,SkillType.DoubleDefence,
            SkillType.DoubleHealth,SkillType.TripleHealth,SkillType.Xue,SkillType.Shi,SkillType.Shen,SkillType.Xian,
            SkillType.Long,SkillType.Bian,SkillType.Tong,SkillType.Mo,SkillType.Seng,SkillType.Dao,SkillType.Di,
            SkillType.SuperGod,SkillType.SuperImmortal,SkillType.SuperEmperor };
        _all = skills.Distinct().OrderBy(skill => Array.IndexOf(order,skill) is int rank && rank>=0 ? rank : 1000+(int)skill).ToArray();
        _filtered = _all;
        if (initial.HasValue) Focus(initial.Value);
    }

    public void SetPageSize(int size) => PageSize = Math.Max(1, size);
    public bool Focus(SkillType skill)
    {
        int index = Array.IndexOf(_filtered, skill);
        if (index < 0) return false;
        _index = index;
        return true;
    }
    public void Filter(SkillCategory category)
    {
        SkillType previous = Focused;
        var filtered = _all.Where(skill => category == SkillCategory.All || CategoryOf(skill) == category).ToArray();
        if (filtered.Length == 0) return;
        Category = category;
        _filtered = filtered;
        _index = Math.Max(0, Array.IndexOf(_filtered, previous));
    }
    public void Move(int steps) => _index = Math.Clamp(_index + steps, 0, _filtered.Length - 1);
    public void MoveSpatial(int dx, int dy, int columns)
    {
        if (dy != 0) Move(dy * columns);
        else if (dx < 0 && _index % columns > 0) Move(-1);
        else if (dx > 0 && _index % columns < columns - 1) Move(1);
    }
    public void ChangePage(int delta)
    {
        int page = Math.Clamp(Page + delta, 0, PageCount - 1);
        _index = Math.Min(_filtered.Length - 1, page * PageSize + _index % PageSize);
    }
    public static SkillCategory CategoryOf(SkillType skill) => skill switch
    {
        SkillType.PlusAttack or SkillType.DoubleAttack or SkillType.Bian or SkillType.Long => SkillCategory.Offense,
        SkillType.PlusDefence or SkillType.DoubleDefence or SkillType.DoubleHealth or SkillType.TripleHealth or SkillType.Shi => SkillCategory.Defense,
        _ => SkillCategory.Control
    };
}
