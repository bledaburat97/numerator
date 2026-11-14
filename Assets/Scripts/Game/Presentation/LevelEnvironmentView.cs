using System;
using DG.Tweening;
using Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scripts
{
    // All state here is presentation-only; it never reads the hidden answer or writes a checkpoint.
    public sealed class LevelEnvironmentView : MonoBehaviour
    {
        private LevelData _definition;
        private GameUIView _game;
        private RectTransform _board,_middle,_intro;
        private CanvasGroup _introGroup;
        private TMP_Text _introTitle;
        private Image _introReward,_bonusPreview;
        private FruitLabGlyph _introIcon,_badge;
        private readonly Image[] _steps=new Image[3];
        private readonly Image[] _future=new Image[2];
        private int _active,_guesses;
        private Sequence _transition;
        private Tween _hintFill;
        private bool _fullHint;
        public void PulseHintProgress()
        {
            if(_definition?.Mechanic!=LevelMechanic.Helper)return;
            _hintFill?.Kill();_fullHint=true;RefreshMechanic(LevelIdentity.For(_definition));
            _hintFill=DOVirtual.DelayedCall(.24f,()=>{_fullHint=false;if(_definition!=null)RefreshMechanic(LevelIdentity.For(_definition));})
                .SetLink(gameObject,LinkBehaviour.KillOnDestroy);
        }
        public static LevelEnvironmentView Ensure(Canvas canvas)
        {return canvas.GetComponent<LevelEnvironmentView>() ?? canvas.gameObject.AddComponent<LevelEnvironmentView>();}
        public void Render(LevelData definition,int active,int guesses)
        {
            ResetPresentation();
            if(guesses==0 || _definition?.Mechanic!=definition?.Mechanic){_hintFill?.Kill();_fullHint=false;}
            _definition=definition?.Copy();_active=active;_guesses=guesses;
            _game=GetComponentInChildren<GameUIView>(true);
            _board=GetComponentInChildren<BoardAreaView>(true).GetRectTransform();
            _middle=_board.parent.parent as RectTransform;
            RefreshLayout();
        }
        public void RefreshLayout()
        {
            if(_definition==null || _game==null || _game.IsMultiplayer)return;
            var identity=LevelIdentity.For(_definition);
            FruitLabStyle.AddSurface(gameObject,identity.Background);
            var headerBand=transform.Find("SafeArea/GameHeaderBand")?.GetComponent<Image>();
            if(headerBand!=null)headerBand.color=identity.Background;
            var middle=transform.Find("SafeArea/MiddleArea") as RectTransform ?? _middle;
            if(middle!=null)FruitLabStyle.ApplyGameBackdrop(middle,identity.Background);
            var layout=GetComponent<FruitLabGameLayout>();
            if(layout!=null)FruitLabStyle.AddSurface(layout.HistoryArea.gameObject,Color.Lerp(identity.Background,identity.Panel,.55f));
            if(_board.parent!=null)FruitLabStyle.AddSurface(_board.parent.gameObject,Color.Lerp(identity.Background,FruitLabStyle.Panel,.35f));
            FruitLabStyle.AddSurface(_board.gameObject,identity.Panel);
            var rim=_board.Find("LabRim")?.GetComponent<Image>();if(rim!=null)rim.color=identity.Accent*.7f;
            if(_game.GetCheckButton() is BaseButtonView check)ButtonAppearance.For(check).ApplyActionAccent(identity.Accent);
            RefreshMechanic(identity);RefreshFuture(identity);
        }
        private void RefreshMechanic(LevelIdentity identity)
        {
            var rect=_game.MechanicStatusRect;
            if(rect==null)return;
            if(_badge==null){_badge=FruitLabGlyph.Create(rect,identity.Symbol,identity.Accent);_badge.name="LevelKind";}
            _badge.Type=identity.Symbol;_badge.color=identity.Accent;_badge.SetAllDirty();
            FruitLabStyle.Place(_badge.rectTransform,rect,0,-3,20,20);
            int count=_definition.Mechanic==LevelMechanic.Helper?3:_definition.Mechanic==LevelMechanic.Growing?2:0;
            int filled=count==3?(_fullHint?3:_guesses%3):_active>=GrowingBoardState.MaximumCount?2:_guesses%2;
            if(_bonusPreview==null)_bonusPreview=FruitLabStyle.Detail(rect,"BonusReward",Color.white);
            _bonusPreview.gameObject.SetActive(_definition.BonusReward.HasValue);
            if(_definition.BonusReward.HasValue){FruitLabStyle.SetIcon(_bonusPreview,PowerUpTypeUtility.IconName(_definition.BonusReward.Value));FruitLabStyle.Place(_bonusPreview.rectTransform,rect,rect.rect.width-22,-3,20,20);}
            var label=rect.GetComponent<TMP_Text>();label.color=identity.Accent;label.margin=new Vector4(24,0,_definition.BonusReward.HasValue?26:count>0?count*16+4:0,0);label.alignment=TextAlignmentOptions.MidlineLeft;
            for(int i=0;i<_steps.Length;i++)
            {
                if(_steps[i]==null)_steps[i]=FruitLabStyle.Detail(rect,"MechanicStep"+i,FruitLabStyle.Border);
                _steps[i].gameObject.SetActive(i<count);
                if(i<count){_steps[i].color=i<filled?identity.Accent:FruitLabStyle.Border*.6f;FruitLabStyle.Place(_steps[i].rectTransform,rect,rect.rect.width-count*16+i*16,3,11,8);}
            }
        }
        private void RefreshFuture(LevelIdentity identity)
        {
            int remaining=_definition.Mechanic==LevelMechanic.Growing?Mathf.Clamp(4-_active,0,2):0;
            var holders=_board.GetComponentsInChildren<BoardHolderView>();
            float right=float.NegativeInfinity,centerY=0,side=0,holderWidth=0;
            foreach(var holder in holders)
            {
                var rect=holder.GetRectTransform();var center=_board.InverseTransformPoint(rect.position);
                // Growth scales the new holder. Future slots use its final bounds,
                // so their positions never depend on an animation's current scale.
                right=Mathf.Max(right,center.x+rect.rect.width*.5f);centerY=center.y;side=rect.rect.height;holderWidth=rect.rect.width;
            }
            if(side<=0){remaining=0;right=0;}
            float ghostWidth=holderWidth;
            float spacing=holderWidth*3f/70f;
            for(int i=0;i<_future.Length;i++)
            {
                if(_future[i]==null){_future[i]=FruitLabStyle.Detail(_board,"FutureSlot"+i,identity.Accent,true);var plus=FruitLabStyle.CreateText("Plus",_future[i].transform,16,identity.Accent);plus.text="+";FruitLabStyle.Stretch(plus.rectTransform);}
                _future[i].gameObject.SetActive(i<remaining);
                if(i>=remaining)continue;
                _future[i].color=new Color(identity.Accent.r,identity.Accent.g,identity.Accent.b,.32f);
                FruitLabStyle.Place(_future[i].rectTransform,_board,right-_board.rect.xMin+spacing+i*(ghostWidth+spacing),centerY-_board.rect.yMin-side*.5f,ghostWidth,side);
            }
        }
        public Sequence PlayIntro()
        {
            ResetPresentation();
            if(_definition==null || _game==null || _game.IsMultiplayer)return null;
            var identity=LevelIdentity.For(_definition);
            if(string.IsNullOrEmpty(identity.IntroTitle))return null;
            if(_intro==null)
            {
                _intro=FruitLabOnlineLayout.Panel("LevelIntro",transform);
                _introGroup=_intro.gameObject.AddComponent<CanvasGroup>();_introGroup.blocksRaycasts=_introGroup.interactable=false;
                _introIcon=FruitLabGlyph.Create(_intro,identity.Symbol,identity.Accent);
                _introTitle=FruitLabStyle.CreateText("Title",_intro,24,FruitLabStyle.Text);
                _introTitle.textWrappingMode=TextWrappingModes.Normal;
                _introReward=FruitLabStyle.Detail(_intro,"BonusReward",Color.white);
            }
            _intro.gameObject.SetActive(true);_intro.SetAsLastSibling();_introGroup.alpha=0;
            _intro.anchorMin=_intro.anchorMax=_intro.pivot=Vector2.one*.5f;
            _intro.sizeDelta=new Vector2(Mathf.Min(320,((RectTransform)transform).rect.width-40),100);_intro.anchoredPosition=Vector2.zero;
            FruitLabStyle.AddSurface(_intro.gameObject,identity.Panel);
            _introIcon.Type=identity.Symbol;_introIcon.color=identity.Accent;_introIcon.SetAllDirty();
            FruitLabStyle.Place(_introIcon.rectTransform,_intro,14,30,40,40);
            _introTitle.text=identity.IntroTitle;FruitLabStyle.Place(_introTitle.rectTransform,_intro,66,18,_intro.rect.width-80,64);
            _introReward.gameObject.SetActive(_definition.BonusReward.HasValue);
            if(_definition.BonusReward.HasValue){FruitLabStyle.SetIcon(_introReward,PowerUpTypeUtility.IconName(_definition.BonusReward.Value));FruitLabStyle.Place(_introReward.rectTransform,_intro,_intro.rect.width-58,30,40,40);FruitLabStyle.Place(_introTitle.rectTransform,_intro,66,18,_intro.rect.width-130,64);}
            float exitX=(((RectTransform)transform).rect.width+_intro.rect.width)*.5f+24;
            return _transition=DOTween.Sequence().Append(_introGroup.DOFade(1,.14f)).AppendInterval(1f)
                .Append(_intro.DOAnchorPosX(exitX,.38f).SetEase(Ease.InCubic))
                .AppendCallback(()=>_intro.gameObject.SetActive(false)).SetLink(gameObject);
        }
        public void ResetPresentation(){_transition?.Kill();if(_intro!=null)_intro.gameObject.SetActive(false);}
        private void OnDestroy(){_transition?.Kill();_hintFill?.Kill();}
    }
}
