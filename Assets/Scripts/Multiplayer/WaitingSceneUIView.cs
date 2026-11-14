using TMPro;
using UnityEngine;

namespace Scripts
{
    public class WaitingSceneUIView : MonoBehaviour, IWaitingSceneUIView
    {
        public void OpenSettings(ISettingsPopupService service)=>GetComponentInParent<FruitLabOnlineLayout>().OpenSettings(service);
        [SerializeField] private TMP_Text lobbyNameText;
        [SerializeField] private TMP_Text lobbyCodeText;
        [SerializeField] private BaseButtonView menuButton;
        [SerializeField] private BaseButtonView readyButton;
        [SerializeField] private BaseButtonView startButton;
        private UnityEngine.UI.Button _share;
        private bool _host;
        public void ConfigureShare(System.Action share)
        {
            _share = FriendUi.Button(transform, "ShareInvitation", "Davet et", share);
            _share.gameObject.SetActive(_host);
        }
        public void SetLobbyNameText(string text) => lobbyNameText.text = text;
        public void SetLobbyCodeText(string text) => lobbyCodeText.text = "Kod: " + text;
        public IBaseButtonView GetMenuButton() => menuButton;
        public IBaseButtonView GetReadyButton() => readyButton;
        public IBaseButtonView GetStartButton()
        {
            if (startButton == null) { startButton = Instantiate(readyButton, transform); startButton.name = "StartGameButton"; }
            return startButton;
        }
        public void SetStartAvailability(bool isHost, bool canStart)
        {
            _host = isHost;
            GetStartButton(); startButton.gameObject.SetActive(isHost && canStart);
            readyButton.gameObject.SetActive(!(isHost && canStart));
            if (_share != null) _share.gameObject.SetActive(isHost);
            var group = startButton.GetComponent<CanvasGroup>();
            if (group == null) group = startButton.gameObject.AddComponent<CanvasGroup>();
            group.alpha = canStart ? 1 : .4f; group.interactable = group.blocksRaycasts = canStart;
        }
        public void SetStatus(string message)
        {
            var status = transform.Find("WaitingStatus")?.GetComponent<TMP_Text>();
            if (status == null) status = FruitLabStyle.CreateText("WaitingStatus", transform, 16, PopupArtStyle.Body);
            status.text = message; status.textWrappingMode = TextWrappingModes.Normal;
        }
        public void ApplyOnlineLayout(float width, float height)
        {
            var heading = transform.Find("OnlineTitle") as RectTransform;
            if (heading != null) FruitLabStyle.Place(heading, transform, 20, height - 46, width - 40, 30);
            FruitLabStyle.Place(lobbyNameText.rectTransform, transform, 14, height - 77, 84, 22);
            lobbyNameText.fontSize = 17; lobbyNameText.color = PopupArtStyle.Body;
            FruitLabStyle.Place(lobbyCodeText.rectTransform, transform, 18, height - 111, width - 136, 32);
            lobbyCodeText.fontSize = 20; lobbyCodeText.color = PopupArtStyle.Ink;
            if (transform.Find("WaitingStatus") == null) SetStatus("Arkadaşın bekleniyor…");
            var status = transform.Find("WaitingStatus").GetComponent<TMP_Text>(); status.fontSize = 16;status.color=PopupArtStyle.Body;
            FruitLabStyle.Place(status.rectTransform, transform, 16, 68, width - 32, 30);
            var oldTitle = transform.Find("PlayersTitle"); if (oldTitle != null) oldTitle.gameObject.SetActive(false);
            var names = GetComponentInParent<Canvas>().GetComponentInChildren<PlayerNameAreaView>(true);
            foreach (var group in names.GetComponents<UnityEngine.UI.LayoutGroup>()) group.enabled = false;
            var oldSurface=names.GetComponent<UnityEngine.UI.Image>();if(oldSurface!=null)oldSurface.enabled=false;
            var oldRim=names.transform.Find("LabRim");if(oldRim!=null)oldRim.gameObject.SetActive(false);
            FruitLabStyle.Place((RectTransform)names.transform, transform, 14, 104, width - 28, height - 224);
            names.SetParticipantCount(names.GetComponentsInChildren<PlayerNameView>().Length);
            GetStartButton();
            FruitLabOnlineLayout.Button(readyButton, transform, 8, width, true); readyButton.SetText("Hazırım");
            FruitLabOnlineLayout.Button(startButton, transform, 8, width, true); startButton.SetText("Oyunu Başlat");
            if (_share == null) ConfigureShare(null);
            FruitLabStyle.Place((RectTransform)_share.transform, transform, width-112, height - 113, 94, 44);
            FruitLabOnlineLayout.Settings(menuButton, (RectTransform)GetComponentInParent<Canvas>().transform);
            foreach (var row in names.GetComponentsInChildren<PlayerNameView>()) row.ApplyRowLayout();
        }
    }
    public interface IWaitingSceneUIView
    {
        void OpenSettings(ISettingsPopupService service);
        void ConfigureShare(System.Action share);
        void SetStatus(string message); void SetLobbyNameText(string text); void SetLobbyCodeText(string text);
        void SetStartAvailability(bool isHost, bool canStart);
        IBaseButtonView GetMenuButton(); IBaseButtonView GetReadyButton(); IBaseButtonView GetStartButton();
    }
}
