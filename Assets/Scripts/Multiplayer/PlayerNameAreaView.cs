using System;
using UnityEngine;

namespace Scripts
{
    public class PlayerNameAreaView : MonoBehaviour, IPlayerNameAreaView
    {
        [SerializeField] private PlayerNameView playerNamePrefab;
        private PlayerNameViewFactory _playerNameViewFactory;
        private Action _onDestroyAction;
        public int VisibleSlots { get; private set; } = 2;
        public void SetParticipantCount(int count)
        {
            VisibleSlots = count <= 2 ? 2 : 4;
            for (int i = 0; i < 4; i++)
            {
                var old = transform.Find("EmptyPlayer" + i) as RectTransform;
                if (old == null) old = FruitLabOnlineLayout.Panel("EmptyPlayer" + i, transform);
                FruitLabStyle.Surface(old.GetComponent<UnityEngine.UI.Image>(),new Color32(227,230,209,255));
                var rim=old.Find("LabRim");if(rim!=null)rim.gameObject.SetActive(false);
                old.gameObject.SetActive(i >= count && i < VisibleSlots);
                float width = (((RectTransform)transform).rect.width - 10) * .5f;
                float height = VisibleSlots <= 2 ? Mathf.Min(194, ((RectTransform)transform).rect.height) : (((RectTransform)transform).rect.height - 10) * .5f;
                old.anchorMin = old.anchorMax = old.pivot = new Vector2(0, 1);
                old.sizeDelta = new Vector2(width, height); old.anchoredPosition = new Vector2(i % 2 * (width + 10), -(i / 2) * (height + 10));
                var icon = FruitLabStyle.Detail(old, "WaitingIcon", new Color(1, 1, 1, .38f)); icon.sprite = FruitLabStyle.Icon("Pair"); icon.type = UnityEngine.UI.Image.Type.Simple; icon.preserveAspect = true;
                FruitLabStyle.Place(icon.rectTransform, old, (width - 44) * .5f, height - 58, 44, 44);
                var text = FriendUi.Text(old, "WaitingText", "Arkadaşın\nbekleniyor…", 17); text.color = PopupArtStyle.Body;
                FruitLabStyle.Place(text.rectTransform, old, 5, 8, width - 10, 48);
            }
            foreach (var row in GetComponentsInChildren<PlayerNameView>()) row.ApplyRowLayout();
        }
        public void Init(Action onDestroyAction)
        {
            _onDestroyAction = onDestroyAction;
        }

        public IPlayerNameView CreatePlayerNameView()
        {
            _playerNameViewFactory = new PlayerNameViewFactory();
            IPlayerNameView playerNameView = _playerNameViewFactory.Spawn(transform, playerNamePrefab);
            return playerNameView;
        }

        private void OnDestroy()
        {
            Debug.Log("OnDestroyPlayerNameAreaView");
            _onDestroyAction?.Invoke();
        }
    }

    public interface IPlayerNameAreaView
    {
        void SetParticipantCount(int count);
        void Init(Action onDestroyAction);
        IPlayerNameView CreatePlayerNameView();
    }
}