using System;
using System.Collections.Generic;
using Game;

namespace Scripts
{
    public enum TutorialLessonId
    {
        None, Basics, EliminationNote, Crystal, PositionNote, Reveal, Life, Bomb, Growing,
        HelperLevel, Inspect, Bonus, Dynamite, PairLevel, Pair, Ultra, Ice, SplitLevel, Split, NoPowerUps
    }

    [Serializable]
    public sealed class TutorialLessonState
    {
        public TutorialLessonId Lesson;
        public int Step;
        public bool Completed;
        public TutorialLessonState Copy()=>(TutorialLessonState)MemberwiseClone();
    }

    public enum LessonSignal { Continue, Placed, Guess, NoteOpened, FruitExcluded, PositionExcluded, Available, PowerApplied, Growth, Helper, Disarmed, Frozen, FrozenGuess }
    public enum LessonTarget { None, Card, Holder, Guess, Result, NoteRed, NotePosition, Power, Life, Crystal, Pair, Board, Initial }
    public sealed class TutorialLessonStep
    {
        public string Title,Text;
        public LessonTarget Target;
        public LessonSignal Expected;
        public int Card=-1,Slot=-1;
        public bool Wait,Drag;
        public RewardType? Power;
    }

    // Owns teaching progress only. Validated gameplay sends signals; the lesson never
    // evaluates a guess, reveals a hidden answer, spends stock or awards a reward.
    public sealed class TutorialLessonController
    {
        private readonly List<TutorialLessonStep> _steps;
        private readonly TutorialLessonState _state;
        private readonly Action _checkpoint;
        public event Action<TutorialLessonStep> Changed;
        public TutorialLessonStep Current=>_state.Completed || _state.Step>=_steps.Count?null:_steps[_state.Step];
        public TutorialLessonState State=>_state;
        public TutorialLessonController(TutorialLessonId lesson,TutorialLessonState state,Action checkpoint)
        {
            _state=state;_checkpoint=checkpoint;
            if(state.Lesson!=lesson){state.Lesson=lesson;state.Step=0;state.Completed=false;}
            _steps=Build(lesson);
            if(state.Step<0 || state.Step>=_steps.Count)state.Completed=true;
        }
        public void Start()=>Changed?.Invoke(Current);
        public void RewindUnplacedCards(bool keepPlacements=false)
        {
            var step=Current;if(step==null)return;
            if(!keepPlacements && (step.Expected==LessonSignal.Placed || step.Expected==LessonSignal.Guess))
                while(_state.Step>0 && _steps[_state.Step-1].Expected==LessonSignal.Placed)_state.Step--;
            else if(step.Expected==LessonSignal.FruitExcluded || step.Expected==LessonSignal.PositionExcluded)
                if(_state.Step>0 && _steps[_state.Step-1].Expected==LessonSignal.NoteOpened)_state.Step--;
        }
        public bool Accept(LessonSignal signal,int card=-1,int slot=-1)
        {
            var step=Current;
            bool positioned=signal==LessonSignal.Placed || signal==LessonSignal.NoteOpened || signal==LessonSignal.FruitExcluded || signal==LessonSignal.PositionExcluded;
            if(step==null || step.Expected!=signal || (positioned && step.Card>=0 && step.Card!=card)
                || (positioned && signal!=LessonSignal.NoteOpened && step.Slot>=0 && step.Slot!=slot))return false;
            _state.Step++;
            if(_state.Step>=_steps.Count)_state.Completed=true;
            _checkpoint?.Invoke();Changed?.Invoke(Current);return true;
        }
        public void Dismiss(){if(_state.Completed)return;_state.Completed=true;_checkpoint?.Invoke();Changed?.Invoke(null);}

