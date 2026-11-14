using System;
using System.Linq;
using System.Collections.Generic;
using Game;

namespace Scripts
{
    // Presentation and gameplay parameters share one catalogue. Saved RewardType ids stay unchanged.
    public sealed class PowerUpDefinition
    {
        public RewardType Type { get; }
        public GameUIButtonType ButtonType { get; }
        public bool NeedsBoardTarget { get; }
        public string Name { get; }
        public string Description { get; }
        public string IconName { get; }
        public string TutorialKey { get; }
        public int ExtraGuesses { get; }
        private readonly bool _unscheduled;
        private PowerUpDefinition(RewardType type,GameUIButtonType button,string name,string description,string icon,string tutorial,int guesses=0,bool unscheduled=false,bool boardTarget=false)
        {Type=type;ButtonType=button;NeedsBoardTarget=boardTarget;Name=name;Description=description;IconName=icon;TutorialKey=tutorial;ExtraGuesses=guesses;_unscheduled=unscheduled;}

        public int UnlockLevel
        {
            get
            {
                if(LevelPlaytestSchedule.Settings.Enabled)return LevelPlaytestSchedule.IntroductionLevel(Type);
                if(_unscheduled)return int.MaxValue;
                var level=LevelDataGetter.GetStartingLevelsData().Find(item=>item.NumOfBoardHolders>2);
                return level==null?int.MaxValue:level.LevelId+1;
            }
        }
        private static readonly PowerUpDefinition[] All={
            new PowerUpDefinition(RewardType.Revealing,GameUIButtonType.RevealingPowerUp,"Göster","Bir alan seç. Doğru meyve oraya yerleşir ve sabitlenir.","Reveal",TutorialProgress.Reveal,boardTarget:true),
            new PowerUpDefinition(RewardType.Life,GameUIButtonType.LifePowerUp,"Can","2 ek tahmin hakkı verir. 2 hak harcadığında kullanabilirsin.","Life",TutorialProgress.Life,2),
            new PowerUpDefinition(RewardType.Bomb,GameUIButtonType.BombPowerUp,"Bomba","İlk tahminden önce son alanı kaldırır.","Bomb",TutorialProgress.Bomb),
            new PowerUpDefinition(RewardType.Helper,GameUIButtonType.HelperPowerUp,"İpucu","Son tahminindeki meyvelerin ayrıntısını gösterir.","Helper",TutorialProgress.Helper,unscheduled:true),
            new PowerUpDefinition(RewardType.Pair,GameUIButtonType.PairPowerUp,"Bağlı","Cevapta yan yana bulunan iki meyveyi bağlar.","Pair",TutorialProgress.Pair,unscheduled:true),
            new PowerUpDefinition(RewardType.Split,GameUIButtonType.SplitPowerUp,"Bölme","İlk tahminden önce tahtayı böler. Sonuçlar ayrı hesaplanır.","Split",TutorialProgress.Split,unscheduled:true),
            new PowerUpDefinition(RewardType.Ultra,GameUIButtonType.UltraPowerUp,"Ultra","Bu bölümde konum notlarını otomatik hesaplar.","Ultra",TutorialProgress.Ultra)
        };
        public static IReadOnlyList<RewardType> RewardOrder {get;}=Array.AsReadOnly(All.Select(definition=>definition.Type).ToArray());
        // Visual slots follow introduction order; reward rotation and saved ids stay unchanged.
        public static IReadOnlyList<RewardType> DisplayOrder {get;}=Array.AsReadOnly(new[]{
            RewardType.Revealing,RewardType.Life,RewardType.Bomb,RewardType.Helper,RewardType.Pair,RewardType.Ultra,RewardType.Split});
        public static bool TryGet(GameUIButtonType button,out PowerUpDefinition definition)
        {definition=All.FirstOrDefault(item=>item.ButtonType==button);return definition!=null;}
        public static PowerUpDefinition Get(RewardType type)
        {foreach(var definition in All)if(definition.Type==type)return definition;throw new ArgumentOutOfRangeException(nameof(type));}
    }
}
