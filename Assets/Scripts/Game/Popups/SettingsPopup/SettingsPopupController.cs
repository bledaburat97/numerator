using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scripts
{
    public interface ISettingsPopupService
    {
        SettingsPopupHandle Show(SettingsPopupRequest request);
    }
    public enum SettingsPopupContext { Game, Menu, Online }
    public sealed class SettingsPopupRequest
    {
        public SettingsPopupView Prefab;
        public Transform Parent;
        public string OverlayName;
        public SettingsPopupContext Context;
        public Action CancelInteractions,Closed,Save,Reset;
        public IPositionNotesSettings Notes;
    }
    // Owns just this popup's controller and overlay, including external destruction.
    public sealed class SettingsPopupHandle : IDisposable
    {
        private readonly RectTransform _overlay;
        private readonly Action _closed;
        public SettingsPopupView View {get;}
        public SettingsPopupController Controller {get;}
        public bool IsClosed {get;private set;}
        internal SettingsPopupHandle(RectTransform overlay,SettingsPopupView view,SettingsPopupController controller,Action closed)
        {_overlay=overlay;View=view;Controller=controller;_closed=closed;view.Destroyed+=Close;}
        public void Close()
        {
            if(IsClosed)return;IsClosed=true;
            View.Destroyed-=Close;Controller.Dispose();
            if(_overlay!=null){_overlay.gameObject.SetActive(false);TutorialObjectUtility.Destroy(_overlay.gameObject);}
            ObserverNotification.Run(_closed);
        }
        public void Dispose()=>Close();
    }
    public sealed class SettingsPopupService : ISettingsPopupService
    {
        private readonly ILevelTracker _tracker;private readonly BaseButtonControllerFactory _buttons;
        private readonly Action<string> _loadScene;private readonly Action _shutdown,_privacy;
        [Zenject.Inject]
        public SettingsPopupService(ILevelTracker tracker,BaseButtonControllerFactory buttons)
            :this(tracker,buttons,scene=>SceneManager.LoadScene(scene),MultiplayerConnection.Shutdown,()=>TelemetryRuntime.Instance?.ShowPrivacy()){}
        public SettingsPopupService(ILevelTracker tracker,BaseButtonControllerFactory buttons,Action<string> loadScene,Action shutdown,Action privacy)
        {_tracker=tracker;_buttons=buttons;_loadScene=loadScene;_shutdown=shutdown;_privacy=privacy;}
        public SettingsPopupHandle Show(SettingsPopupRequest request)
        {
            if(request==null || request.Prefab==null || request.Parent==null || string.IsNullOrWhiteSpace(request.OverlayName))
                throw new ArgumentException("Settings require a prefab, parent and overlay name.",nameof(request));
            if(request.Context==SettingsPopupContext.Game && (request.CancelInteractions==null
                || _tracker.GetGameOption()==GameOption.SinglePlayer && request.Reset==null))
                throw new ArgumentException("Game settings require interaction cancellation and a single-player reset action.",nameof(request));
            if(request.Parent.Find(request.OverlayName)!=null)return null;
            request.CancelInteractions?.Invoke(); // Required work precedes creation and UI observers.
            var overlay=FriendUi.Overlay(request.Parent,request.OverlayName);
            SettingsPopupHandle handle=null;
            try
            {
                var popup=UnityEngine.Object.Instantiate(request.Prefab,overlay,false);
                var controller=new SettingsPopupController(_loadScene,_shutdown,_privacy);
                handle=new SettingsPopupHandle(overlay,popup,controller,request.Closed);
                controller.Initialize(popup,handle.Close,request.Save,request.Reset,_tracker,_buttons,request.Notes,request.Context);
                if(request.Context==SettingsPopupContext.Menu)popup.ConfigureForMenu();
                else if(request.Context==SettingsPopupContext.Online)popup.GetCloseButton().SetText("Geri dön");
                return handle;
            }
            catch
            {
                if(handle!=null)handle.Dispose();else TutorialObjectUtility.Destroy(overlay.gameObject);
                throw;
            }
        }
    }
    public class SettingsPopupController : ISettingsPopupController,IDisposable
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
        public void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker, BaseButtonControllerFactory baseButtonControllerFactory,IPositionNotesSettings notes=null,SettingsPopupContext context=SettingsPopupContext.Game)
        {
            if(_view!=null)_view.PrivacyRequested-=OpenPrivacy;
            _view = view;_view.PrivacyRequested+=OpenPrivacy;
            _levelTracker = levelTracker;
            _isNavigating = false;
            _view.Init();
            bool single=context==SettingsPopupContext.Game && _levelTracker.GetGameOption()==GameOption.SinglePlayer;
            _view.SetSinglePlayer(single);
            void RefreshNotes()=>_view.ConfigureNotes(notes?.CanResetNotes==true,
                ()=>{notes.ResetNotes();RefreshNotes();saveGameAction?.Invoke();});
            RefreshNotes();
            IBaseButtonController closeButtonController =
                baseButtonControllerFactory.Create(_view.GetCloseButton(), () => OnCloseButtonClick(onCloseAction));
            closeButtonController.SetText("Oyuna dön");
            closeButtonController.SetClickOnRelease(true);

            if (single)
            {
                IBaseButtonController retryButtonController = baseButtonControllerFactory.Create(_view.GetRetryButton(), () => OnRetryButtonClick(deleteSaveAction));
                retryButtonController.SetText("Yeniden başlat");
                retryButtonController.SetClickOnRelease(true);
                
                IBaseButtonController menuButtonController = baseButtonControllerFactory.Create(_view.GetMenuButton(), () => OnMenuButtonClick(saveGameAction));

                menuButtonController.SetText("Ana menü");
                menuButtonController.SetClickOnRelease(true);
            }

            else
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
            try{deleteSaveAction.Invoke();_loadScene("Game");}
            catch{_isNavigating=false;throw;}
        }
        
        private void OnMenuButtonClick(Action saveGameAction)
        {
            if (_isNavigating) return;
            _isNavigating = true;
            try{saveGameAction?.Invoke();_shutdownNetwork?.Invoke();_loadScene("Menu");}
            catch{_isNavigating=false;throw;}
        }
        public void Dispose(){if(_view!=null)_view.PrivacyRequested-=OpenPrivacy;_view=null;_isNavigating=true;}
    }

    public interface ISettingsPopupController
    {
        void Initialize(ISettingsPopupView view, Action onCloseAction, Action saveGameAction, Action deleteSaveAction, ILevelTracker levelTracker, BaseButtonControllerFactory baseButtonControllerFactory,IPositionNotesSettings notes=null,SettingsPopupContext context=SettingsPopupContext.Game);
    }
}
