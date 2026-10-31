using TMPro;
using UnityEngine;

namespace Scripts
{
    public class WaitingSceneUIView : MonoBehaviour, IWaitingSceneUIView
    {
        [SerializeField] private TMP_Text lobbyNameText;
        [SerializeField] private TMP_Text lobbyCodeText;
        [SerializeField] private BaseButtonView menuButton;
        [SerializeField] private BaseButtonView readyButton;
        [SerializeField] private BaseButtonView startButton;
        public void SetLobbyNameText(string text) => lobbyNameText.text = text;
        public void SetLobbyCodeText(string text) => lobbyCodeText.text = "Oda kodu: " + text;
        public IBaseButtonView GetMenuButton() => menuButton;
        public IBaseButtonView GetReadyButton() => readyButton;
        public IBaseButtonView GetStartButton()
        {
            if (startButton == null) { startButton = Instantiate(readyButton, transform); startButton.name = "StartGameButton"; }
            return startButton;
        }
        public void SetStartAvailability(bool isHost, bool canStart)
        {
            GetStartButton(); startButton.gameObject.SetActive(isHost);
            var group = startButton.GetComponent<CanvasGroup>();
            if (group == null) group = startButton.gameObject.AddComponent<CanvasGroup>();
            group.alpha = canStart ? 1 : .4f; group.interactable = group.blocksRaycasts = canStart;
        }
        public void SetStatus(string message)
        {
            var status = transform.Find("WaitingStatus")?.GetComponent<TMP_Text>();
            if (status == null) status = FruitLabStyle.CreateText("WaitingStatus", transform, 12, FruitLabStyle.Muted);
            status.text = message; status.textWrappingMode = TextWrappingModes.Normal;
        }
        public void ApplyOnlineLayout(float width, float height)
        {
            FruitLabStyle.Place(lobbyNameText.rectTransform, transform, 20, height - 86, width - 40, 22);
            lobbyNameText.fontSize = 14; lobbyNameText.color = FruitLabStyle.Muted;
            FruitLabStyle.Place(lobbyCodeText.rectTransform, transform, 20, height - 126, width - 40, 32);
            lobbyCodeText.fontSize = 22; lobbyCodeText.color = FruitLabStyle.Mint;
            if (transform.Find("WaitingStatus") == null) SetStatus("Oyuncular bekleniyor.");
            FruitLabStyle.Place((RectTransform)transform.Find("WaitingStatus"), transform, 20, height - 169, width - 40, 36);
            var title = transform.Find("PlayersTitle")?.GetComponent<TMP_Text>();
            if (title == null) title = FruitLabStyle.CreateText("PlayersTitle", transform, 15, FruitLabStyle.Text);
            title.text = "Oyuncular"; title.alignment = TextAlignmentOptions.MidlineLeft;
            FruitLabStyle.Place(title.rectTransform, transform, 20, height - 187, width - 40, 24);
            var names = GetComponentInParent<Canvas>().GetComponentInChildren<PlayerNameAreaView>(true);
            foreach (var group in names.GetComponents<UnityEngine.UI.LayoutGroup>()) group.enabled = false;
            FruitLabStyle.Place((RectTransform)names.transform, transform, 20, 138, width - 40, height - 328);
            GetStartButton();
            FruitLabOnlineLayout.Button(readyButton, transform, 82, width, false); readyButton.SetText("Hazırım");
            FruitLabOnlineLayout.Button(startButton, transform, 22, width, true); startButton.SetText("Oyunu Başlat");
            FruitLabOnlineLayout.Settings(menuButton, (RectTransform)GetComponentInParent<Canvas>().transform);
            foreach (var row in names.GetComponentsInChildren<PlayerNameView>()) row.ApplyRowLayout();
        }
    }
    public interface IWaitingSceneUIView
    {
        void SetStatus(string message); void SetLobbyNameText(string text); void SetLobbyCodeText(string text);
        void SetStartAvailability(bool isHost, bool canStart);
        IBaseButtonView GetMenuButton(); IBaseButtonView GetReadyButton(); IBaseButtonView GetStartButton();
    }
}
