using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public interface ISettingsPopupService
    {
        void Show(SettingsPopupView prefab,UnityEngine.Transform parent,string name,Action collection=null,bool menu=false);
    }
    public sealed class SettingsPopupService : ISettingsPopupService
    {
        private readonly ILevelTracker _tracker;private readonly BaseButtonControllerFactory _buttons;
        public SettingsPopupService(ILevelTracker tracker,BaseButtonControllerFactory buttons){_tracker=tracker;_buttons=buttons;}
        public void Show(SettingsPopupView prefab,UnityEngine.Transform parent,string name,Action collection=null,bool menu=false)
        {
            if(prefab==null || parent.Find(name)!=null)return;
            var overlay=FriendUi.Overlay(parent,name);
            var popup=UnityEngine.Object.Instantiate(prefab,overlay);
            void Close()=>TutorialObjectUtility.Destroy(overlay.gameObject);
            new SettingsPopupController().Initialize(popup,Close,null,null,_tracker,_buttons);
            if(menu)popup.ConfigureForMenu(()=>{Close();collection?.Invoke();});
            else popup.GetCloseButton().SetText("Geri dön");
        }
    }
    public class SettingsPopupController : ISettingsPopupController
    {
        private ISettingsPopupView _view;
        private ILevelTracker _levelTracker;
        private readonly Action<string> _loadScene;
        private readonly Action _shutdownNetwork;
        private bool _isNavigating;
        private readonly Action _privacy;

        public SettingsPopupController() : this(scene => SceneManager.LoadScene(scene), () =>
        {
            MultiplayerConnection.Shutdown();
        }) { }

        public SettingsPopupController(Action<string> loadScene, Action shutdownNetwork,Action privacy=null)
        {
            _privacy=privacy??(()=>TelemetryRuntime.Instance?.ShowPrivacy());
            _loadScene = loadScene;
            _shutdownNetwork = shutdownNetwork;
        }
        public void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker, BaseButtonControllerFactory baseButtonControllerFactory,IPositionNotesSettings notes=null)
        {
            if(_view!=null)_view.PrivacyRequested-=OpenPrivacy;
            _view = view;_view.PrivacyRequested+=OpenPrivacy;
            _levelTracker = levelTracker;
            _isNavigating = false;
            _view.Init();
            _view.SetSinglePlayer(_levelTracker.GetGameOption() == GameOption.SinglePlayer);
            void RefreshNotes()=>_view.ConfigureNotes(notes?.CanResetNotes==true,
                ()=>{notes.ResetNotes();RefreshNotes();saveGameAction?.Invoke();});
            RefreshNotes();
            IBaseButtonController closeButtonController =
                baseButtonControllerFactory.Create(_view.GetCloseButton(), () => OnCloseButtonClick(onCloseAction));
            closeButtonController.SetText("Oyuna dön");
            closeButtonController.SetClickOnRelease(true);

            if (_levelTracker.GetGameOption() == GameOption.SinglePlayer)
            {
                IBaseButtonController retryButtonController = baseButtonControllerFactory.Create(_view.GetRetryButton(), () => OnRetryButtonClick(deleteSaveAction));
                retryButtonController.SetText("Yeniden başlat");
                retryButtonController.SetClickOnRelease(true);
                
                IBaseButtonController menuButtonController = baseButtonControllerFactory.Create(_view.GetMenuButton(), () => OnMenuButtonClick(saveGameAction));

                menuButtonController.SetText("Ana menü");
                menuButtonController.SetClickOnRelease(true);
            }

            else if (_levelTracker.GetGameOption() == GameOption.MultiPlayer)
            {
                IBaseButtonController menuButtonController = baseButtonControllerFactory.Create(_view.GetMenuButton(), () => OnMenuButtonClick(null));
                menuButtonController.SetText("Ana menü");
                menuButtonController.SetClickOnRelease(true);
                
            }
        }

        private void OpenPrivacy()=>_privacy();
        private void OnCloseButtonClick(Action onCloseAction)
        {
            _view.Close();
            onCloseAction.Invoke();
        }

        private void OnRetryButtonClick(Action deleteSaveAction)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            deleteSaveAction.Invoke();
            _loadScene("Game");
        }
        
        private void OnMenuButtonClick(Action saveGameAction)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            saveGameAction?.Invoke();
            _shutdownNetwork?.Invoke();
            _loadScene("Menu");
        }
    }

    public interface ISettingsPopupController
    {
        void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker, BaseButtonControllerFactory baseButtonControllerFactory,IPositionNotesSettings notes=null);
    }
}
