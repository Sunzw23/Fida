using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Fida
{
    



    public class GameConfig
    {
        public static List<SkillType> GameStartSkills => [SkillType.TripleHealth];
        public static List<SkillType> TurnStartSkills => [SkillType.Tong, SkillType.Di];
        public static List<SkillType> OrderingAttackSkills => [SkillType.Long, SkillType.DoubleAttack];

        public static List<SkillType> DamagingSkills => [SkillType.Mo];
        public static List<SkillType> DamagedSkills => [SkillType.Shen];
    }

    
}
