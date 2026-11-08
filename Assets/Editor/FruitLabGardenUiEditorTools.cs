using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Game;
using Menu;
using Newtonsoft.Json;
using Scripts;
using Scripts.Economy;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using Object=UnityEngine.Object;

public static partial class FruitLabTutorialEditorTools
{
    public static void ReviewGardenUi(Canvas gameCanvas,Camera camera)
    {
        var errors=new List<string>();string before=Profile();
        Application.LogCallback collect=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message);};
        Application.logMessageReceived+=collect;
        var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/Menu.unity");
        GameObject clone=null,events=null,applicationRoot=null;float oldOrtho=camera.orthographicSize;
        gameCanvas.gameObject.SetActive(false);
        try
        {
            clone=Object.Instantiate(Find<FruitLabMenuLayout>(source).gameObject);SceneManager.MoveGameObjectToScene(clone,camera.gameObject.scene);
            var canvas=clone.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var rect=(RectTransform)clone.transform;rect.localScale=Vector3.one;rect.position=Vector3.zero;rect.sizeDelta=new Vector2(375,812);
            clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();camera.orthographicSize=Mathf.Max(406,375/(2*camera.aspect));
            events=new GameObject("GardenUiEvents",typeof(EventSystem));SceneManager.MoveGameObjectToScene(events,camera.gameObject.scene);
            var eventSystem=events.GetComponent<EventSystem>();
            var clock=new GardenUiClock();
            var profiles=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>{
                var profile=new PlayerProfileData();profile.Progress.LevelId=17;profile.Wallet.Coins=50;profile.Wallet.CrystalProgress=5;
                foreach(RewardType type in Enum.GetValues(typeof(RewardType)))profile.Wallet.PowerUps[type]=2;return profile;
            });
            var garden=new GardenService(profiles,new EconomySettings(),clock);var visits=new GardenVisitCoordinator(garden);
            var menu=clone.GetComponentInChildren<MenuUIView>(true);var view=menu.GetCollectionView();
            var notice=menu.GetGardenNoticeView();var inventory=new FruitInventoryService(profiles);
            var bindings=new DiContainer();bindings.Bind<IHapticController>().To<HapticController>().AsSingle();
            bindings.BindFactory<IBaseButtonView,Action,IBaseButtonController,BaseButtonControllerFactory>().To<BaseButtonController>();
            var buttons=bindings.Resolve<BaseButtonControllerFactory>();
            using(var presenter=new FruitCollectionPresenter(inventory,profiles,view,buttons,garden,visits,notice))
            using(var header=new MenuHeaderController(clone.GetComponentInChildren<MenuHeaderView>(true),profiles))
            {
                var trackerRoot=new GameObject("GardenUiTracker");trackerRoot.SetActive(false);trackerRoot.transform.SetParent(clone.transform,false);
                var tracker=trackerRoot.AddComponent<LevelTracker>();tracker.SuppressPersistenceForTests=true;Field(tracker,"_levelId",17);
                new MenuUIController(tracker,new MemorySave(),buttons,menu,presenter);header.Initialize(tracker);
                applicationRoot=new GameObject("GardenApplicationEvents");applicationRoot.SetActive(false);
                SceneManager.MoveGameObjectToScene(applicationRoot,camera.gameObject.scene);
                var app=applicationRoot.AddComponent<GlobalContext>();Field(app,"_gardenVisits",visits);GardenApp(app,"Start");
                presenter.ShowPendingNotice();Require(!notice.IsVisible,"Empty startup has no growth announcement");
                TapCollectionButton(canvas,camera,eventSystem,(BaseButtonView)menu.GetCollectionButton());
                var panel=(GardenPanelView)view.Garden;var popup=(SeedPurchasePopupView)view.Purchase;
                Require(view.IsVisible && panel.gameObject.activeSelf,"Bahçem initially opens its garden tab");
                TapGarden(canvas,camera,eventSystem,panel.BuyButton);
                TapGarden(canvas,camera,eventSystem,popup.FruitButton(FruitId.Strawberry));popup.QuantityInput.text="10";
                Require(popup.ConfirmButton.interactable && popup.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="Toplam: 10 coin")
                    && popup.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="Kalan: 40 coin"),"Purchase preview shows fruit, quantity, total and remaining coins");
                TapGarden(canvas,camera,eventSystem,popup.CancelButton);
                Require(profiles.Snapshot().Wallet.Coins==50 && garden.GetBatches().Count==0,"Cancel never spends coins");
                TapGarden(canvas,camera,eventSystem,panel.BuyButton);
                var outside=popup.GetComponent<Button>();var popupRect=(RectTransform)popup.transform;
                TapGarden(canvas,camera,eventSystem,outside,popupRect.TransformPoint(popupRect.rect.min+new Vector2(4,4)));
                Require(!popup.gameObject.activeSelf && profiles.Snapshot().Wallet.Coins==50,"Tapping outside the modal cancels safely");
                TapGarden(canvas,camera,eventSystem,panel.BuyButton);
                TapGarden(canvas,camera,eventSystem,popup.FruitButton(FruitId.Strawberry));popup.QuantityInput.text="0";
                Require(!popup.ConfirmButton.interactable,"Zero quantity cannot be bought");
                popup.QuantityInput.text="1000";Require(!popup.ConfirmButton.interactable,"Unaffordable quote disables purchase");
                popup.QuantityInput.text="10";
                Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-seed-purchase.png");
                TapGarden(canvas,camera,eventSystem,popup.ConfirmButton);popup.ConfirmButton.onClick.Invoke();
                Require(!popup.gameObject.activeSelf && profiles.Snapshot().Wallet.Coins==40 && garden.GetBatches().Single().Value.Quantity==10,
                    "Actual UI purchase debits once, creates a batch and ignores a duplicate confirmation");
                Require(clone.GetComponentInChildren<MenuHeaderView>(true).GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text=="40"),"Menu coin counter refreshes after purchase");
                Require(panel.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="10 adet") && panel.GetComponentsInChildren<TMP_Text>().Any(t=>t.text=="Tohum"),"New batch is displayed with amount and stage");
                notice.Hide();Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-garden-seeds.png");
                TapCollectionButton(canvas,camera,eventSystem,(BaseButtonView)view.CloseButton);
                Require(!view.IsVisible && !garden.ProcessVisit().HasProgress,"Closing and reopening menu cannot grow purchase-day seeds");

                clock.Day(1);GardenApp(app,"OnApplicationFocus",false);GardenApp(app,"OnApplicationPause",true);
                GardenApp(app,"OnApplicationPause",false);
                Require(garden.GetBatches().Single().Value.Stage==GardenGrowthStage.Seed,"Paired focus/pause events wait for real foreground");
                GardenApp(app,"OnApplicationFocus",true);
                Require(garden.GetBatches().Single().Value.Stage==GardenGrowthStage.Sprout && notice.IsVisible,"Global foreground grows the garden without opening Bahçem");
                long revision=profiles.Snapshot().Revision;
                GardenApp(app,"OnApplicationPause",false);GardenApp(app,"OnApplicationFocus",true);GardenApp(app,"Start");
                Require(profiles.Snapshot().Revision==revision,"Duplicate foreground/start events never advance twice");
                Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-garden-growth-notice.png");
                presenter.Dispose();clock.Day(40);GardenApp(app,"OnApplicationPause",true);GardenApp(app,"OnApplicationPause",false);
                Require(!notice.IsVisible && garden.GetBatches().Single().Value.Stage==GardenGrowthStage.Leaves,"Growth continues in another scene and long absence gives one stage");
                using(var returning=new FruitCollectionPresenter(inventory,profiles,view,buttons,garden,visits,notice))
                {
                    returning.ShowPendingNotice();Require(notice.IsVisible,"Queued game-scene progress is shown when the menu is ready");
                    notice.Hide();returning.ShowPendingNotice();Require(!notice.IsVisible,"Menu recreation never repeats an already shown notice");
                    clock.Day(41);GardenApp(app,"OnApplicationPause",true);GardenApp(app,"OnApplicationPause",false);
                    Require(garden.GetBatches().Single().Value.Stage==GardenGrowthStage.Flower,"Third entry reaches flower");
                    clock.Day(42);GardenApp(app,"OnApplicationFocus",false);GardenApp(app,"OnApplicationFocus",true);
                    Require(garden.GetBatches().Count==0 && inventory.GetCollection()[FruitId.Strawberry].Current==10,"Fourth entry automatically moves fruit into collection");
                    Require(notice.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("10 çilek") && t.text.Contains("koleksiyonuna eklendi")),"Harvest announcement names the actual fruit and quantity");
                    Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-garden-harvest-notice.png");
                    revision=profiles.Snapshot().Revision;notice.Hide();
                    GardenApp(app,"OnApplicationPause",true);GardenApp(app,"OnApplicationPause",false);
                    Require(!notice.IsVisible && profiles.Snapshot().Revision==revision,"Repeated same-day resume has no announcement or second harvest");
                    returning.Open();TapGarden(canvas,camera,eventSystem,view.GetTabButton(GardenTab.Collection));
                    var cells=(FruitCollectionCell[])Get(view,"cells");Require(cells.Single(c=>c.Fruit==FruitId.Strawberry).Amount.text=="10","Collection tab contains the actual harvest");
                    Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-garden-collection.png");
                    GardenLayouts(canvas,camera,eventSystem,clone,view,popup,panel,returning,profiles);
                }
            }
            Require(errors.Count==0,"Garden UI has no Unity errors: "+string.Join(" | ",errors));
            Require(before==Profile(),"Real player preferences stay unchanged");
            File.WriteAllText(Folder+"/garden-ui-review.json",JsonConvert.SerializeObject(new {passed=true,phone_layouts=4,
                actual_raycast_purchase=true,cancel_outside=true,duplicate_click=true,global_focus_pause=true,
                menu_reentry_no_growth=true,queued_notice=true,harvest_to_collection=true,real_player_unchanged=true},Formatting.Indented));
        }
        finally
        {
            Application.logMessageReceived-=collect;
            if(applicationRoot!=null)Object.DestroyImmediate(applicationRoot);if(events!=null)Object.DestroyImmediate(events);
            if(clone!=null)Object.DestroyImmediate(clone);EditorSceneManager.ClosePreviewScene(source);
            gameCanvas.gameObject.SetActive(true);camera.orthographicSize=oldOrtho;
        }
    }

    private static void GardenLayouts(Canvas canvas,Camera camera,EventSystem events,GameObject clone,FruitCollectionView view,
        SeedPurchasePopupView popup,GardenPanelView panel,FruitCollectionPresenter presenter,IPlayerProfileRepository profiles)
    {
        profiles.Update(profile=>{
            for(int index=0;index<12;index++)profile.Garden.Add("layout:"+index,new PlantingBatch {Fruit=(FruitId)(index%9+1),Quantity=1000,
                PlantedUtcDay=100,LastGrowthUtcDay=100+index%4,Stage=(GardenGrowthStage)(index%4)});
        });
        foreach(var size in new[]{new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
        {
            ((RectTransform)canvas.transform).sizeDelta=size;camera.orthographicSize=Mathf.Max(size.y*.5f,size.x/(2*camera.aspect));
            clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();presenter.Open();view.ApplyLayout();panel.ApplyLayout();
            var list=panel.GetComponentsInChildren<ScrollRect>().Single();
            Require(list.content.childCount==12 && list.content.rect.height>list.viewport.rect.height,"Quantities use batch rows with working scroll");
            TapGarden(canvas,camera,events,panel.BuyButton);
            var popupPanel=(RectTransform)popup.transform.Find("Panel");
            Require(popupPanel.rect.width<=size.x-24 && popupPanel.rect.height<=size.y-24,"Purchase popup fits phone bounds");
            foreach(FruitId fruit in Enum.GetValues(typeof(FruitId)))TapGarden(canvas,camera,events,popup.FruitButton(fruit));
            foreach(var label in popup.GetComponentsInChildren<TMP_Text>(false))
            {label.ForceMeshUpdate();Require(!label.isTextOverflowing,"Purchase text fits: "+label.name+" "+label.text);}
            TapGarden(canvas,camera,events,popup.CancelButton);
            TapGarden(canvas,camera,events,view.GetTabButton(GardenTab.Collection));
            foreach(var cell in (FruitCollectionCell[])Get(view,"cells"))
            {
                Require(((RectTransform)cell.Card.transform).rect.width>=50,"Collection fruit remains readable on small screens");
                foreach(var text in new[]{cell.Name,cell.Amount}){text.ForceMeshUpdate();Require(!text.isTextOverflowing,"Collection labels fit: "+text.text);}
            }
            if(size.x==375 && size.y==812){view.SelectTab(GardenTab.Garden);Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-garden-batches.png");}
            presenter.Close();
        }
    }
    private static void TapGarden(Canvas canvas,Camera camera,EventSystem events,Button button,Vector3? world=null)
    {
        Layer(canvas.transform);Draw(camera);var rect=(RectTransform)button.transform;
        var pointer=new PointerEventData(events) {position=RectTransformUtility.WorldToScreenPoint(camera,world??rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();canvas.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
        Require(hits.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject,
            "Garden control receives actual input: "+button.name+"; top="+hits.FirstOrDefault().gameObject?.name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerDownHandler);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerUpHandler);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
    private static void GardenApp(GlobalContext app,string method,params object[] arguments)
        =>typeof(GlobalContext).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,arguments);
    private sealed class GardenUiClock : IGardenClock
    {
        public DateTime UtcNow {get;private set;}=new DateTime(2026,11,1,12,0,0,DateTimeKind.Utc);
        public void Day(int day)=>UtcNow=new DateTime(2026,11,1,12,0,0,DateTimeKind.Utc).AddDays(day);
    }
}
