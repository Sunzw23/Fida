using System;
using System.Collections.Generic;
using Fida.Logic;

namespace Fida.UI.Campaign
{
    /// <summary>Pure circular selection state, independent from rendering and input devices.</summary>
    public sealed class SkillWheelModel
    {
        private readonly IReadOnlyList<SkillType> _skills;

        public int FocusedIndex { get; private set; }
        public int Count => _skills.Count;
        public SkillType Focused => GetAtOffset(0);

        public SkillWheelModel(IReadOnlyList<SkillType> skills, SkillType? initial = null)
        {
            _skills = skills ?? throw new ArgumentNullException(nameof(skills));
            if (_skills.Count == 0) throw new ArgumentException("A skill wheel requires at least one item.", nameof(skills));
            if (initial.HasValue) Focus(initial.Value);
        }

        public void Move(int steps)
        {
            if (_skills.Count == 0 || steps == 0) return;
            FocusedIndex = Mod(FocusedIndex + steps, _skills.Count);
        }

        public bool Focus(SkillType skill)
        {
            for (int i = 0; i < _skills.Count; i++)
            {
                if (_skills[i] != skill) continue;
                FocusedIndex = i;
                return true;
            }
            return false;
        }

        public SkillType GetAtOffset(int offset) => _skills[Mod(FocusedIndex + offset, _skills.Count)];

        private static int Mod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }
}
