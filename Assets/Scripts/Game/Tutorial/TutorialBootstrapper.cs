using System;
using System.Linq;
using DG.Tweening;
using Game;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public sealed class TutorialBootstrapper : ITutorialBootstrapper
    {
        [InjectOptional] private ILevelDataCreator _levels;
        [InjectOptional] private ILevelSaveDataManager _save;
        [InjectOptional] private IGameSaveService _saves;
        [InjectOptional] private LazyInject<IGameSaveSnapshotProvider> _snapshots;
        [InjectOptional] private LazyInject<PowerUpUseService> _uses;
        private readonly ILevelTracker _tracker;
        private readonly IInitialCardAreaController _initial;
        private readonly IBoardAreaController _board;
        private readonly ICardPlacementCoordinator _placement;
        private readonly IResultManager _results;
        private readonly IRoundStateManager _round;
        private readonly ICardItemInfoManager _info;
        private readonly IGameUIController _ui;
        private readonly IUnmaskServiceAreaView _mask;
        private readonly IFadePanelController _fade;
        private readonly TutorialInputGate _gate;
        private readonly BaseButtonControllerFactory _buttons;
        private Transform _parent;
        private RectTransform _canvas;
        private HandTutorialView _handPrefab, _hand;
        private TutorialMessagePopupView _messagePrefab, _message;
        private FirstLevelTutorialController _first;
        private bool _ready, _roundEnded;
        private TutorialLessonController _lesson;
        private LevelData _definition;
        private CardAnnotationController _annotations;
        private Sequence _lessonDelay;
        private bool _suspended,_modalSuspended;
        private bool _rewardTipShown,_coinHiddenForTip;
        private bool _basicsRound;
        private bool _resumeInPlace;
        private int _suspendedFirstStep=-1;
        private int _lastGuess=-1,_lastRevision=-1;
        public TutorialLessonController ActiveLesson=>_lesson;
        public void SetAnnotations(CardAnnotationController annotations)=>_annotations=annotations;

        [Inject]
        public TutorialBootstrapper(ILevelTracker tracker, IInitialCardAreaController initial, IBoardAreaController board,
            ICardPlacementCoordinator placement, IResultManager results, IRoundStateManager round, ICardItemInfoManager info,
            IGameUIController ui, IUnmaskServiceAreaView mask, IFadePanelController fade,
            TutorialInputGate gate, BaseButtonControllerFactory buttons)
        {
            _tracker = tracker; _initial = initial; _board = board; _placement = placement; _results = results;
            _round = round; _info = info; _ui = ui; _mask = mask; _fade = fade; _gate = gate; _buttons = buttons;
        }
        public bool IsFirstActive => _first != null && _first.IsActive;
        public void Configure(Transform parent, RectTransform safeArea, RectTransform canvas,
            HandTutorialView handPrefab, TutorialMessagePopupView messagePrefab)
        {
            _parent = parent; _canvas = canvas; _handPrefab = handPrefab; _messagePrefab = messagePrefab;
            _mask.Initialize(_fade);
            _gate.FirstUseHandler = ExplainPowerUp;
        }
        public void PrepareRound()
        {
            Cancel(); _ready = false; _roundEnded = false; _gate.Block();
        }
        public void StartRound()
        {
            bool inPlace=_resumeInPlace;int firstStep=_suspendedFirstStep;
            Cancel(); _ready = true; _roundEnded = false; _rewardTipShown=false; _gate.Clear();
            if (_tracker.GetGameOption() != GameOption.SinglePlayer) return;
            _definition=_levels?.GetLevelData();
            _basicsRound=TutorialLessonController.IsBasics(_definition,_tracker.IsFirstLevelTutorial());
            var pendingLesson=_save?.GetLevelSaveData().Tutorial;
            if(_definition?.Lesson==TutorialLessonId.None && pendingLesson!=null && !pendingLesson.Completed && TutorialLessonController.PowerFor(pendingLesson.Lesson).HasValue)
            {_definition=_definition.Copy();_definition.Lesson=pendingLesson.Lesson;_definition.TrialPowerUp=TutorialLessonController.PowerFor(pendingLesson.Lesson);}
            if(_definition!=null && _definition.Lesson!=TutorialLessonId.None && _definition.Lesson!=TutorialLessonId.Basics)
            {
                if(_definition.IsPlaytest || _definition.DisablePowerUps || !TutorialProgress.Seen("lesson_"+_definition.Lesson))StartLesson(inPlace);
                return;
            }
            if (TutorialLessonController.IsBasics(_definition,_tracker.IsFirstLevelTutorial()))
            {
                CreatePresentation();
                _gate.SetNavigationLocked(true);
                _first = new FirstLevelTutorialController(_initial, _board, _placement, _results, _round, _info, _ui,
                    _gate, _mask, _hand, _message, _buttons, LatestResult, () =>
                    {
                        _first = null; _gate.SetNavigationLocked(false); ClosePresentation(false);
                    },()=>Find<LifeBarView>()?.GetRectTransform(),(step,completed)=>{
                        var state=_save?.GetLevelSaveData().Tutorial;
                        if(state==null)return;state.Step=step;state.Completed=completed;CheckpointLesson();
                    });
                _first.Start(inPlace?firstStep:_save?.GetLevelSaveData().Tutorial?.Step??-1,inPlace);
            }
            else if (_tracker.GetPowerUpCount(RewardType.Ultra)>0 && !TutorialProgress.Seen(TutorialProgress.Ultra)
                && _levels?.GetLevelData()?.Mechanic==LevelMechanic.Normal && ExplainPowerUp(GameUIButtonType.UltraPowerUp)) { }
            else if (_levels?.GetLevelData()?.TrialPowerUp is RewardType trial && ExplainPowerUp(PowerUpTypeUtility.ToGameButtonType(trial))) { }
            else if (_levels != null && ExplainLevelMechanic()) { }
            else if (!TutorialProgress.Seen(TutorialProgress.CrystalHints) && _round.GetLifeBarRewardInfoList().Any(token => token.IsCrystal))
            {
                ShowTip("Kristal İpuçları", "Kazanamadığın kristaller oyun içinde sana yardım eder.",
                    TutorialProgress.CrystalHints, Find<LifeBarView>()?.GetRectTransform());
            }
        }
        private bool ExplainLevelMechanic()
        {
            var mechanic = LevelMechanicRules.Normalize(_levels.GetLevelData().Mechanic);
            if (mechanic == LevelMechanic.Normal) return false;
            if(mechanic==LevelMechanic.Ice)
            {
                if(TutorialProgress.Seen(TutorialProgress.IceLevel))return false;
                ShowTip(LevelInformation.Title(mechanic),LevelInformation.Description(mechanic),TutorialProgress.IceLevel,Find<BoardAreaView>()?.GetRectTransform());
                return true;
            }
            if (mechanic == LevelMechanic.Dynamite)
            {
                if (TutorialProgress.Seen(TutorialProgress.DynamiteLevel)) return false;
                ShowTip("Dinamitli Tahta", "İlk 4 hamlede dinamitli alan için doğru meyveyi bulmalısın yoksa oyun kaybedilir.",
                    TutorialProgress.DynamiteLevel,Find<BoardAreaView>()?.GetRectTransform());
                return true;
            }
            if (mechanic == LevelMechanic.Growing)
            {
                if (TutorialProgress.Seen(TutorialProgress.GrowingLevel)) return false;
                ShowTip("Büyüyen Tahta", "2 gizli meyve ile başlarsın, her iki tahminden sonra bir gizli meyve daha bulman gerekir.",
                    TutorialProgress.GrowingLevel,Find<BoardAreaView>()?.GetRectTransform());
                return true;
            }
            if (mechanic == LevelMechanic.Split)
            {
                if (TutorialProgress.Seen(TutorialProgress.SplitLevel)) return false;
                ShowTip("Bölünmüş Tahta", "İki grubun sonuçları ayrı hesaplanır, yerleştirilen meyveler kendi grubunda değerlendirilir.",
                    TutorialProgress.SplitLevel,Find<BoardAreaView>()?.GetRectTransform());
                return true;
            }
            if (mechanic == LevelMechanic.Pair)
            {
                if (TutorialProgress.Seen(TutorialProgress.PairLevel)) return false;
                ShowTip("Ardışık Bağlı Meyveler", "Cevapta bulunan yan yana iki meyveyi gösterir.",
                    TutorialProgress.PairLevel, PairTargets());
                return true;
            }
            if (TutorialProgress.Seen(TutorialProgress.HelperLevel)) return false;
            ShowTip("Bonus İpucu", "Her 3 yanlış tahminde bir tahmindeki meyvelerin detaylı bilgisi verilir",
                TutorialProgress.HelperLevel, LatestResult());
            return true;
        }
        public void RoundEnded(bool success)
        {
            _roundEnded = true; _ready = false;
            StopLesson();
            if (_first != null)
            {
                if (success) _first.Complete();
                else { _first.Dispose(); _first = null; ClosePresentation(false); }
            }
            else ClosePresentation(false);
        }
        public void ShowRewards(int coins, int crystals)
        {
            if(_rewardTipShown)return;
            if(_basicsRound && coins>0)
            {
                _rewardTipShown=true;
                ShowTip("Harikasın!", "Bulduğun meyveleri koleksiyonuna, altınları cüzdanına ekliyorum",TutorialProgress.Currency);
                return;
            }
            if(_definition?.Lesson==TutorialLessonId.Crystal && crystals>0)
            {
                _rewardTipShown=true;
                ShowTip("Kristal Etkisi", "Kristaller güçlendirici kazandırır",TutorialProgress.Currency,
                    Find<VerticalCrystalProgressView>()?.GetRectTransform());
                _message?.ShowCrystalFlow();
                _coinHiddenForTip=true;_ui.SetCoinCounterVisible(false);
                return;
            }
            if(_definition?.Lesson!=TutorialLessonId.None && _definition!=null)
            {
                return;
            }
            if ((coins <= 0 && crystals <= 0) || TutorialProgress.Seen(TutorialProgress.Currency)) return;
            RectTransform bar = Find<VerticalCrystalProgressView>()?.GetRectTransform();
            if (bar != null && !bar.gameObject.activeInHierarchy) bar = null;
            ShowTip("Coin ve güçlendiriciler", "Coinlerle tohum al.\nKristallerle güçlendirici kazan.",
                TutorialProgress.Currency, _ui.GetCoinImageRectTransform(), bar);
        }
        private bool ExplainPowerUp(GameUIButtonType type)
        {
            // A scheduled lesson owns its own introduction. Acknowledging it must not
            // open a second first-use popup or consume the player's click twice.
            if(_definition?.Lesson!=TutorialLessonId.None && _definition!=null)return false;
            if (!_ready || _roundEnded || _message != null || _tracker.GetGameOption() != GameOption.SinglePlayer) return false;
            if (type != GameUIButtonType.BombPowerUp && type != GameUIButtonType.RevealingPowerUp
                && type != GameUIButtonType.LifePowerUp && type != GameUIButtonType.HelperPowerUp
                && type != GameUIButtonType.PairPowerUp && type != GameUIButtonType.SplitPowerUp && type != GameUIButtonType.UltraPowerUp) return false;
            var reward=PowerUpTypeUtility.ToRewardType(type);
            string key=PowerUpTypeUtility.TutorialKey(reward);
            if (TutorialProgress.Seen(key) || _tracker.GetPowerUpCount(reward) <= 0) return false;
            if(_levels==null || _save==null || (_uses!=null && !_uses.Value.CanUse(reward)))return false;
            _definition=_levels.GetLevelData().Copy();
            _definition.Lesson=TutorialLessonController.ForPower(reward);
            _definition.TrialPowerUp=reward;
            _save.GetLevelSaveData().Tutorial=new TutorialLessonState {Lesson=_definition.Lesson};
            StartLesson(true);
            return true;
        }
        private void ShowTip(string title, string text, string key, params RectTransform[] targets)
        {
            if (_message != null) return;
            GameTelemetry.Tutorial("tutorial_started", key);
            CreatePresentation(); _gate.Block(); _gate.SetNavigationLocked(true); LockCards(-1);
            foreach (var target in targets) _mask.Highlight(target);
            _message.ShowAt(title, text, true, TutorialMessagePlacement.Center, "Tamam");
            var presentation=_message;
            bool closed=false;
            void Dismiss()
            {
                if(closed || _message==null || _message!=presentation)return;
                closed=true;
                GameTelemetry.Tutorial("tutorial_completed", key);
                TutorialProgress.Mark(key); _gate.SetNavigationLocked(false); ClosePresentation(!_roundEnded);
                // Acknowledging an introduction never activates or spends a powerup.
            }
            _message.Bind(_buttons,Dismiss);
            _mask.SetDismiss(Dismiss);
        }
        private RectTransform[] PairTargets()
        {
            var clue = Find<PairClueView>()?.Clue;
            if (clue == null) return Array.Empty<RectTransform>();
            return new[] { _initial.GetRectTransformOfCardItem(clue.First - 1), _initial.GetRectTransformOfCardItem(clue.Second - 1) };
        }

        private void CreatePresentation()
        {
            _mask.Init(1f, _canvas.rect.height); _mask.InstantiateTutorialFade();
            _message = UnityEngine.Object.Instantiate(_messagePrefab, _parent, false); _message.Init();
            _hand = UnityEngine.Object.Instantiate(_handPrefab, _parent, false); _hand.Init(1f, _canvas.rect.height);
            _message.transform.SetParent(_canvas,false);_message.ApplyLayout();
            _hand.transform.SetParent(_canvas,false);_hand.transform.SetAsLastSibling();
            foreach (Transform child in _message.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = _parent.gameObject.layer;
            foreach (Transform child in _hand.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = _parent.gameObject.layer;
        }
        private RectTransform LatestResult()
        {
            var area = Find<ResultAreaView>();
            area?.SetScrollPositionToBottom();
            var row = area?.GetVisibleRow(area.HistoryCount-1);
            return row != null ? (RectTransform)row.transform : null;
        }
        private T Find<T>() where T : Component => _canvas != null ? _canvas.GetComponentInChildren<T>(true) : null;
        private void LockCards(int allowed)
        {
            if (_initial == null) return;
            for (int i = 0; i < _initial.GetCardCount(); i++)
                if (_initial.TryGetCardItem(i, out var card)) card.GetCardMoveHandler().SetMovementLocked(i != allowed);
        }
        private void ClosePresentation(bool restoreCards)
        {
            if(_coinHiddenForTip){_coinHiddenForTip=false;_ui.SetCoinCounterVisible(true);}
            if (_hand != null) _hand.Destroy(); _hand = null;
            if (_message != null) _message.Destroy(); _message = null;
            if(!(_mask is UnityEngine.Object owner) || owner!=null)_mask.CloseTutorialFade();
            _gate.Clear();
            if (!restoreCards || _canvas==null) return;
            var info = _info.GetCardItemInfoList();
            for (int i = 0; i < _initial.GetCardCount(); i++)
                if (_initial.TryGetCardItem(i, out var card)) card.GetCardMoveHandler().SetMovementLocked(info != null && i < info.Count && (info[i].isLocked || !info[i].isExisted));
        }
        public void Cancel()
        {
            _gate.SetNavigationLocked(false);
            if(_ready && !_roundEnded && !_suspended && !_resumeInPlace && _lesson!=null && !_lesson.State.Completed)
                GameTelemetry.Tutorial("tutorial_abandoned","lesson_"+_lesson.State.Lesson,_lesson.State.Step);
            StopLesson();_suspended=_modalSuspended=false;
            _resumeInPlace=false;_suspendedFirstStep=-1;
            _first?.Dispose(); _first = null; ClosePresentation(!_roundEnded); _ready = false;
        }

        private void StartLesson(bool keepPlacements)
        {
            if(_save==null)return;
            var state=_save.GetLevelSaveData().Tutorial??(_save.GetLevelSaveData().Tutorial=new TutorialLessonState {Lesson=_definition.Lesson});
            _lesson=new TutorialLessonController(_definition.Lesson,state,CheckpointLesson,_round.GetTriedCardsList().FirstOrDefault());
            _lesson.RewindUnplacedCards(keepPlacements);
            _lesson.Changed+=RenderLesson;
            _placement.CardPlacedEvent+=OnLessonPlaced;_results.GuessResolved+=OnLessonGuess;
            _gate.NoteOpened+=OnNoteOpened;_gate.FruitExcluded+=OnFruitExcluded;_gate.PositionExcluded+=OnPositionExcluded;
            if(_uses!=null)_uses.Value.Applied+=OnLessonPower;
            _lastGuess=_lastRevision=-1;
            GameTelemetry.Tutorial("tutorial_started","lesson_"+_definition.Lesson);
            _lesson.Start();Tick();
        }
        private void StopLesson()
        {
            _gate.SetNavigationLocked(false);
            _lessonDelay?.Kill();_lessonDelay=null;
            if(_lesson==null)return;
            _lesson.Changed-=RenderLesson;
            _placement.CardPlacedEvent-=OnLessonPlaced;_results.GuessResolved-=OnLessonGuess;
            _gate.NoteOpened-=OnNoteOpened;_gate.FruitExcluded-=OnFruitExcluded;_gate.PositionExcluded-=OnPositionExcluded;
            if(_uses!=null)_uses.Value.Applied-=OnLessonPower;
            _lesson=null;
        }
        private void CheckpointLesson()
        {
            if(_snapshots==null || _saves==null)return;
            ObserverNotification.Run(()=>{if(_snapshots.Value.TryCreateSnapshot(out var data,true))_saves.Save(data);});
        }
        private void OnLessonPlaced(object sender,(int cardIndex,int boardIndex) value){if(!_suspended)_lesson?.Accept(LessonSignal.Placed,value.cardIndex,value.boardIndex);}
        private void OnNoteOpened(int card){if(!_suspended)_lesson?.Accept(LessonSignal.NoteOpened,card);}
        private void OnFruitExcluded(int card){if(!_suspended)_lesson?.Accept(LessonSignal.FruitExcluded,card);}
        private void OnPositionExcluded(int card,int slot){if(!_suspended)_lesson?.Accept(LessonSignal.PositionExcluded,card,slot);}
        private void OnLessonPower(RewardType type)
        {if(!_suspended && _lesson?.Current?.Power==type)_lesson.Accept(LessonSignal.PowerApplied);}
        private void OnLessonGuess(object sender,GuessEvaluation guess)
        {
            if(_lesson==null || _suspended)return;
            _lesson.BindFirstGuess(_round.GetTriedCardsList().FirstOrDefault());
            if(guess.Submission.Slots.Any(slot=>slot.Kind==GuessSlotKind.Frozen))_lesson.Accept(LessonSignal.FrozenGuess);
            _lesson.Accept(LessonSignal.Guess);Tick();
        }
        public void Tick()
        {
            if(_lesson==null || _suspended || _roundEnded)return;
            if(_annotations!=null && !_annotations.IsSelecting
                && (_lesson.Current?.Expected==LessonSignal.PositionExcluded || _lesson.Current?.Expected==LessonSignal.FruitExcluded))
            {
                // Dismissing the editor must leave a way back to the required note.
                _lesson.RewindUnplacedCards(true);CheckpointLesson();_lesson.Start();return;
            }
            int guess=_round.GetTriedCardsList().Count,revision=_save.GetLevelSaveData().PowerUpUseRevision;
            if(guess==_lastGuess && revision==_lastRevision)return;
            _lastGuess=guess;_lastRevision=revision;
            var data=_save.GetLevelSaveData();
            if(revision>0 && data.LastPowerUp.HasValue && data.LastPowerUp==_definition.TrialPowerUp)
            {
                _lesson.Accept(LessonSignal.Available);
                if(_lesson.Current?.Power==data.LastPowerUp)_lesson.Accept(LessonSignal.PowerApplied);
            }
            if(_lesson.Current?.Expected==LessonSignal.Available && _definition.TrialPowerUp.HasValue && _uses?.Value.CanUse(_definition.TrialPowerUp.Value)==true)
                _lesson.Accept(LessonSignal.Available);
            if(data.GrowingBoard?.ActiveCount>2)_lesson.Accept(LessonSignal.Growth);
            if(data.RevealedGuessNumbers.Count>0)_lesson.Accept(LessonSignal.Helper);
            if(data.Dynamite?.Disarmed==true)_lesson.Accept(LessonSignal.Disarmed);
            if(data.Ice?.Frozen==true)_lesson.Accept(LessonSignal.Frozen);
        }
        private void RenderLesson(TutorialLessonStep step)
        {
            _lessonDelay?.Kill();_lessonDelay=null;
            if(step?.Expected==LessonSignal.NoteOpened)_annotations?.Cancel();
            ClosePresentation(true);
            // Waiting for a power to become usable is ordinary play, without a
            // teaching overlay. Keep settings available until the guided action.
            _gate.SetNavigationLocked(step!=null && !(step.Wait && step.Expected==LessonSignal.Available));
            if(step==null)
            {
                if(_lesson?.State.Completed==true)
                {
                    GameTelemetry.Tutorial("tutorial_completed","lesson_"+_definition.Lesson,_lesson.State.Step);
                    if(!_definition.IsPlaytest)TutorialProgress.Mark("lesson_"+_definition.Lesson);
                    var power=TutorialLessonController.PowerFor(_definition.Lesson);
                    if(power.HasValue)TutorialProgress.Mark(PowerUpTypeUtility.TutorialKey(power.Value));
                }
                return;
            }
            if(step.Wait || _suspended)return;
            SetLessonGate(step);
            // State and checkpoint have already advanced. This delay only waits for
            // cards returning home; cancelling it cannot cancel an applied game rule.
            float delay=_definition.Lesson==TutorialLessonId.PositionNote ? .75f : .4f;
            _lessonDelay=DOTween.Sequence().AppendInterval(delay).AppendCallback(()=>PresentLesson(step));
        }
        private void PresentLesson(TutorialLessonStep step)
        {
            if(_lesson?.Current!=step || _suspended || _roundEnded)return;
            CreatePresentation();
            SetLessonGate(step);
            RectTransform target=LessonTargetRect(step);
            _mask.Highlight(target);
            if(step.Drag)
            {
                var holder=_board.GetRectTransformOfGarden(step.Slot);_mask.Highlight(holder);
                if(target!=null && holder!=null)_hand.StartDragAnimation(target.position,holder.position);
            }
            else if(step.Expected==LessonSignal.NoteOpened){if(target!=null)_hand.StartClickAnimation(target.position);}
            else if(step.Expected!=LessonSignal.Continue && target!=null)_hand.StartClickAnimation(target.position);
            if(step.Target==LessonTarget.Pair)foreach(var item in PairTargets())_mask.Highlight(item);
            if(step.Power==RewardType.Revealing && step.Expected==LessonSignal.PowerApplied)
                _mask.Highlight(Find<BoardAreaView>()?.GetRectTransform());
            if(step.Power==RewardType.Ultra)_mask.Highlight(Find<InitialCardAreaView>()?.GetRectTransform());
            bool instruction=step.Expected!=LessonSignal.Continue;
            string open=(_definition.NumOfBoardHolders-1).ToString();
            bool positionLesson=_definition.Lesson==TutorialLessonId.PositionNote;
            _message.ShowAt(step.Title.Replace("{open}",open),step.Text.Replace("{open}",open),!instruction,
                instruction && !positionLesson?TutorialMessagePlacement.Top:TutorialMessagePlacement.Center);
            _message.Bind(_buttons,()=>{
                if(_lesson?.Current!=step)return;
                if(!instruction)_lesson?.Accept(LessonSignal.Continue);
                _lastGuess=_lastRevision=-1;Tick();
            });
            if(positionLesson)_message.FadeIn(.25f);
            _mask.SetOutsideDismiss(null);
            GameTelemetry.Tutorial("tutorial_step","lesson_"+_definition.Lesson,_lesson.State.Step);
        }
        private void SetLessonGate(TutorialLessonStep step)
        {
            if(step.Expected==LessonSignal.Placed){_gate.AllowPlacement(step.Card,step.Slot);LockCards(step.Card);}
            else if(step.Expected==LessonSignal.Guess){_gate.AllowGuess();LockCards(-1);}
            else if(step.Expected==LessonSignal.NoteOpened || step.Expected==LessonSignal.FruitExcluded || step.Expected==LessonSignal.PositionExcluded)
                _gate.AllowNotes(step.Card);
            else if(step.Expected==LessonSignal.Continue){_gate.Block();LockCards(-1);}
            else if(step.Expected==LessonSignal.PowerApplied && step.Power.HasValue){_gate.AllowPower(PowerUpTypeUtility.ToGameButtonType(step.Power.Value));LockCards(-1);}
            else _gate.Clear();
        }
        private RectTransform LessonTargetRect(TutorialLessonStep step)
        {
            switch(step.Target)
            {
                case LessonTarget.Card:return step.Card>=0?_initial.GetRectTransformOfCardItem(step.Card):null;
                case LessonTarget.Holder:return _board.GetRectTransformOfGarden(step.Slot==-2?_definition.NumOfBoardHolders-1:step.Slot);
                case LessonTarget.Initial:return Find<InitialCardAreaView>()?.GetRectTransform();
                case LessonTarget.Guess:return _ui.GetCheckButtonRectTransform();
                case LessonTarget.Result:return LatestResult();
                case LessonTarget.NoteRed:return _annotations?.GetButton(0)?.transform as RectTransform;
                case LessonTarget.NotePosition:return _annotations?.GetPositionButton(step.Slot)?.GetRectTransform();
                case LessonTarget.Power:return step.Power.HasValue?Find<PowerUpListHolderView>()?.GetPowerUpButton(step.Power.Value)?.GetButtonRectTransform():null;
                case LessonTarget.Life:return Find<LifeBarView>()?.GetRectTransform();
                case LessonTarget.Crystal:return Find<VerticalCrystalProgressView>()?.GetRectTransform();
                case LessonTarget.Pair:return Find<PairClueView>()?.transform as RectTransform;
                default:return Find<BoardAreaView>()?.GetRectTransform();
            }
        }
        public void Suspend(bool modal=false)
        {
            if(!_ready || _roundEnded)return;
            if(!_suspended && _lesson!=null)GameTelemetry.Tutorial("tutorial_suspended","lesson_"+_lesson.State.Lesson,_lesson.State.Step);
            if(!_suspended){_resumeInPlace=true;_suspendedFirstStep=_first?.Step??-1;}
            _suspended=true;_modalSuspended|=modal;_lessonDelay?.Kill();
            _first?.Dispose();_first=null;ClosePresentation(true);
        }
        public void Resume(bool modal=false)
        {
            if(modal)_modalSuspended=false;
            if(!_suspended || _modalSuspended || !_ready || _roundEnded)return;
            if(_canvas==null || _parent==null){_ready=false;return;}
            _suspended=false;StartRound();
        }
        public void Dispose() { Cancel(); _gate.FirstUseHandler = null; }
    }
    public interface ITutorialBootstrapper : IDisposable
    {
        void Configure(Transform parent, RectTransform safeArea, RectTransform canvas, HandTutorialView handPrefab, TutorialMessagePopupView messagePrefab);
        void PrepareRound();
        void StartRound();
        void RoundEnded(bool success);
        void ShowRewards(int coins, int crystals);
        void Cancel();
        void Tick();
        void Suspend(bool modal=false);
        void Resume(bool modal=false);
    }
}
