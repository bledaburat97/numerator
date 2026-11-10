using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public readonly struct GardenHarvestVisual
    {
        public readonly Sprite Artwork;
        public readonly Vector3 Center,Right,Up;
        public GardenHarvestVisual(Sprite artwork,Vector3 center,Vector3 right,Vector3 up)
        {Artwork=artwork;Center=center;Right=right;Up=up;}
        public bool IsValid=>Artwork!=null;
    }

    // Visual acknowledgement only. Inventory is already committed before Play.
    // A bounded pool stays outside the scrolling mask and is owned by this page.
    public sealed class GardenHarvestPresentation : MonoBehaviour
    {
        private const int Capacity=6;
        private readonly Flight[] _flights=new Flight[Capacity];
        private int _next;
        private Tween _arrival;
        private RectTransform _destination;
        public int ActiveFlights
        { get {int count=0;foreach(var flight in _flights)if(flight!=null && flight.Root.gameObject.activeSelf)count++;return count;} }
        public void Play(GardenHarvestVisual visual,RectTransform destination)
        {
            if(!visual.IsValid || destination==null || !GardenCellAnimationView.CanAnimate || !isActiveAndEnabled)return;
            _destination=destination;transform.SetAsLastSibling();
            int index=_next;_next=(_next+1)%Capacity;
            var flight=_flights[index] ?? (_flights[index]=new Flight(transform,index));flight.Stop();
            var overlay=(RectTransform)transform;
            flight.Root.gameObject.SetActive(true);flight.Art.sprite=visual.Artwork;flight.Art.color=Color.white;
            Vector2 start=overlay.InverseTransformPoint(visual.Center);
            Vector2 size=new Vector2(overlay.InverseTransformVector(visual.Right).magnitude,overlay.InverseTransformVector(visual.Up).magnitude);
            flight.Root.anchoredPosition=start;flight.Root.sizeDelta=size;
            flight.Motion=DOTween.Sequence().Append(DOVirtual.Float(0,1,.48f,value=>{
                if(_destination==null)return;
                Vector2 target=overlay.InverseTransformPoint(_destination.TransformPoint(_destination.rect.center));
                flight.Root.anchoredPosition=Vector2.Lerp(start,target,value)+Vector2.up*(Mathf.Sin(value*Mathf.PI)*36);
                flight.Root.sizeDelta=Vector2.Lerp(size,Vector2.one*16,value);
                flight.Art.color=new Color(1,1,1,1-Mathf.Clamp01((value-.8f)*5));
            }).SetEase(Ease.InOutSine)).OnComplete(()=>{
                flight.Root.gameObject.SetActive(false);flight.Motion=null;
                if(_destination!=null){_arrival?.Kill(false);_destination.localScale=Vector3.one;_arrival=_destination.DOPunchScale(Vector3.one*.08f,.18f,2,.6f).SetUpdate(true).SetLink(gameObject,LinkBehaviour.KillOnDisable);}
            }).SetUpdate(true).SetLink(gameObject,LinkBehaviour.KillOnDisable);
        }
        public void Cancel()
        {
            foreach(var flight in _flights)flight?.Stop();_arrival?.Kill(false);_arrival=null;
            if(_destination!=null)_destination.localScale=Vector3.one;_destination=null;
        }
        private void OnDisable()=>Cancel();
        private void OnDestroy()=>Cancel();
        private sealed class Flight
        {
            public readonly RectTransform Root;
            public readonly Image Art;
            public Sequence Motion;
            public Flight(Transform parent,int index)
            {
                Art=GardenPlotView.Art(parent,"HarvestFruit"+index,null);Root=Art.rectTransform;Art.preserveAspect=true;
                Root.anchorMin=Root.anchorMax=Root.pivot=Vector2.one*.5f;
                Root.gameObject.SetActive(false);
            }
            public void Stop(){Motion?.Kill(false);Motion=null;if(Root!=null)Root.gameObject.SetActive(false);}
        }
    }
}
