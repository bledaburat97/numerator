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

        public void SetLobbyNameText(string text)
        {
            lobbyNameText.text = text;
        }
        
        public void SetLobbyCodeText(string text)
        {
            lobbyCodeText.text = "Oda kodu: " + text;
        }
        
        public IBaseButtonView GetMenuButton()
        {
            return menuButton;
        }

        public IBaseButtonView GetReadyButton()
        {
            return readyButton;
        }
        public void SetStatus(string message)
        {
            var status = transform.Find("WaitingStatus")?.GetComponent<TMP_Text>() ?? FruitLabStyle.CreateText("WaitingStatus", transform, 12f, FruitLabStyle.Muted);
            status.text = message;
        }
        public void ApplyOnlineLayout(float width, float height)
        {
            FruitLabStyle.Place(lobbyNameText.rectTransform, transform, 20, height - 96, width - 40, 24);
            lobbyNameText.fontSize = 15; lobbyNameText.color = FruitLabStyle.Muted;
            FruitLabStyle.Place(lobbyCodeText.rectTransform, transform, 20, height - 142, width - 40, 36);
            lobbyCodeText.fontSize = 23; lobbyCodeText.color = FruitLabStyle.Mint;
            if (transform.Find("WaitingStatus") == null) SetStatus("Rakibin bekleniyor.");
            FruitLabStyle.Place((RectTransform)transform.Find("WaitingStatus"), transform, 20, height - 173, width - 40, 24);
            var title = transform.Find("PlayersTitle")?.GetComponent<TMP_Text>() ?? FruitLabStyle.CreateText("PlayersTitle", transform, 15f, FruitLabStyle.Text);
            title.text = "Oyuncular"; title.alignment = TextAlignmentOptions.MidlineLeft;
            FruitLabStyle.Place(title.rectTransform, transform, 20, height - 213, width - 40, 24);
            FruitLabOnlineLayout.Button(readyButton, transform, 80, width, true);
            FruitLabOnlineLayout.Button(menuButton, transform, 20, width, false);
            var names = GetComponentInParent<Canvas>().GetComponentInChildren<PlayerNameAreaView>(true);
            foreach (var group in names.GetComponents<UnityEngine.UI.LayoutGroup>()) group.enabled = false;
            FruitLabStyle.Place((RectTransform)names.transform, transform, 20, 146, width - 40, height - 368);
            foreach (var row in names.GetComponentsInChildren<PlayerNameView>()) row.ApplyRowLayout();
        }
    }

    public interface IWaitingSceneUIView
    {
        void SetStatus(string message);
        void SetLobbyNameText(string text);
        void SetLobbyCodeText(string text);
        IBaseButtonView GetMenuButton();
        IBaseButtonView GetReadyButton();
    }
}
