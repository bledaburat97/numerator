using Game;
using UnityEngine;

namespace Scripts
{
    public enum LevelGroup { Normal, Hard, Assisted, Bonus }

    // Describes actual content. Number-ending distribution is deliberately not enabled.
    public readonly struct LevelIdentity
    {
        public readonly LevelGroup Group;
        public readonly string Name;
        public readonly Color Background, Panel, Accent;
        public readonly FruitLabGlyph.Symbol Symbol;
        private LevelIdentity(LevelGroup group,string name,string background,string panel,string accent,FruitLabGlyph.Symbol symbol)
        {Group=group;Name=name;Background=Hex(background);Panel=Hex(panel);Accent=Hex(accent);Symbol=symbol;}
        private static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var color);return color;}
        public static LevelIdentity For(LevelData level)
        {
            level ??= new LevelData();
            if(level.BonusReward.HasValue)return new LevelIdentity(LevelGroup.Bonus,"Bonus Bölüm","2B2338","44344E","B79AD3",FruitLabGlyph.Symbol.Gift);
            if(level.Mechanic==LevelMechanic.Helper || level.Mechanic==LevelMechanic.Pair || level.Mechanic==LevelMechanic.Split)
                return new LevelIdentity(LevelGroup.Assisted,level.Mechanic==LevelMechanic.Helper?"Bonus İpucu":level.Mechanic==LevelMechanic.Pair?"Bağlı Meyveler":"Bölünmüş Tahta",
                    "172F3C","294957","86B7CE",level.Mechanic==LevelMechanic.Helper?FruitLabGlyph.Symbol.Helper:level.Mechanic==LevelMechanic.Pair?FruitLabGlyph.Symbol.Pair:FruitLabGlyph.Symbol.Split);
            if(level.Mechanic==LevelMechanic.Ice || level.Mechanic==LevelMechanic.Growing || level.Mechanic==LevelMechanic.Dynamite || level.NumOfBoardHolders==5)
                return new LevelIdentity(LevelGroup.Hard,level.Mechanic==LevelMechanic.Ice?"Buzlu Tahta":level.Mechanic==LevelMechanic.Growing?"Büyüyen Tahta":level.Mechanic==LevelMechanic.Dynamite?"Dinamitli Tahta":"Zor Bölüm",
                    "352329","4C3338","CC8B86",level.Mechanic==LevelMechanic.Ice?FruitLabGlyph.Symbol.Ice:level.Mechanic==LevelMechanic.Growing?FruitLabGlyph.Symbol.Growing:level.Mechanic==LevelMechanic.Dynamite?FruitLabGlyph.Symbol.Dynamite:FruitLabGlyph.Symbol.Challenge);
            return new LevelIdentity(LevelGroup.Normal,"Normal Bölüm","192F29","2B463C","94C3A6",FruitLabGlyph.Symbol.Leaf);
        }
    }
}