        // Only the explicitly prepared early examples use fixed answers. Every other
        // lesson plays an ordinary round and waits for its real effect/availability.
        public static int[] PreparedAnswer(TutorialLessonId lesson)
        {
            if(lesson==TutorialLessonId.Basics || lesson==TutorialLessonId.EliminationNote)return new[]{4,1};
            if(lesson==TutorialLessonId.PositionNote)return new[]{1,2,3};
            return null;
        }
        public static bool IsBasics(LevelData definition,bool legacyFirst)=>definition?.Lesson==TutorialLessonId.Basics
            || ((definition?.Lesson??TutorialLessonId.None)==TutorialLessonId.None && legacyFirst);
        public static TutorialLessonId ForPower(RewardType type)
        {
            switch(type){case RewardType.Bomb:return TutorialLessonId.Bomb;case RewardType.Revealing:return TutorialLessonId.Reveal;
                case RewardType.Life:return TutorialLessonId.Life;case RewardType.Helper:return TutorialLessonId.Inspect;
                case RewardType.Pair:return TutorialLessonId.Pair;case RewardType.Split:return TutorialLessonId.Split;default:return TutorialLessonId.Ultra;}
        }
        public static RewardType? PowerFor(TutorialLessonId lesson)
        {
            foreach(RewardType type in Enum.GetValues(typeof(RewardType)))if(ForPower(type)==lesson)return type;
            return null;
        }
        public static bool Fits(LevelData level)
        {
            if(level.Lesson==TutorialLessonId.Crystal && (level.NumOfBoardHolders!=3 || level.NumOfCards<5))return false;
            var answer=PreparedAnswer(level.Lesson);
            if(answer!=null && (answer.Length!=level.NumOfBoardHolders || Array.Exists(answer,fruit=>fruit>level.NumOfCards)))return false;
            if(level.Lesson==TutorialLessonId.Basics && level.NumOfCards!=5)return false;
            if(level.Lesson==TutorialLessonId.NoPowerUps && !level.DisablePowerUps)return false;
            return true;
        }
        private static List<TutorialLessonStep> Build(TutorialLessonId lesson)
        {
            var steps=new List<TutorialLessonStep>();
            void Message(string title,string text,LessonTarget target=LessonTarget.Board,int slot=-1)
                =>steps.Add(new TutorialLessonStep {Title=title,Text=text,Target=target,Slot=slot,Expected=LessonSignal.Continue});
            void Wait(LessonSignal signal)=>steps.Add(new TutorialLessonStep {Expected=signal,Wait=true});
            void Place(int card,int slot)=>steps.Add(new TutorialLessonStep {Title="Meyveyi yerleştir",Text="İşaretli meyveyi alana taşı.",
                Target=LessonTarget.Card,Card=card,Slot=slot,Drag=true,Expected=LessonSignal.Placed});
            void Guess()=>steps.Add(new TutorialLessonStep {Title="Tahminini gönder",Text="Sonucu birlikte okuyalım.",Target=LessonTarget.Guess,Expected=LessonSignal.Guess});
            void Power(RewardType type)
            {
                var definition=PowerUpDefinition.Get(type);
                Message(definition.Name,definition.Description,LessonTarget.Power);
                steps[steps.Count-1].Power=type;
                if(type==RewardType.Life || type==RewardType.Helper)Wait(LessonSignal.Available);
                steps.Add(new TutorialLessonStep {Title=definition.Name,Text=type==RewardType.Revealing?"İkona dokun, sonra açık bir alan seç."
                    :"Güçlendirici ikonuna dokun.",Target=LessonTarget.Power,Power=type,Expected=LessonSignal.PowerApplied});
                Message("Etkisini gördün",type==RewardType.Life?"2 tahmin hakkı eklendi.":type==RewardType.Helper?"Ayrıntılar yalnızca son tahmininde gösterilir."
                    :type==RewardType.Ultra?"Konum notları bu bölüm boyunca otomatik güncellenir."
                    :type==RewardType.Pair?"İp, cevapta komşu olan meyveleri gösterir. Sıralarını sen bulursun."
                    :type==RewardType.Split?"Her grup kendi cevabına göre değerlendirilir."
                    :type==RewardType.Bomb?"Son alan kaldırıldı. Artık daha kısa cevabı arıyorsun.":"Doğru meyve yerleşti ve sabitlendi.",
                    type==RewardType.Life?LessonTarget.Life:type==RewardType.Helper?LessonTarget.Result:type==RewardType.Pair?LessonTarget.Pair:type==RewardType.Ultra?LessonTarget.Initial:LessonTarget.Board);
            }
            switch(lesson)
            {
                case TutorialLessonId.EliminationNote:
                    Message("Meyve eleme notu","Bu örnekte iki meyveyi deneyip sonucu okuyalım.");Place(1,0);Place(2,1);Guess();
                    Message("İkisi de cevapta yok","0 doğru, 0 yanlış yer: denediğin iki meyve de cevapta yok.",LessonTarget.Result);
                    steps.Add(new TutorialLessonStep {Title="Kendi notunu koy",Text="Muza yarım saniye basılı tut.",Target=LessonTarget.Card,Card=1,Expected=LessonSignal.NoteOpened});
                    steps.Add(new TutorialLessonStep {Title="Bu meyve yok",Text="Kırmızıya dokun. Bu işareti istediğinde değiştirebilirsin.",Target=LessonTarget.NoteRed,Card=1,Expected=LessonSignal.FruitExcluded});
                    steps.Add(new TutorialLessonStep {Title="Çilek de cevapta yok",Text="Şimdi çileğe yarım saniye basılı tut.",Target=LessonTarget.Card,Card=2,Expected=LessonSignal.NoteOpened});
                    steps.Add(new TutorialLessonStep {Title="Çileği de ele",Text="Kırmızıya dokun.",Target=LessonTarget.NoteRed,Card=2,Expected=LessonSignal.FruitExcluded});
                    Message("Notun hazır","Karttaki X, meyvenin cevapta olmadığını düşündüğünü gösterir.",LessonTarget.Card);steps[steps.Count-1].Card=1;break;
                case TutorialLessonId.PositionNote:
                    Message("Konum notları","Bu kez meyveler cevapta var; yerlerini ayırt edeceğiz.");Place(1,0);Place(2,1);Place(0,2);Guess();
                    Message("Meyveler başka yerde","0 doğru, 3 yanlış yer: meyvelerin hiçbiri denediğin konumda olamaz.",LessonTarget.Result);
                    steps.Add(new TutorialLessonStep {Title="Konumunu not et",Text="Muza yarım saniye basılı tut.",Target=LessonTarget.Card,Card=1,Expected=LessonSignal.NoteOpened});
                    steps.Add(new TutorialLessonStep {Title="Birinci konum olamaz",Text="Tahtanın altındaki 1'e dokun. Bu X yalnızca o konumu eler.",Target=LessonTarget.NotePosition,Card=1,Slot=0,Expected=LessonSignal.PositionExcluded});
                    steps.Add(new TutorialLessonStep {Title="Çileğin konumunu ele",Text="Çileğe yarım saniye basılı tut.",Target=LessonTarget.Card,Card=2,Expected=LessonSignal.NoteOpened});
                    steps.Add(new TutorialLessonStep {Title="İkinci konum olamaz",Text="Tahtanın altındaki 2'ye dokun.",Target=LessonTarget.NotePosition,Card=2,Slot=1,Expected=LessonSignal.PositionExcluded});
                    steps.Add(new TutorialLessonStep {Title="Elmanın konumunu ele",Text="Elmaya yarım saniye basılı tut.",Target=LessonTarget.Card,Card=0,Expected=LessonSignal.NoteOpened});
                    steps.Add(new TutorialLessonStep {Title="Üçüncü konum olamaz",Text="Tahtanın altındaki 3'e dokun.",Target=LessonTarget.NotePosition,Card=0,Slot=2,Expected=LessonSignal.PositionExcluded});
                    Message("Meyveyi değil, yerini eledin","Kart altındaki 1 artık X. Diğer konumlar hâlâ aday.",LessonTarget.Card);steps[steps.Count-1].Card=1;break;
                case TutorialLessonId.Crystal:
                    Message("Artık üç alan var","Üç meyvenin yerini bul. Kristali de birlikte kazanalım.",LessonTarget.Holder,2);
                    break;
                case TutorialLessonId.Reveal:Power(RewardType.Revealing);break;
                case TutorialLessonId.Life:Power(RewardType.Life);break;
                case TutorialLessonId.Bomb:Power(RewardType.Bomb);break;
                case TutorialLessonId.Inspect:Power(RewardType.Helper);break;
                case TutorialLessonId.Pair:Power(RewardType.Pair);break;
                case TutorialLessonId.Ultra:Power(RewardType.Ultra);break;
                case TutorialLessonId.Split:Power(RewardType.Split);break;
                case TutorialLessonId.Growing:
                    Message("Büyüyen Tahta","2 gizli meyve ile başlarsın, her iki tahminden sonra bir gizli meyve daha bulman gerekir.");
                    Wait(LessonSignal.Growth);Message("Yeni alan açıldı","İki yanlış tahmin yaptın. Şimdi bir meyve daha yerleştir.",LessonTarget.Holder,2);break;
                case TutorialLessonId.HelperLevel:
                    Message("Bonus İpucu","Her 3 yanlış tahminde bir son tahmininin ayrıntıları açılır.");
                    Wait(LessonSignal.Helper);Message("İpucun burada","Yeşil: doğru yer. Sarı: başka yer. X: bu meyve cevapta yok.",LessonTarget.Result);break;
                case TutorialLessonId.Dynamite:
                    Message("Dinamitli Tahta","İlk 4 hamlede dinamitli alan için doğru meyveyi bulmalısın yoksa oyun kaybedilir.");
                    Wait(LessonSignal.Disarmed);Message("Dinamit söndü","Doğru meyve bu alanda sabitlendi.");break;
                case TutorialLessonId.PairLevel:
                    Message("Ardışık Bağlı Meyveler","İple bağlı meyveler cevapta yan yana. Hangisinin önce geldiğini sen bulursun.",LessonTarget.Pair);break;
                case TutorialLessonId.Ice:
                    Message("Buzlu Tahta","Bir açık, bir buzlu tahmin. Son hakkın daima açık.");
                    Wait(LessonSignal.Frozen);Message("Bu tur {open} kart","Son alan dondu. Gizli meyvesi cevapta kalır; başka alanda denenirse yanlış yer sayılabilir.",LessonTarget.Holder,-2);
                    Wait(LessonSignal.FrozenGuess);Message("Buz geçmişte kalır","Bu satırın buz simgesi kalıcı. Buzlu turda bilgi toplarsın; kazanmak için açık tur gerekir.",LessonTarget.Result);break;
                case TutorialLessonId.SplitLevel:
                    Message("Bölünmüş Tahta","İki grubun sonuçları ayrı hesaplanır, yerleştirilen meyveler kendi grubunda değerlendirilir.");
                    Wait(LessonSignal.Guess);Message("İki ayrı sonuç","Bir meyve diğer grupta olsa da bu grupta yanlış yer sayılmaz.",LessonTarget.Result);break;
                case TutorialLessonId.Bonus:
                    Message("Bonus Bölüm","Kazanınca doğrudan bir güçlendirici alırsın. Meyveler koleksiyonuna eklenir; kristal ilerlemen korunur.");break;
                case TutorialLessonId.NoPowerUps:
                    Message("Powerupsız Bölüm","Bu bölümde powerup kullanılamaz. Notlarını ve tahmin geçmişini kullanabilirsin.");break;
            }
            return steps;
        }
    }
    public static class CrystalTutorialAnswer
    {
        public static List<int> Choose(LevelData definition,List<int> guess,IReadOnlyList<List<int>> history,IReadOnlyList<int> current,IReadOnlyList<CardItemInfo> knowledge=null)
        {
            if(definition?.Lesson!=TutorialLessonId.Crystal || guess.Count!=3 || definition.NumOfCards<5)return null;
            bool FitsKnowledge(IReadOnlyList<int> candidate)
            {
                if(knowledge==null)return true;
                for(int fruit=0;fruit<knowledge.Count;fruit++)
                {
                    var fact=knowledge[fruit];int slot=-1;
                    for(int i=0;i<candidate.Count;i++)if(candidate[i]==fruit+1)slot=i;
                    if(!fact.isExisted && slot>=0)return false;
                    if(fact.isLocked && (slot<0 || !fact.possibleCardHolderIndicatorIndexes.Contains(slot)))return false;
                }
                return true;
            }
            if(history.Count>0)
            {
                if(!FitsKnowledge(guess))return null;
                foreach(var previous in history)
                {
                    var before=new GuessEvaluation(0,previous,current);var after=new GuessEvaluation(0,previous,guess);
                    if(before.Correct!=after.Correct || before.Misplaced!=after.Misplaced)return null;
                }
                return new List<int>(guess);
            }
            for(int a=1;a<=definition.NumOfCards;a++)for(int b=1;b<=definition.NumOfCards;b++)for(int c=1;c<=definition.NumOfCards;c++)
            {
                if(a==b || a==c || b==c)continue;
                var candidate=new List<int>{a,b,c};var feedback=new GuessEvaluation(0,guess,candidate);
                if(feedback.Correct==0 && feedback.Misplaced==1 && FitsKnowledge(candidate))return candidate;
            }
            return null;
        }
    }
}
