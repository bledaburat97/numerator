using System;
using UnityEngine;
using Zenject;

namespace Scripts
{
    public class GamePopupCreator : MonoBehaviour, IGamePopupCreator
    {
        [Inject] private BaseButtonControllerFactory _baseButtonControllerFactory;
        [Inject] private IHapticController _hapticController;

        [Inject] private IFadePanelController _fadePanelController;
        [Inject] private IGameSaveService _gameSaveService;
        [Inject] private IGameSaveSnapshotProvider _gameSaveSnapshotProvider;
        [Inject] private ILevelTracker _levelTracker;
        [Inject] private IGameUIController _gameUIController;
        [Inject] private ITutorialBootstrapper _tutorialBootstrapper;
        [Inject] private IPositionNotesSettings _notes;
        [Inject] private IPowerUpMessageController _powerMessages;
        [Inject] private ILevelDataCreator _levels;
        [Inject] private ISettingsPopupService _settings;
        [Inject] private LazyInject<IGameInteractionCancellation> _interactions;
        private GameInformationPopup _information;
        
        [SerializeField] private SettingsPopupView settingsPopupPrefab;
        [SerializeField] private DisconnectionPopupView disconnectionPopupPrefab;
        [SerializeField] private MessagePopupView messagePopupPrefab;
        [SerializeField] private HandTutorialView handTutorialPrefab;
        [SerializeField] private TutorialMessagePopupView tutorialMessagePopupPrefab;

        private DisconnectionPopupViewFactory _disconnectionPopupViewFactory;
        private MessagePopupViewFactory _messagePopupViewFactory;

        private Action _saveGameAction = null;
        private Action _deleteSaveAction = null;
        [SerializeField] private RectTransform safeAreaRectTransform;
        [SerializeField] private RectTransform canvasRectTransform;
        
        private IMessagePopupView _newGameOfferPopup;
        private IMessagePopupView _notAbleToMovePopup;
        private bool _initialized;
        
        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _disconnectionPopupViewFactory = new DisconnectionPopupViewFactory();
            _messagePopupViewFactory = new MessagePopupViewFactory();
    
            _gameUIController.OpenSettings += CreateSettingsPopup;
            _gameUIController.OpenInformation += CreateInformationPopup;
            _gameUIController.NotAbleToCheck += CreateNotAbleToMovePopup;

            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                _saveGameAction = TrySaveGame;
            }
            else
            {
                _saveGameAction = null;
            }

            _deleteSaveAction = _gameSaveService.DeleteSave;
            _tutorialBootstrapper.Configure(
                transform,
                safeAreaRectTransform,
                canvasRectTransform,
                handTutorialPrefab,
                tutorialMessagePopupPrefab);
        }

        public RectTransform GetSafeAreaRectTransform()
        {
            return safeAreaRectTransform;
        }

        public void PrepareTutorials() { _powerMessages.CloseActivePowerUp(); _tutorialBootstrapper.PrepareRound(); }
        public void StartTutorials() => _tutorialBootstrapper.StartRound();
        public void EndTutorials(bool success) { _powerMessages.CloseActivePowerUp(); _tutorialBootstrapper.RoundEnded(success); }
        public void ShowRewardTutorials(int coins, int crystals) => _tutorialBootstrapper.ShowRewards(coins, crystals);
        private void OnDestroy()
        {
            if(_gameUIController==null)return;
            _gameUIController.OpenSettings-=CreateSettingsPopup;
            _gameUIController.OpenInformation-=CreateInformationPopup;
            _gameUIController.NotAbleToCheck-=CreateNotAbleToMovePopup;
        }

        public RectTransform GetCanvasRectTransform()
        {
            return canvasRectTransform;
        }
        
        private void CreateNotAbleToMovePopup(object sender, EventArgs args)
        {
            void OnClose() => _notAbleToMovePopup = null;
            _notAbleToMovePopup = _messagePopupViewFactory.Spawn(transform, messagePopupPrefab);
            _notAbleToMovePopup.Init("Please wait for your turn.", 0f, new Vector2(0,318));
            _notAbleToMovePopup.SetColor(ConstantValues.NOT_ABLE_TO_MOVE_TEXT_COLOR);
            _notAbleToMovePopup.Animate(1f, OnClose);
        }

        public void CreateNewGameOfferPopup()
        {
            _newGameOfferPopup = _messagePopupViewFactory.Spawn(transform, messagePopupPrefab);
            _hapticController.Vibrate(HapticType.CardRelease);
            _newGameOfferPopup.Init("Opponent offers a new game.", 1f, new Vector2(0,200));
        }
        
        
        public void CreateDisconnectionPopup()
        {
            _fadePanelController.SetFadeImageStatus(true);
            IDisconnectionPopupController disconnectionPopupController = new DisconnectionPopupController();
            IDisconnectionPopupView disconnectionPopupView =
                _disconnectionPopupViewFactory.Spawn(transform, disconnectionPopupPrefab);
            _hapticController.Vibrate(HapticType.Warning);
            disconnectionPopupController.Initialize(disconnectionPopupView, _baseButtonControllerFactory);
            disconnectionPopupController.SetText("Opponent is disconnected!");
        }
        
        public void CloseNotAbleToMovePopup()
        {
            if (_notAbleToMovePopup != null)
            {
                _notAbleToMovePopup.Close();
            }
        }
        
        public void CloseNewGameOfferPopup()
        {
            if (_newGameOfferPopup != null)
            {
                _newGameOfferPopup.Close();
            }
        }
        
        private void CreateInformationPopup(object sender,EventArgs args)
        {
            if(_information!=null)return;
            _interactions.Value.CancelCurrentInteractions();
            _information=GameInformationPopup.Open(safeAreaRectTransform,_levels.GetLevelData().Mechanic,()=>_information=null);
        }

        private void CreateSettingsPopup(object sender, EventArgs args)
        {
            _settings.Show(new SettingsPopupRequest {
                Prefab=settingsPopupPrefab,Parent=canvasRectTransform,OverlayName="GameSettingsOverlay",Context=SettingsPopupContext.Game,
                CancelInteractions=_interactions.Value.CancelCurrentInteractions,Save=_saveGameAction,Reset=_deleteSaveAction,Notes=_notes});
        }

        private void TrySaveGame()
        {
            if (_gameSaveSnapshotProvider.TryCreateSnapshot(out LevelSaveData levelSaveData))
            {
                _gameSaveService.Save(levelSaveData);
            }
        }
    }

    public interface IGamePopupCreator
    {
        void Initialize();
        void PrepareTutorials();
        void StartTutorials();
        void EndTutorials(bool success);
        void ShowRewardTutorials(int coins, int crystals);
        void CreateNewGameOfferPopup();
        void CreateDisconnectionPopup();
        void CloseNotAbleToMovePopup();
        void CloseNewGameOfferPopup();
        RectTransform GetSafeAreaRectTransform();
        RectTransform GetCanvasRectTransform();
    }
}
