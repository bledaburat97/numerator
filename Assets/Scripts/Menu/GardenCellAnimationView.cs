using DG.Tweening;
using Scripts.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public sealed class GardenCellAnimationView : MonoBehaviour
    {
        private Image _plant,_previous;
        private RectTransform _badge;
        private GardenCellBurstGraphic _burst;
        private Sequence _motion;
        private GardenGrowthStage _stage,_previousStage;
        private float _width,_height;
        public bool IsAnimating=>_motion!=null && _motion.IsActive();
        public static bool CanAnimate=>Application.isPlaying || DOTween.defaultUpdateType==UpdateType.Manual;

        public void Configure(Image plant,RectTransform badge)
        {
            _plant=plant;_badge=badge;
            _previous=GardenPlotView.Art(transform,"PreviousPlant",null);_previous.preserveAspect=true;
            _previous.transform.SetSiblingIndex(_plant.transform.GetSiblingIndex());_previous.gameObject.SetActive(false);
            var root=GardenUi.Rect(transform,"CellParticles");
            if(root.GetComponent<CanvasRenderer>()==null)root.gameObject.AddComponent<CanvasRenderer>();
            _burst=root.GetComponent<GardenCellBurstGraphic>() ?? root.gameObject.AddComponent<GardenCellBurstGraphic>();
            _burst.raycastTarget=false;_burst.gameObject.SetActive(false);
        }
        public void Layout(float width,float height,GardenGrowthStage stage)
        {
            _width=width;_height=height;_stage=stage;
            PlacePlant(_plant.rectTransform,width,height,stage);
            if(_previous!=null)PlacePlant(_previous.rectTransform,width,height,_previousStage);
            if(_burst!=null)Scripts.FruitLabStyle.Stretch(_burst.rectTransform);
        }
        private static void PlacePlant(RectTransform rect,float width,float height,GardenGrowthStage stage)
        {
            float side=Mathf.Min(width*.85f,height*.57f);
            if(stage==GardenGrowthStage.Seed)side=Mathf.Min(side,Mathf.Max(12,side*.4f));
            else if(stage==GardenGrowthStage.Sprout)side=Mathf.Min(side,Mathf.Max(19,side*.7f));
            // PreserveAspect uses this bottom pivot too, so wide vines and tall
            // saplings stand on the same ground point without a stage-change jump.
            rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=new Vector2(.5f,0);
            rect.anchoredPosition=new Vector2(width*.5f,height*.43f);rect.sizeDelta=Vector2.one*side;
        }
        public void PlayPlanting(float delay=0)
        {
            Cancel();if(!CanAnimate || !isActiveAndEnabled || !_plant.gameObject.activeSelf)return;
            _plant.transform.localScale=Vector3.one*.85f;PrepareBurst(GardenCellEffect.Soil);
            _motion=DOTween.Sequence().AppendInterval(delay)
                .Append(_plant.transform.DOScale(1,.28f).SetEase(Ease.OutCubic))
                .Join(_badge.DOPunchScale(Vector3.one*.06f,.22f,2,.7f))
                .Join(DOVirtual.Float(0,1,.38f,value=>_burst.Progress=value));
            OwnMotion();
        }
        public void PlayGrowth(Sprite previous,GardenGrowthStage previousStage,float delay=0)
        {
            Cancel();if(!CanAnimate || !isActiveAndEnabled || previous==null || !_plant.gameObject.activeSelf)return;
            _previousStage=previousStage;_previous.sprite=previous;_previous.color=Color.white;_previous.gameObject.SetActive(true);
            PlacePlant(_previous.rectTransform,_width,_height,previousStage);
            _plant.color=new Color(1,1,1,0);_plant.transform.localScale=Vector3.one*.82f;
            PrepareBurst(_stage==GardenGrowthStage.Flower?GardenCellEffect.Flower:
                _stage==GardenGrowthStage.Fruit?GardenCellEffect.Ripe:GardenCellEffect.Growth);
            _motion=DOTween.Sequence().AppendInterval(delay)
                .Append(_plant.DOFade(1,.22f)).Join(_previous.DOFade(0,.18f))
                .Join(_plant.transform.DOScale(1,.32f).SetEase(Ease.OutCubic))
                .Join(DOVirtual.Float(0,1,.40f,value=>_burst.Progress=value));
            OwnMotion();
        }
        private void PrepareBurst(GardenCellEffect effect)
        { _burst.Effect=effect;_burst.Progress=0;_burst.gameObject.SetActive(true); }
        private void OwnMotion()
        {
            _motion.SetUpdate(true).SetLink(gameObject,LinkBehaviour.KillOnDisable).OnComplete(()=>{_motion=null;ResetVisuals();});
        }
        public void Cancel(){_motion?.Kill(false);_motion=null;ResetVisuals();}
        private void ResetVisuals()
        {
            if(_plant!=null){_plant.color=Color.white;_plant.transform.localScale=Vector3.one;}
            if(_badge!=null)_badge.localScale=Vector3.one;
            if(_previous!=null){_previous.gameObject.SetActive(false);_previous.sprite=null;}
            if(_burst!=null){_burst.Progress=0;_burst.gameObject.SetActive(false);}
        }
        private void OnDisable()=>Cancel();
        private void OnDestroy()=>Cancel();
    }
}
