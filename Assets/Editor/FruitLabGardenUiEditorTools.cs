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
            var canvas=clone.GetComponent<Canvas>();
            // Isolate legacy modal/presenter checks; footer routing and raycasts are covered by navigation-review.
            clone.GetComponentInChildren<MainNavigationView>(true)?.gameObject.SetActive(false);canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
            var rect=(RectTransform)clone.transform;rect.localScale=Vector3.one;rect.position=Vector3.zero;rect.sizeDelta=new Vector2(375,812);
            clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();camera.orthographicSize=Mathf.Max(406,375/(2*camera.aspect));
            events=new GameObject("GardenUiEvents",typeof(EventSystem));SceneManager.MoveGameObjectToScene(events,camera.gameObject.scene);
            var eventSystem=events.GetComponent<EventSystem>();
            var clock=new GardenUiClock();
            var profiles=new PlayerProfileRepository(new MemoryPlayerProfileStorage(),()=>{
                var profile=new PlayerProfileData();profile.Progress.LevelId=17;profile.Wallet.Coins=50;profile.Wallet.CrystalProgress=5;
                foreach(RewardType type in Enum.GetValues(typeof(RewardType)))profile.Wallet.PowerUps[type]=2;return profile;
            });
            var garden=new GardenService(profiles,new EconomySettings(),clock,new AchievementRules(new EconomySettings()));var visits=new GardenVisitCoordinator(garden);
            var menu=clone.GetComponentInChildren<MenuUIView>(true);var view=menu.GetCollectionView();
            var notice=menu.GetGardenNoticeView();var inventory=new FruitInventoryService(profiles,new AchievementRules(new EconomySettings()));
            var bindings=new DiContainer();bindings.Bind<IHapticController>().To<HapticController>().AsSingle();
            bindings.BindFactory<IBaseButtonView,Action,IBaseButtonController,BaseButtonControllerFactory>().To<BaseButtonController>();
            var buttons=bindings.Resolve<BaseButtonControllerFactory>();
            using(var presenter=new FruitCollectionPresenter(inventory,profiles,view,buttons,garden,visits,notice,new Scripts.Economy.AchievementService(profiles,new Scripts.Economy.AchievementRules(new Scripts.Economy.EconomySettings()))))
            using(var header=new MenuHeaderController(clone.GetComponentInChildren<MenuHeaderView>(true),profiles))
            {
                var trackerRoot=new GameObject("GardenUiTracker");trackerRoot.SetActive(false);trackerRoot.transform.SetParent(clone.transform,false);
                var tracker=trackerRoot.AddComponent<LevelTracker>();tracker.SuppressPersistenceForTests=true;Field(tracker,"_levelId",17);
                new MenuUIController(tracker,new MemorySave(),buttons,menu);header.Initialize(tracker);
                applicationRoot=new GameObject("GardenApplicationEvents");applicationRoot.SetActive(false);
                SceneManager.MoveGameObjectToScene(applicationRoot,camera.gameObject.scene);
                var app=applicationRoot.AddComponent<GlobalContext>();Field(app,"_gardenVisits",visits);Field(app,"_achievements",new AchievementService(profiles,new AchievementRules(new EconomySettings())));GardenApp(app,"Start");
                presenter.ShowPendingNotice();Require(!notice.IsVisible,"Empty startup has no growth announcement");
                presenter.Open(); // Footer entry raycasts are covered by navigation-review.
                var panel=(GardenPanelView)view.Garden;var popup=(SeedPurchasePopupView)view.Purchase;
                Require(view.IsVisible && panel.gameObject.activeSelf,"Bahçem initially opens its garden tab");
                Require(panel.Plots.Count==9 && panel.Plots.All(plot=>plot.OccupiedCells==0),"Empty garden shows all nine permanent plots without fake crops");
                Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-garden-purpose.png");
                TapGarden(canvas,camera,eventSystem,panel.BuyButton);
                TapGarden(canvas,camera,eventSystem,popup.FruitButton(FruitId.Strawberry));popup.QuantityInput.text="10";
                Require(popup.ConfirmButton.interactable && popup.transform.Find("Panel/TotalCounter/Value").GetComponent<TMP_Text>().text=="5"
                    && !popup.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.StartsWith("Kalan") || t.text=="Bakiye"),"Purchase preview shows fruit, quantity and total without redundant balance text");
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
                Require(!popup.gameObject.activeSelf && profiles.Snapshot().Wallet.Coins==45 && garden.GetBatches().Single().Value.Quantity==10,
                    "Actual UI purchase debits once, creates a batch and ignores a duplicate confirmation");
                Require(clone.GetComponentInChildren<MenuHeaderView>(true).GetComponentsInChildren<TMP_Text>(true).Any(t=>t.text=="45"),"Menu coin counter refreshes after purchase");
                var planted=panel.Plots.Single(plot=>plot.Fruit==FruitId.Strawberry);
                Require(planted.CellCount(0).text=="10 tohum" && planted.CellArt(0).sprite.name=="Seed","New batch is displayed with its real amount and seed artwork");
                Require(planted.CellCountdown(0).text=="2 gün 0 saat" && planted.CellCountdown(0).fontStyle==FontStyles.Bold,"New seeds display the 48-hour countdown in bold");
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
                presenter.Dispose();clock.Day(2);GardenApp(app,"OnApplicationPause",true);GardenApp(app,"OnApplicationPause",false);
                Require(!notice.IsVisible && garden.GetBatches().Single().Value.Stage==GardenGrowthStage.Leaves,"Growth continues in another scene and elapsed time advances to the next scheduled stage");
                using(var returning=new FruitCollectionPresenter(inventory,profiles,view,buttons,garden,visits,notice,new Scripts.Economy.AchievementService(profiles,new Scripts.Economy.AchievementRules(new Scripts.Economy.EconomySettings()))))
                {
                    returning.ShowPendingNotice();Require(notice.IsVisible,"Queued game-scene progress is shown when the menu is ready");
                    notice.Hide();returning.ShowPendingNotice();Require(!notice.IsVisible,"Menu recreation never repeats an already shown notice");
                    clock.Day(3);GardenApp(app,"OnApplicationPause",true);GardenApp(app,"OnApplicationPause",false);
                    Require(garden.GetBatches().Single().Value.Stage==GardenGrowthStage.Flower,"Third entry reaches flower");
                    clock.Day(4);GardenApp(app,"OnApplicationFocus",false);GardenApp(app,"OnApplicationFocus",true);
                    Require(garden.GetBatches().Single().Value.Stage==GardenGrowthStage.Fruit && inventory.GetCollection()[FruitId.Strawberry].Current==0,"Fourth entry ripens but does not collect");
                    Require(notice.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("dokun")) && !notice.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("koleksiyonuna eklendi")),"Ready notice teaches tapping and never claims an uncollected reward");
                    Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-garden-ready-notice.png");
                    returning.Open();var harvestHit=planted.CellRect(0).Find("HarvestHit").GetComponent<GardenHarvestCell>();
                    Layer(clone.transform);Draw(camera);var hitRect=(RectTransform)harvestHit.transform;
                    var point=RectTransformUtility.WorldToScreenPoint(camera,hitRect.TransformPoint(hitRect.rect.center));
                    var pointer=new PointerEventData(eventSystem) {position=point,pressPosition=point,button=PointerEventData.InputButton.Left,
                        pointerPressRaycast=new RaycastResult {module=canvas.GetComponent<GraphicRaycaster>()}};
                    harvestHit.OnPointerDown(pointer);panel.WorldScroll.OnBeginDrag(pointer);panel.WorldScroll.OnEndDrag(pointer);harvestHit.OnPointerClick(pointer);
                    Require(inventory.GetCollection()[FruitId.Strawberry].Current==0 && garden.GetBatches().Count==1,"A swipe that returns to its start still cannot harvest");
                    Layer(clone.transform);Draw(camera);var hits=new List<RaycastResult>();canvas.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
                    Require(hits.Count>0 && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==harvestHit.gameObject,"Actual raycast reaches the ripe cell");
                    harvestHit.OnPointerDown(pointer);harvestHit.OnPointerUp(pointer);harvestHit.OnPointerClick(pointer);harvestHit.OnPointerClick(pointer);
                    Require(garden.GetBatches().Count==0 && inventory.GetCollection()[FruitId.Strawberry].Current==10,"Actual cell tap collects once and empties the cell");
                    Require(notice.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("10 çilek") && t.text.Contains("koleksiyonuna eklendi")),"Collection message appears only after committed harvest");
                    Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-garden-harvest-notice.png");
                    revision=profiles.Snapshot().Revision;notice.Hide();
                    GardenApp(app,"OnApplicationPause",true);GardenApp(app,"OnApplicationPause",false);
                    Require(!notice.IsVisible && profiles.Snapshot().Revision==revision,"Same-day resume cannot repeat manual harvest");
                    returning.Open();TapGarden(canvas,camera,eventSystem,view.GetTabButton(GardenTab.Collection));
                    var cells=(FruitCollectionCell[])Get(view,"cells");Require(cells.Single(c=>c.Fruit==FruitId.Strawberry).Amount.text=="10","Collection tab contains the actual harvest");
                    Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-garden-collection.png");
                    GardenLayouts(canvas,camera,eventSystem,clone,view,popup,panel,returning,profiles);
                }
            }
            Require(errors.Count==0,"Garden UI has no Unity errors: "+string.Join(" | ",errors));
            Require(before==Profile(),"Real player preferences stay unchanged");
            File.WriteAllText(Folder+"/garden-ui-review.json",JsonConvert.SerializeObject(new {passed=true,phone_layouts=4,
                manual_harvest=true,drag_cannot_harvest=true,actual_raycast_purchase=true,cancel_outside=true,duplicate_click=true,global_focus_pause=true,
                menu_reentry_no_growth=true,queued_notice=true,harvest_to_collection=true,real_player_unchanged=true,
                fixed_growth_rule=true,shared_fruit_cards=true,shared_coin_counters=true,readable_stage_progress=true},Formatting.Indented));
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
        foreach(var size in new[]{new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
        {
            ((RectTransform)canvas.transform).sizeDelta=size;clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();
            presenter.Open();view.ApplyLayout();panel.ApplyLayout();Canvas.ForceUpdateCanvases();
            Require(panel.Plots.Count==9 && panel.Plots.All(plot=>plot.OccupiedCells==0),"Empty garden retains all permanent plots");
            Require(panel.WorldScroll.horizontal && !panel.WorldScroll.vertical,"Garden scrolls horizontally");
            Require(Mathf.Abs(panel.WorldScroll.content.rect.width-panel.WorldScroll.viewport.rect.width*1.5f)<.1f,"The visible world has two columns out of three");
            Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-garden-empty-"+(int)size.y+".png");presenter.Close();
        }
        profiles.Update(profile=>{
            for(int index=0;index<12;index++)profile.Garden.Add("layout:"+index,new PlantingBatch {CellIndex=index/9,Fruit=(FruitId)(index%9+1),Quantity=1000,
                PlantedUtcDay=100,LastGrowthUtcDay=100+index%4,Stage=(GardenGrowthStage)(index%4)});
            for(int index=2;index<4;index++)profile.Garden.Add("full-plot:"+index,new PlantingBatch {CellIndex=index,Fruit=FruitId.Strawberry,Quantity=10,
                PlantedUtcDay=100,LastGrowthUtcDay=103,Stage=GardenGrowthStage.Flower});
        });
        foreach(var size in new[]{new Vector2(320,568),new Vector2(375,667),new Vector2(375,812),new Vector2(430,932)})
        {
            ((RectTransform)canvas.transform).sizeDelta=size;camera.orthographicSize=Mathf.Max(size.y*.5f,size.x/(2*camera.aspect));
            clone.GetComponent<FruitLabMenuLayout>().ApplyLayout();presenter.Open();view.ApplyLayout();panel.ApplyLayout();
            Require(panel.Plots.Sum(plot=>plot.OccupiedCells)==14,"Batch quantities use one representation per occupied cell");
            Require(panel.Plots.Any(plot=>Enumerable.Range(0,4).Any(i=>plot.CellArt(i).gameObject.activeSelf && plot.CellArt(i).sprite.name=="Sprout"))
                && panel.Plots.Any(plot=>Enumerable.Range(0,4).Any(i=>plot.CellArt(i).gameObject.activeSelf && plot.CellArt(i).sprite.name=="Flower")),"Growth stages have distinct artwork");
            foreach(var amount in panel.GetComponentsInChildren<TMP_Text>().Where(t=>t.name=="Quantity"))Require(amount.fontSizeMax>=14,"Compact garden cell quantities retain readable typography");
            foreach(var label in panel.GetComponentsInChildren<TMP_Text>()){label.ForceMeshUpdate();Require(!label.isTextOverflowing,"Garden text fits: "+label.text);}
            TapGarden(canvas,camera,events,panel.BuyButton);
            TapGarden(canvas,camera,events,popup.FruitButton(FruitId.Strawberry));
            Require(!popup.ConfirmButton.interactable && popup.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("Parsel dolu")),"Full plot is explained and disables purchase before coins are spent");
            var popupPanel=(RectTransform)popup.transform.Find("Panel");
            Require(popupPanel.rect.width<=size.x-24 && popupPanel.rect.height<=size.y-24,"Purchase popup fits phone bounds");
            foreach(FruitId fruit in Enum.GetValues(typeof(FruitId)))TapGarden(canvas,camera,events,popup.FruitButton(fruit));
            Require(popup.GetComponentsInChildren<NonDraggableCardItemView>().Length==9,"Seed choices use nine shared fruit cards");
            var rule=popupPanel.Find("GrowthRule").GetComponent<TMP_Text>();
            Require(rule.gameObject.activeSelf && rule.text.Contains("2 gün"),"The 48-hour growing rule is visible before confirming");
            Require(rule.fontSize>=16 && !rule.GetComponentInParent<ScrollRect>(),"Growth explanation remains readable outside the scrolling options");
            var options=popupPanel.Find("Options").GetComponent<ScrollRect>();
            Canvas.ForceUpdateCanvases();popup.ApplyLayout();Canvas.ForceUpdateCanvases();
            Require(options.content.rect.height<=options.viewport.rect.height+.1f,"Nine fruit choices and quantity fit without scrolling on supported phones");
            foreach(string counter in new[]{"TotalCounter"})
            {
                var amount=popupPanel.Find(counter+"/Value").GetComponent<TMP_Text>();var icon=popupPanel.Find(counter+"/Coin").GetComponent<Image>();
                Require(amount.fontSize>=22 && amount.color==PopupArtStyle.Ink && icon.sprite==FruitLabStyle.Icon("Coin"),"Price counters share the game coin artwork and readable values");
            }
            foreach(var label in popup.GetComponentsInChildren<TMP_Text>(false))
            {label.ForceMeshUpdate();Require(!label.isTextOverflowing,"Purchase text fits: "+label.name+" "+label.text);}
            Layer(clone.transform);Draw(camera);Capture(camera,Folder+"/preview-garden-purchase-"+(int)size.y+".png");
            TapGarden(canvas,camera,events,popup.CancelButton);
            TapGarden(canvas,camera,events,panel.CollectionButton);
            Require(view.CollectionPopup.IsVisible,"Garden album opens the current collection popup");
            foreach(var cell in (FruitCollectionCell[])Get(view,"cells"))
            {
                Require(((RectTransform)cell.Card.transform).rect.width>=50,"Collection fruit remains readable on small screens");
                Require(cell.Amount.color==PopupArtStyle.Ink && cell.Amount.fontSize>=22,"Collection quantities use the shared cream counter style");
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
        public void Day(int day)=>UtcNow=new DateTime(2026,11,1,12,0,0,DateTimeKind.Utc).AddHours(day*12);
    }
}
