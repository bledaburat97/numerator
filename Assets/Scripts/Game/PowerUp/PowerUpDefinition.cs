using System;
using Game;

namespace Scripts
{
    // Presentation and gameplay parameters share one catalogue. Saved RewardType ids stay unchanged.
    public sealed class PowerUpDefinition
    {
        public RewardType Type { get; }
        public string Name { get; }
        public string Description { get; }
        public string IconName { get; }
        public string TutorialKey { get; }
        public int ExtraGuesses { get; }
        private readonly bool _unscheduled;
        private PowerUpDefinition(RewardType type,string name,string description,string icon,string tutorial,int guesses=0,bool unscheduled=false)
        {Type=type;Name=name;Description=description;IconName=icon;TutorialKey=tutorial;ExtraGuesses=guesses;_unscheduled=unscheduled;}

        public int UnlockLevel
        {
            get
            {
                if(LevelPlaytestSchedule.Settings.Enabled)return LevelPlaytestSchedule.Settings.StartLevel;
                if(_unscheduled)return int.MaxValue;
                var level=LevelDataGetter.GetStartingLevelsData().Find(item=>item.NumOfBoardHolders>2);
                return level==null?int.MaxValue:level.LevelId+1;
            }
        }
        private static readonly PowerUpDefinition[] All={
            new PowerUpDefinition(RewardType.Revealing,"Göster","Bir alan seç. Doğru meyve oraya yerleşir ve sabitlenir.","Reveal",TutorialProgress.Reveal),
            new PowerUpDefinition(RewardType.Life,"Can","2 ek tahmin hakkı verir. 2 hak harcadığında kullanabilirsin.","Life",TutorialProgress.Life,2),
            new PowerUpDefinition(RewardType.Bomb,"Bomba","İlk tahminden önce son alanı kaldırır.","Bomb",TutorialProgress.Bomb),
            new PowerUpDefinition(RewardType.Helper,"İpucu","Son tahminindeki meyvelerin ayrıntısını gösterir.","Helper",TutorialProgress.Helper,unscheduled:true),
            new PowerUpDefinition(RewardType.Pair,"Bağlı","Cevapta yan yana bulunan iki meyveyi bağlar.","Pair",TutorialProgress.Pair,unscheduled:true),
            new PowerUpDefinition(RewardType.Split,"Bölme","İlk tahminden önce tahtayı böler. Sonuçlar ayrı hesaplanır.","Split",TutorialProgress.Split,unscheduled:true),
            new PowerUpDefinition(RewardType.Ultra,"Ultra","Bu bölümde konum notlarını otomatik hesaplar.","Ultra",TutorialProgress.Ultra)
        };
        public static PowerUpDefinition Get(RewardType type)
        {foreach(var definition in All)if(definition.Type==type)return definition;throw new ArgumentOutOfRangeException(nameof(type));}
    }
}
