using System.Collections.Generic;

namespace DeskWarrior.Models
{
    /// <summary>
    /// 몬스터 데이터 정의 (JSON에서 로드)
    /// </summary>
    public class MonsterData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int BaseHp { get; set; }
        public int HpGrowth { get; set; }
        public int BaseGold { get; set; }
        public int GoldGrowth { get; set; }
        public string Emoji { get; set; } = "👹";
    }

    /// <summary>
    /// 영웅 데이터 정의 (JSON에서 로드)
    /// </summary>
    public class HeroData
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// CharacterData.json 루트 구조
    /// </summary>
    public class CharacterDataRoot
    {
        public List<MonsterData> Monsters { get; set; } = new();
        public List<MonsterData> Bosses { get; set; } = new();
        public List<HeroData> Heroes { get; set; } = new();
    }
}
