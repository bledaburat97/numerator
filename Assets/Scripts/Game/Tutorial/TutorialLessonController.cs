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
        public int Revision;
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
        public const int CurrentRevision=2;
        private readonly List<TutorialLessonStep> _steps;
        private readonly TutorialLessonState _state;
        private readonly Action _checkpoint;
        public event Action<TutorialLessonStep> Changed;
        public TutorialLessonStep Current=>_state.Completed || _state.Step>=_steps.Count?null:_steps[_state.Step];
        public TutorialLessonState State=>_state;
        public TutorialLessonController(TutorialLessonId lesson,TutorialLessonState state,Action checkpoint,IReadOnlyList<int> firstGuess=null)
        {
            _state=state;_checkpoint=checkpoint;
            if(state.Lesson!=lesson){state.Lesson=lesson;state.Step=0;state.Completed=false;}
            int revision=lesson==TutorialLessonId.PositionNote || lesson==TutorialLessonId.Reveal?CurrentRevision:1;
            if(state.Revision!=revision && !state.Completed)
            {
                if(state.Revision==1 && lesson==TutorialLessonId.PositionNote && firstGuess?.Count==3)state.Step=Math.Max(1,state.Step-4);
                else if(state.Revision==1 && lesson==TutorialLessonId.Reveal)state.Step=Math.Max(0,state.Step-1);
                else state.Step=0;
            }
            state.Revision=revision;
            _steps=Build(lesson,firstGuess);
            if(((lesson==TutorialLessonId.EliminationNote && firstGuess?.Count==2)
                || (lesson==TutorialLessonId.PositionNote && firstGuess?.Count==3)) && state.Step==0)state.Step=1;
            if(state.Step<0 || state.Step>=_steps.Count)state.Completed=true;
        }
        public void Start()=>Changed?.Invoke(Current);
        public void BindFirstGuess(IReadOnlyList<int> guess)
        {
            if(!(_state.Lesson==TutorialLessonId.EliminationNote && guess?.Count==2)
                && !(_state.Lesson==TutorialLessonId.PositionNote && guess?.Count==3))return;
            _steps.Clear();_steps.AddRange(Build(_state.Lesson,guess));
        }
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
            if(lesson==TutorialLessonId.Basics)return new[]{4,1};
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
            if(level.Lesson==TutorialLessonId.PositionNote && level.NumOfBoardHolders!=3)return false;
            if(level.Lesson==TutorialLessonId.EliminationNote && (level.NumOfBoardHolders!=2 || level.NumOfCards<4))return false;
            var answer=PreparedAnswer(level.Lesson);
            if(answer!=null && (answer.Length!=level.NumOfBoardHolders || Array.Exists(answer,fruit=>fruit>level.NumOfCards)))return false;
            if(level.Lesson==TutorialLessonId.Basics && level.NumOfCards!=5)return false;
            if(level.Lesson==TutorialLessonId.NoPowerUps && !level.DisablePowerUps)return false;
            return true;
        }
        private static List<TutorialLessonStep> Build(TutorialLessonId lesson,IReadOnlyList<int> firstGuess=null)
        {
            var steps=new List<TutorialLessonStep>();
            void Message(string title,string text,LessonTarget target=LessonTarget.Board,int slot=-1)
                =>steps.Add(new TutorialLessonStep {Title=title,Text=text,Target=target,Slot=slot,Expected=LessonSignal.Continue});
            void Wait(LessonSignal signal)=>steps.Add(new TutorialLessonStep {Expected=signal,Wait=true});
            void Power(RewardType type)
            {
                var definition=PowerUpDefinition.Get(type);
                // These effects need only one guided popup. Inspect waits until a
                // real guess exists; no empty or unusable action is presented.
                if(type==RewardType.Helper || type==RewardType.Ultra || type==RewardType.Split || type==RewardType.Revealing)
                {
                    if(type==RewardType.Helper)Wait(LessonSignal.Available);
                    steps.Add(new TutorialLessonStep {
                        Title=type==RewardType.Helper?"İpucu Güçlendiricisi":type==RewardType.Ultra?"Yardım Güçlendiricisi":type==RewardType.Revealing?"Göster Güçlendiricisi":"Bölme Güçlendiricisi",
                        Text=type==RewardType.Helper?"Kullanıldığında son tahmindeki meyvelerle ilgili ipucu gösterir.":type==RewardType.Ultra?"Konum notları otomatik hesaplanır":type==RewardType.Revealing?"İkona dokun, sonra bir alan seç. Doğru meyve yerleşir ve sabitlenir.":definition.Description,
                        Target=LessonTarget.Power,Power=type,Expected=LessonSignal.PowerApplied});
                    if(type==RewardType.Revealing)Message("Etkisini gördün","Doğru meyve yerleşti ve sabitlendi.");
                    return;
                }
                Message(type==RewardType.Pair?"Bağlama Güçlendiricisi":definition.Name,definition.Description,LessonTarget.Power);
                steps[steps.Count-1].Power=type;
                if(type==RewardType.Life)Wait(LessonSignal.Available);
                steps.Add(new TutorialLessonStep {Title=type==RewardType.Pair?"Bağlayalım":definition.Name,Text=type==RewardType.Revealing?"İkona dokun, sonra açık bir alan seç."
                    :"Güçlendirici ikonuna dokun.",Target=LessonTarget.Power,Power=type,Expected=LessonSignal.PowerApplied});
                Message(type==RewardType.Pair?"Bağlandı":"Etkisini gördün",type==RewardType.Life?"2 tahmin hakkı eklendi."
                    :type==RewardType.Pair?"Bu meyveler cevapta bulunuyorlar ve ardışıklar."
                    :type==RewardType.Bomb?"Son alan kaldırıldı. Artık daha kısa cevabı arıyorsun.":"Doğru meyve yerleşti ve sabitlendi.",
                    type==RewardType.Life?LessonTarget.Life:type==RewardType.Pair?LessonTarget.Pair:LessonTarget.Board);
            }
            switch(lesson)
            {
                case TutorialLessonId.EliminationNote:
                    Wait(LessonSignal.Guess);
                    for(int i=0;i<2;i++)
                    {
                        int fruit=firstGuess?.Count==2?firstGuess[i]-1:i;
                        steps.Add(new TutorialLessonStep {Title="Meyveye Uzun Basalım",Text="Basılı tut.",Target=LessonTarget.Card,Card=fruit,Expected=LessonSignal.NoteOpened});
                        steps.Add(new TutorialLessonStep {Title="Bu Meyveyi Eleyelim",Text="Kırmızıya dokun.",Target=LessonTarget.NoteRed,Card=fruit,Expected=LessonSignal.FruitExcluded});
                    }
                    break;
                case TutorialLessonId.PositionNote:
                    Wait(LessonSignal.Guess);
                    Message("Meyveler başka yerde","0 doğru, 3 yanlış yer: meyvelerin hiçbiri denediğin konumda olamaz.",LessonTarget.Result);
                    string[] fruitNames={"Elma","Muz","Çilek","Portakal","Üzüm","Yaban mersini","Armut","Karpuz","Kivi"};
                    string[] positions={"Birinci","İkinci","Üçüncü"};
                    for(int i=0;i<3;i++)
                    {
                        int fruit=firstGuess?.Count==3?firstGuess[i]-1:new[]{1,2,0}[i];
                        steps.Add(new TutorialLessonStep {Title=fruitNames[fruit]+" için konum notu",Text="Meyveye basılı tut.",Target=LessonTarget.Card,Card=fruit,Expected=LessonSignal.NoteOpened});
                        steps.Add(new TutorialLessonStep {Title=positions[i]+" konum olamaz",Text="Tahtanın altındaki "+(i+1)+" numarasına dokun."+(i==0?" Bu X yalnızca o konumu eler.":""),Target=LessonTarget.NotePosition,Card=fruit,Slot=i,Expected=LessonSignal.PositionExcluded});
                    }
                    Message("Meyveyi değil, yerini eledin","İşaretlediğin konumlar artık X. Diğer konumlar hâlâ aday.",LessonTarget.Card);
                    steps[steps.Count-1].Card=firstGuess?.Count==3?firstGuess[0]-1:1;break;
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
                    Wait(LessonSignal.Helper);break;
                case TutorialLessonId.Dynamite:
                    Message("Dinamitli Tahta","İlk 4 hamlede dinamitli alan için doğru meyveyi bulmalısın yoksa oyun kaybedilir.");
                    Wait(LessonSignal.Disarmed);break;
                case TutorialLessonId.PairLevel:
                    Message("Ardışık Bağlı Meyveler","İple bağlı meyveler cevapta yan yana. Hangisinin önce geldiğini sen bulursun.",LessonTarget.Pair);break;
                case TutorialLessonId.Ice:
                    Message("Buzlu Tahta","İki tahminde bir son alan buz tutar ve oraya tahmin yapamazsın.");
                    Wait(LessonSignal.Frozen);Message("Buzlanma","Buzlanan alanının gizli meyvesi cevapta kalır, başka alanda denenirse bilgisi verilir. Bu tur {open} kart ile tahmin yapın.",LessonTarget.Holder,-2);
                    Wait(LessonSignal.FrozenGuess);Message("Buzlu Tahmin","Buzlu tur bilgi toplamak amaçlıdır, doğru tahmini buzun olmadığı turda yapmalısın.",LessonTarget.Result);break;
                case TutorialLessonId.SplitLevel:
                    Message("Bölünmüş Tahta","İki grubun sonuçları ayrı hesaplanır, yerleştirilen meyveler kendi grubunda değerlendirilir.");
                    Wait(LessonSignal.Guess);Message("İki ayrı sonuç","Bir meyve diğer grupta olsa da bu grupta yanlış yer sayılmaz.",LessonTarget.Result);break;
                case TutorialLessonId.Bonus:
                    Message("Bonus Bölüm","Kazanınca doğrudan bir güçlendirici alırsın");break;
                case TutorialLessonId.NoPowerUps:
                    Message("Güçlendiricisiz Bölüm","Güçlendiricilere bu bölüm erişilemiyor.");break;
            }
            return steps;
        }
    }
    // Only the free opening guess of the elimination lesson is adaptive. Once
    // chosen, the real answer is fixed and is saved with the ordinary round.
    public static class EliminationTutorialAnswer
    {
        public static List<int> Choose(LevelData definition,IReadOnlyList<int> guess,int previousGuesses)
        {
            if(definition?.Lesson!=TutorialLessonId.EliminationNote || previousGuesses!=0 || guess.Count!=2)return null;
            var answer=new List<int>();
            for(int fruit=1;fruit<=definition.NumOfCards && answer.Count<2;fruit++)
                if(fruit!=guess[0] && fruit!=guess[1])answer.Add(fruit);
            return answer.Count==2?answer:null;
        }
    }
    public static class PositionTutorialAnswer
    {
        public static List<int> Choose(LevelData definition,IReadOnlyList<int> guess,int previousGuesses)
        {
            if(definition?.Lesson!=TutorialLessonId.PositionNote || previousGuesses!=0 || guess?.Count!=3)return null;
            // Keep all three chosen fruits, with no fruit at its attempted position.
            // The real answer is committed before feedback/checkpoint processing.
            for(int i=0;i<3;i++)if(guess[i]<1 || guess[i]>definition.NumOfCards || guess[i]==guess[(i+1)%3])return null;
            return new List<int>{guess[1],guess[2],guess[0]};
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
