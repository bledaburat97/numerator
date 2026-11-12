using System;
using DG.Tweening;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // Owns only the reward's visual receipt. Inventory is committed by the session before this opens.
    public sealed class PowerUpRewardView : MonoBehaviour
    {
        private CanvasGroup _group;
        private Image _hero, _inventory;
        private TMP_Text _title, _count;
        [NonSerialized] private Button _claim;
        private RewardBurstGraphic _burst;
        private Tween _ambient;
        private AudioSource _audio;
        private AudioClip _chime;
        private int _finalCount;
        public Button ClaimButton => _claim;
        public RectTransform Hero => _hero.rectTransform;
        public bool IsVisible => gameObject.activeSelf;

        public static PowerUpRewardView Ensure(Canvas canvas)
        {
            var view = canvas.GetComponentInChildren<PowerUpRewardView>(true);
            bool created = view == null;
            if (created)
            {
                var root = new GameObject("PowerUpReward", typeof(RectTransform), typeof(CanvasGroup));
                root.transform.SetParent(canvas.transform, false);
                view = root.AddComponent<PowerUpRewardView>();
            }
            view.EnsureUi();
            if(created)view.Hide();
            return view;
        }
        private void EnsureUi()
        {
            if (_claim != null && _hero != null) return;
            FruitLabStyle.Stretch((RectTransform)transform);
            _group = GetComponent<CanvasGroup>();
            var shade = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            shade.color = new Color(.025f,.055f,.065f,.91f); shade.raycastTarget = true;
            var burst = transform.Find("Burst") as RectTransform ?? FruitLabOnlineLayout.Rect("Burst", transform);
            if(burst.GetComponent<CanvasRenderer>()==null)burst.gameObject.AddComponent<CanvasRenderer>();
            _burst = burst.GetComponent<RewardBurstGraphic>() ?? burst.gameObject.AddComponent<RewardBurstGraphic>();
            _burst.raycastTarget = false;
            _hero = FruitLabStyle.Detail(transform,"Reward",Color.white);
            var quantity=_hero.transform.Find("Quantity");if(quantity!=null)quantity.gameObject.SetActive(false);
            _title = FriendUi.Text(transform,"Title","Güçlendirici kazandın!",26);
            _title.fontStyle = FontStyles.Bold; _title.color = FruitLabStyle.Gold;
            _inventory = FruitLabStyle.Detail(transform,"InventoryReward",Color.white);
            _count = FriendUi.Text(transform,"InventoryCount","",26);
            _count.alignment = TextAlignmentOptions.MidlineLeft;
            _claim = FriendUi.Button(transform,"Claim","Ödülü al",null,true);
            Layout();
        }
        private void Center(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one*.5f;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        public void Layout()
        {
            if (_hero == null) return;
            float width = ((RectTransform)transform).rect.width;
            float size = Mathf.Clamp(width/3f,100,152);
            Center(_hero.rectTransform,Vector2.zero,Vector2.one*size);
            Center(_burst.rectTransform,Vector2.zero,Vector2.one*size*2.1f);
            Center(_title.rectTransform,new Vector2(0,size*.5f+82),new Vector2(width-40,76));
            float countWidth=Mathf.Max(24,_count.GetPreferredValues().x+4);
            float totalWidth=40+10+countWidth;
            Center(_inventory.rectTransform,new Vector2(-totalWidth*.5f+20,-size*.5f-70),Vector2.one*40);
            Center(_count.rectTransform,new Vector2(-totalWidth*.5f+50+countWidth*.5f,-size*.5f-70),new Vector2(countWidth,42));
            Center((RectTransform)_claim.transform,new Vector2(0,-size*.5f-158),new Vector2(Mathf.Min(280,width-48),56));
        }
        public void Bind(RewardType reward, int finalCount, Action claim,bool bonus=false)
        {
            EnsureUi(); gameObject.SetActive(true); transform.SetAsLastSibling(); Layout();
            _finalCount = Mathf.Max(1,finalCount);
            var accent=bonus?LevelIdentity.For(new LevelData {BonusReward=reward}).Accent:FruitLabStyle.Gold;
            _title.color=accent;_burst.Accent=accent;_burst.SetVerticesDirty();
            FruitLabStyle.Surface(_claim.GetComponent<Image>(),bonus?accent:FruitLabStyle.Mint);
            _group.alpha = 0; _group.blocksRaycasts = _group.interactable = true;
            _hero.rectTransform.localScale = Vector3.one*.35f;
            _burst.rectTransform.localScale = Vector3.one*.65f; _burst.rectTransform.localRotation = Quaternion.identity;
            _count.rectTransform.localScale = Vector3.one; _claim.gameObject.SetActive(true);
            _count.text = _finalCount.ToString();Layout();
            FruitLabStyle.SetIcon(_hero,PowerUpTypeUtility.IconName(reward));
            FruitLabStyle.SetIcon(_inventory,PowerUpTypeUtility.IconName(reward));
            _claim.interactable = false; _claim.onClick.RemoveAllListeners(); _claim.onClick.AddListener(claim.Invoke);
        }
        public void EnableClaim() { if (_claim != null) _claim.interactable = true; }
        public Sequence PlayIntro()
        {
            _ambient?.Kill();
            _ambient = _burst.rectTransform.DOLocalRotate(new Vector3(0,0,360),24,RotateMode.FastBeyond360)
                .SetEase(Ease.Linear).SetLoops(-1).SetLink(gameObject,LinkBehaviour.KillOnDisable);
            return DOTween.Sequence().Append(_group.DOFade(1,.18f))
                .Insert(0,_hero.rectTransform.DOScale(1,.34f).SetEase(Ease.OutBack))
                .Insert(0,_burst.rectTransform.DOScale(1,.38f).SetEase(Ease.OutCubic))
                .SetLink(gameObject,LinkBehaviour.KillOnDisable);
        }
        public Sequence PlayClaim()
        {
            _claim.interactable = false;
            return DOTween.Sequence()
                .Append(_hero.rectTransform.DOAnchorPos(_inventory.rectTransform.anchoredPosition,.34f).SetEase(Ease.InOutCubic))
                .Join(_hero.rectTransform.DOSizeDelta(Vector2.one*40,.34f).SetEase(Ease.InOutCubic))
                .Join(_burst.DOFade(0,.22f))
                .AppendCallback(()=>_count.text=_finalCount.ToString())
                .Append(_count.rectTransform.DOPunchScale(Vector3.one*.16f,.18f,3,.6f))
                .Join(_inventory.DOColor(FruitLabStyle.Gold,.09f).SetLoops(2,LoopType.Yoyo))
                .Append(_group.DOFade(0,.15f)).SetLink(gameObject,LinkBehaviour.KillOnDisable);
        }
        public void PlaySound()
        {
            if (!Application.isPlaying) return;
            if (_audio == null) { _audio = gameObject.AddComponent<AudioSource>(); _audio.playOnAwake = false; _audio.spatialBlend = 0; }
            if (_chime == null)
            {
                const int rate = 22050; var samples = new float[(int)(rate*.36f)];
                for (int i=0;i<samples.Length;i++)
                {
                    float t=i/(float)rate;
                    for (int note=0;note<3;note++)
                    {
                        float start=note*.065f,age=t-start;
                        if(age<0)continue;
                        float frequency=start==0?659.25f:start<.1f?830.61f:987.77f;
                        samples[i]+=Mathf.Sin(age*frequency*Mathf.PI*2)*Mathf.Min(1,age*180)*Mathf.Exp(-age*18)*.16f;
                    }
                }
                _chime=AudioClip.Create("Powerup earned",samples.Length,1,rate,false);_chime.SetData(samples,0);
            }
            _audio.PlayOneShot(_chime,.7f);
        }
        public void Hide()
        {
            _ambient?.Kill(); _ambient=null;
            if (_group != null) _group.blocksRaycasts = _group.interactable = false;
            if (_claim != null) _claim.onClick.RemoveAllListeners();
            if (_burst != null) _burst.color = Color.white;
            if (_audio != null) _audio.Stop();
            gameObject.SetActive(false);
        }
        private void OnDestroy() { _ambient?.Kill(); if(_chime!=null)Destroy(_chime); }
    }
}
