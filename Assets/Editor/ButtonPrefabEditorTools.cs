using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Game;
using Newtonsoft.Json;
using Scripts;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class ButtonPrefabEditorTools
{
    public const string SharedPower="Assets/Prefabs/Buttons/PowerUpButton.prefab";
    private const string BasePath="Assets/Prefabs/Buttons/BaseButton.prefab";
    private const string Folder="Library/FruitLabEditor";
    private static readonly string[] StandardVariants={"CheckButton","ResetButton","SettingsButton","CloseButton","FadeButton","ProbabilityButton","CardHolderIndicatorButton"};
    private static readonly string[] PowerVariants={"NewRevealingPowerUpButton","NewLifePowerUpButton","NewBombPowerUpButton"};
    private static string PathFor(string name)=>"Assets/Prefabs/Buttons/"+name+".prefab";
    private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
    private static void Set(Object target,string property,Object value)
    {var data=new SerializedObject(target);data.FindProperty(property).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(target);}
    private static T At<T>(Transform root,string name) where T:Component=>root.GetComponentsInChildren<T>(true).FirstOrDefault(item=>item.name==name);
    private static Dictionary<string,long> ObjectIds(GameObject root)
    {
        var result=new Dictionary<string,long>();
        foreach(var node in root.GetComponentsInChildren<Transform>(true))
        {
            string key=AnimationUtility.CalculateTransformPath(node,root.transform);
            if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(node.gameObject,out _,out long id))result[key+"|GameObject"]=id;
            var counts=new Dictionary<Type,int>();
            foreach(var component in node.GetComponents<Component>())
            {
                if(component==null)continue;var type=component.GetType();counts.TryGetValue(type,out int count);counts[type]=count+1;
                if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component,out _,out id))result[key+"|"+type.FullName+"|"+count]=id;
            }
        }
        return result;
    }
    private static void BindBase(BaseButtonView button)
    {
        var icon=button.Icon??At<Image>(button.transform,"CheckImage")??At<Image>(button.transform,"Image");
        button.ConfigureReferences((RectTransform)button.transform,button.Label,button.OuterSurface,button.Shadow,button.PressedSurface,icon);
        if(button is PowerUpButtonView)return;
        // Explicit policies have already been migrated. Name inference exists only for old, unconfigured assets.
        bool configured=button.Presentation is ButtonAppearance;
        var appearance=ButtonAppearance.For(button);
        string name=button.name;
        var role=configured?appearance.Kind:name.StartsWith("CheckButton")?ButtonVisualKind.Confirm:name.StartsWith("ResetButton")?ButtonVisualKind.Clear:
            name.StartsWith("SettingsButton")?ButtonVisualKind.Settings:ButtonVisualKind.Standard;
        appearance.Configure(button,role,role==ButtonVisualKind.Confirm || appearance.IsPrimary,At<Image>(button.transform,"Frame"));
        appearance.ConfigureGlyphs(appearance.ActionIcon??At<FruitLabGlyph>(button.transform,"ActionIcon"),appearance.LevelBadge??At<FruitLabGlyph>(button.transform,"LevelKind"));
        foreach(var glyph in button.GetComponentsInChildren<FruitLabGlyph>(true))
            if(glyph!=appearance.ActionIcon && glyph!=appearance.LevelBadge)glyph.gameObject.SetActive(false);
        if(button.Label==null)foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))label.gameObject.SetActive(false);
        appearance.Apply();EditorUtility.SetDirty(appearance);EditorUtility.SetDirty(button);
    }
    private static void BindPower(PowerUpButtonView button)
    {
        var picture=At<Image>(button.transform,"Picture");var bg=At<Image>(button.transform,"Bg");
        var holder=At<RectTransform>(button.transform,"CountHolder");
        Require(picture!=null && bg!=null && holder!=null,"Power button parts are missing: "+button.name);
        var count=holder.GetComponentInChildren<TMP_Text>(true);Require(count!=null,"Power button count is missing: "+button.name);
        var glyph=picture.GetComponentInChildren<FruitLabGlyph>(true);
        if(glyph==null){glyph=FruitLabGlyph.Create(picture.transform,FruitLabGlyph.Symbol.Helper,Color.white);glyph.name="PowerGlyph";}
        foreach(var old in button.GetComponentsInChildren<FruitLabGlyph>(true))if(old!=glyph)old.gameObject.SetActive(false);
        foreach(var label in button.GetComponentsInChildren<TMP_Text>(true))if(label!=count)label.gameObject.SetActive(false);
        button.ConfigureParts(picture,bg,holder,count,glyph);
        foreach(var appearance in button.GetComponents<ButtonAppearance>())Object.DestroyImmediate(appearance);
        EditorUtility.SetDirty(button);
    }
    private static void CreateSharedPower()
    {
        if(File.Exists(SharedPower))return;
        var root=PrefabUtility.LoadPrefabContents(PathFor(PowerVariants[0]));
        try
        {
            root.name="PowerUpButton";BindPower(root.GetComponent<PowerUpButtonView>());
            var button=root.GetComponent<PowerUpButtonView>();button.ConfigureType(RewardType.Revealing);button.CountText.text="0";
            foreach(var node in root.GetComponentsInChildren<Transform>(true))node.gameObject.layer=5;
            Require(PrefabUtility.SaveAsPrefabAsset(root,SharedPower)!=null,"Save shared power button");
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    private static void Convert(string path,string basis)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(PrefabUtility.GetPrefabAssetType(asset)==PrefabAssetType.Variant)return;
        var root=Object.Instantiate(asset);root.name=asset.name;
        try
        {
            if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            PrefabUtility.ConvertToPrefabInstance(root,AssetDatabase.LoadAssetAtPath<GameObject>(basis),new ConvertToPrefabInstanceSettings {
                objectMatchMode=ObjectMatchMode.ByHierarchy,changeRootNameToAssetName=false,
                componentsNotMatchedBecomesOverride=true,gameObjectsNotMatchedBecomesOverride=true,recordPropertyOverridesOfMatches=true
            },InteractionMode.AutomatedAction);
            if(root.GetComponent<PowerUpButtonView>() is PowerUpButtonView power)BindPower(power);else BindBase(root.GetComponent<BaseButtonView>());
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                if(component==null)continue;PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                var source=PrefabUtility.GetCorrespondingObjectFromSource(component);if(source==null)continue;
                var data=new SerializedObject(component);var original=new SerializedObject(source);var property=data.GetIterator();
                var redundant=new List<string>();
                while(property.Next(true))
                {
                    if(!property.prefabOverride || property.isArray || property.propertyType==SerializedPropertyType.Generic)continue;
                    var inherited=original.FindProperty(property.propertyPath);
                    if(inherited!=null && SerializedProperty.DataEquals(property,inherited))redundant.Add(property.propertyPath);
                }
                foreach(string propertyPath in redundant)
                {data.Update();var propertyToRevert=data.FindProperty(propertyPath);if(propertyToRevert!=null)PrefabUtility.RevertPropertyOverride(propertyToRevert,InteractionMode.AutomatedAction);}
            }
            Require(PrefabUtility.SaveAsPrefabAsset(root,path)!=null,"Save variant: "+path);
        }
        finally{Object.DestroyImmediate(root);}
    }
    private static void RemapReferences(Dictionary<string,Dictionary<string,long>> originals)
    {
        if(File.Exists(Folder+"/button-reference-remap.complete"))return;
        var maps=new Dictionary<string,Dictionary<long,long>>();
        foreach(var entry in originals)
        {
            var current=ObjectIds(AssetDatabase.LoadAssetAtPath<GameObject>(entry.Key));var ids=new Dictionary<long,long>();
            foreach(var item in entry.Value)
            {
                Require(current.TryGetValue(item.Key,out long replacement),"Conversion removed an authored part: "+entry.Key+" / "+item.Key);
                if(item.Value!=replacement)ids[item.Value]=replacement;
            }
            if(ids.Count>0)maps.Add(AssetDatabase.AssetPathToGUID(entry.Key),ids);
        }
        foreach(string path in Directory.GetFiles("Assets","*",SearchOption.AllDirectories).Where(path=>path.EndsWith(".prefab") || path.EndsWith(".unity") || path.EndsWith(".asset")))
        {
            string content=File.ReadAllText(path);string changed=content;
            foreach(var map in maps)
            {
                if(!changed.Contains(map.Key))continue;
                changed=Regex.Replace(changed,@"(\{fileID:\s*)(-?\d+)(,\s*guid:\s*"+map.Key+@"\b)",match=>
                    map.Value.TryGetValue(long.Parse(match.Groups[2].Value),out long id)?match.Groups[1].Value+id+match.Groups[3].Value:match.Value);
            }
            if(changed!=content)File.WriteAllText(path,changed);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        File.WriteAllText(Folder+"/button-reference-remap.complete","Original prefab object references remapped.");
    }
    private static void UpgradeButtonCollection(Component owner,PowerUpButtonCollection collection)
    {
        if(collection.Views.Count>0)return;
        collection.SetViews(owner.GetComponentsInChildren<PowerUpButtonView>(true));EditorUtility.SetDirty(owner);
    }
    private static void UpgradeTree(GameObject root,PowerUpButtonView template)
    {
        // Opening a scene can unload an asset held only on the C# stack.
        template=AssetDatabase.LoadAssetAtPath<PowerUpButtonView>(SharedPower);
        foreach(var button in root.GetComponentsInChildren<BaseButtonView>(true))
            if(button is PowerUpButtonView power)BindPower(power);else BindBase(button);
        foreach(var list in root.GetComponentsInChildren<PowerUpListHolderView>(true))
        {
            UpgradeButtonCollection(list,list.Buttons);Set(list,"buttonPrefab",template);
            foreach(var type in PowerUpTypeUtility.OrderedRewardTypes)
            {
                var button=(PowerUpButtonView)list.GetPowerUpButton(type);button.ConfigureType(type);
                EditorUtility.SetDirty(button);PrefabUtility.RecordPrefabInstancePropertyModifications(button);
            }
            EditorUtility.SetDirty(list);
        }
        foreach(var bar in root.GetComponentsInChildren<VerticalCrystalProgressView>(true))
        {UpgradeButtonCollection(bar,bar.Buttons);Set(bar,"buttonPrefab",template);bar.ApplyLabStyle();EditorUtility.SetDirty(bar);}
        foreach(var component in root.GetComponentsInChildren<Component>(true))
            if(component!=null && PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }
    public static string Migrate()
    {
        Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Exit Play mode before migrating buttons.");
        Directory.CreateDirectory(Folder);
        var setup=EditorSceneManager.GetSceneManagerSetup();
        Require(EditorSceneManager.SaveOpenScenes(),"Save open scenes before preserving prefab references.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        try
        {
            string snapshot=Folder+"/button-original-ids.json";
            var paths=StandardVariants.Concat(PowerVariants).Select(PathFor).ToArray();
            var originals=File.Exists(snapshot)?JsonConvert.DeserializeObject<Dictionary<string,Dictionary<string,long>>>(File.ReadAllText(snapshot)):
                paths.ToDictionary(path=>path,path=>ObjectIds(AssetDatabase.LoadAssetAtPath<GameObject>(path)));
            if(!File.Exists(snapshot))File.WriteAllText(snapshot,JsonConvert.SerializeObject(originals,Formatting.Indented));
            CreateSharedPower();
            var baseRoot=PrefabUtility.LoadPrefabContents(BasePath);
            try{BindBase(baseRoot.GetComponent<BaseButtonView>());PrefabUtility.SaveAsPrefabAsset(baseRoot,BasePath);}
            finally{PrefabUtility.UnloadPrefabContents(baseRoot);}
            foreach(string name in StandardVariants)Convert(PathFor(name),BasePath);
            foreach(string name in PowerVariants)Convert(PathFor(name),SharedPower);
            // Retain the authored settings icon parent, including a retry of an interrupted migration.
            var settingsRoot=PrefabUtility.LoadPrefabContents(PathFor("SettingsButton"));
            try
            {
                var view=settingsRoot.GetComponent<BaseButtonView>();var face=settingsRoot.transform.Find("InnerBG");
                if(view.Icon!=null && face!=null && view.Icon.transform.parent==settingsRoot.transform)view.Icon.transform.SetParent(face,false);
                BindBase(view);PrefabUtility.SaveAsPrefabAsset(settingsRoot,PathFor("SettingsButton"));
            }
            finally{PrefabUtility.UnloadPrefabContents(settingsRoot);}
            var powerTypes=new[]{RewardType.Revealing,RewardType.Life,RewardType.Bomb};
            for(int index=0;index<PowerVariants.Length;index++)
            {
                var root=PrefabUtility.LoadPrefabContents(PathFor(PowerVariants[index]));
                try
                {
                    var power=root.GetComponent<PowerUpButtonView>();BindPower(power);power.ConfigureType(powerTypes[index]);
                    foreach(var component in root.GetComponentsInChildren<Component>(true))if(component!=null)PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    PrefabUtility.SaveAsPrefabAsset(root,PathFor(PowerVariants[index]));
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            RemapReferences(originals);
            var template=AssetDatabase.LoadAssetAtPath<PowerUpButtonView>(SharedPower);
            // Update owning prefab assets first; their instances inherit the same explicit parts.
            foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs","Assets/Resources"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(asset==null || (!asset.GetComponentsInChildren<BaseButtonView>(true).Any() && !asset.GetComponentsInChildren<VerticalCrystalProgressView>(true).Any()))continue;
                var root=PrefabUtility.LoadPrefabContents(path);
                try{UpgradeTree(root,template);Require(PrefabUtility.SaveAsPrefabAsset(root,path)!=null,"Save button owner "+path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            foreach(string path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))
            {
                var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
                foreach(var root in scene.GetRootGameObjects())UpgradeTree(root,template);
                EditorSceneManager.MarkSceneDirty(scene);Require(EditorSceneManager.SaveScene(scene),"Save button scene "+path);
            }
            AssetDatabase.SaveAssets();ValidateAssets();
            return "Shared power button, seven standard variants and three compatibility power variants saved; object references remapped and validated.";
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
    }
    private static void ValidateCollection(PowerUpButtonCollection collection,string path,List<string> errors)
    {
        var views=collection.Views;
        if(views.Count!=PowerUpTypeUtility.OrderedRewardTypes.Count || views.Any(view=>view==null)
            || views.Where(view=>view!=null).Select(view=>view.Type).Distinct().Count()!=PowerUpTypeUtility.OrderedRewardTypes.Count
            || PowerUpTypeUtility.OrderedRewardTypes.Any(type=>!views.Any(view=>view!=null && view.Type==type)))
            errors.Add(path+": missing, duplicate or wrong power type mapping");
    }
    public static void ValidateTree(GameObject root,string path,List<string> errors)
    {
        foreach(var button in root.GetComponentsInChildren<BaseButtonView>(true))
        {
            if(new SerializedObject(button).FindProperty("rectTransform").objectReferenceValue!=button.transform)errors.Add(path+": wrong button rect "+button.name);
            if(button is PowerUpButtonView power)
            {
                if(power.Icon==null || power.PressedSurface==null || power.CountHolder==null || power.CountText==null || power.PowerGlyph==null)
                    errors.Add(path+": missing power button binding "+power.name);
                else if(!power.Icon.transform.IsChildOf(power.transform) || !power.PressedSurface.transform.IsChildOf(power.transform)
                    || !power.CountHolder.IsChildOf(power.transform) || !power.CountText.transform.IsChildOf(power.CountHolder) || !power.PowerGlyph.transform.IsChildOf(power.Icon.transform))
                    errors.Add(path+": power button part outside its owner "+power.name);
                if(power.Presentation!=null)errors.Add(path+": power button should own its presentation "+power.name);
            }
            else if(!(button.Presentation is ButtonAppearance appearance) || appearance.Button!=button)
                errors.Add(path+": missing/foreign presentation "+button.name);
            else
            {
                if(button.PressedSurface==null || !button.PressedSurface.transform.IsChildOf(button.transform))errors.Add(path+": missing/foreign button face "+button.name);
                if(appearance.Kind==ButtonVisualKind.Label && button.Label==null)errors.Add(path+": missing button label "+button.name);
                if(appearance.Kind==ButtonVisualKind.Settings && button.Icon==null)errors.Add(path+": missing settings icon "+button.name);
                if((appearance.Kind==ButtonVisualKind.Confirm || appearance.Kind==ButtonVisualKind.Clear) && appearance.ActionIcon==null)errors.Add(path+": missing action icon "+button.name);
            }
        }
        foreach(var list in root.GetComponentsInChildren<PowerUpListHolderView>(true))
        {
            if(list.ButtonPrefab==null || AssetDatabase.GetAssetPath(list.ButtonPrefab)!=SharedPower)errors.Add(path+": list uses the wrong power template");
            ValidateCollection(list.Buttons,path+"/"+list.name,errors);
        }
        foreach(var bar in root.GetComponentsInChildren<VerticalCrystalProgressView>(true))
        {
            if(bar.ButtonPrefab==null || AssetDatabase.GetAssetPath(bar.ButtonPrefab)!=SharedPower)errors.Add(path+": reward preview uses the wrong power template");
            ValidateCollection(bar.Buttons,path+"/"+bar.name,errors);
        }
        foreach(var component in root.GetComponentsInChildren<Component>(true))
        {
            if(component==null){errors.Add(path+": missing script");continue;}
            if(!(component is BaseButtonView) && !(component is ButtonAppearance) && !(component is PowerUpListHolderView) && !(component is VerticalCrystalProgressView))continue;
            var iterator=new SerializedObject(component).GetIterator();
            while(iterator.Next(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference && iterator.objectReferenceValue==null && iterator.objectReferenceInstanceIDValue!=0)
                errors.Add(path+": dangling reference "+component.name+"."+iterator.propertyPath);
        }
    }
    [MenuItem("Tools/UI/Validate Button Prefabs")]
    private static void ValidateFromMenu()=>Debug.Log(ValidateAssets());

    public static string ValidateAssets(string reportDirectory=Folder)
    {
        var errors=new List<string>();
        Require(File.Exists(SharedPower),"Shared power button has not been migrated.");
        foreach(string name in StandardVariants.Concat(PowerVariants))
            if(PrefabUtility.GetPrefabAssetType(AssetDatabase.LoadAssetAtPath<GameObject>(PathFor(name)))!=PrefabAssetType.Variant)errors.Add("Expected prefab variant: "+name);
        foreach(string path in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs","Assets/Resources"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(root!=null)ValidateTree(root,path,errors);
        }
        foreach(string path in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))
        {
            var scene=EditorSceneManager.OpenPreviewScene(path);
            try{foreach(var root in scene.GetRootGameObjects())ValidateTree(root,path,errors);}
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
        Require(errors.Count==0,"Button asset validation failed:\n"+string.Join("\n",errors.Distinct()));
        Directory.CreateDirectory(reportDirectory);
        File.WriteAllText(Path.Combine(reportDirectory,"button-prefab-validation.json"),JsonConvert.SerializeObject(new {passed=true,standard_variants=StandardVariants.Length,power_compatibility_variants=PowerVariants.Length,shared_power_template=SharedPower},Formatting.Indented));
        return "Button prefab and scene bindings passed.";
    }
}
public sealed class ButtonPrefabBuildValidation : IPreprocessBuildWithReport
{
    public int callbackOrder=>-100;
    public void OnPreprocessBuild(BuildReport report)=>ButtonPrefabEditorTools.ValidateAssets();
}

public static partial class FruitLabTutorialEditorTools
{
    public static void ReviewButtonPrefabs(Canvas canvas,Camera camera)
    {
        string prefs=Profile();var errors=new List<string>();
        Application.LogCallback collect=(message,stack,type)=>{if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message);};
        Application.logMessageReceived+=collect;
        try
        {
            string clean=Path.Combine(Path.GetTempPath(),"numerator-buttons-"+Guid.NewGuid().ToString("N"),"reports");
            try
            {
                Require(!Directory.Exists(clean),"The clean-build report directory does not exist before validation");
                ButtonPrefabEditorTools.ValidateAssets(clean);
                Require(File.Exists(Path.Combine(clean,"button-prefab-validation.json")),"Validation creates its own report directory on a clean install");
            }
            finally{if(Directory.Exists(Path.GetDirectoryName(clean)))Directory.Delete(Path.GetDirectoryName(clean),true);}
            ClearPreviewActors(canvas);
            using(var game=new Fixture(canvas,camera,new LevelData {NumOfBoardHolders=5,NumOfCards=9,MaxNumOfTries=9}))
            {
                Field(game.Tracker,"_levelId",20);game.BuildActors();game.Gate.Clear();
                var check=(BaseButtonView)game.View.GetCheckButton();var reset=(BaseButtonView)game.View.GetResetButton();
                var settings=(BaseButtonView)game.View.GetSettingsButton();
                int clicks=0;game.UI.CheckFinalNumbers+=(sender,args)=>clicks++;
                check.name="A renamed control";reset.name="Another renamed control";settings.name="Configuration control";
                check.Init();reset.Init();settings.Init();
                Require(ButtonAppearance.For(check).Kind==ButtonVisualKind.Confirm && ButtonAppearance.For(reset).Kind==ButtonVisualKind.Clear
                    && ButtonAppearance.For(settings).Kind==ButtonVisualKind.Settings,"Roles survive arbitrary object names and repeated initialization");
                Click(check);Click(check);Require(clicks==2,"The shared controller still invokes exactly one callback per press");
                Require(check.GetButtonRectTransform().localScale==Vector3.one,"Button release resets the shared press feedback");
                var list=canvas.GetComponentInChildren<PowerUpListHolderView>(true);
                var assigned=PowerUpTypeUtility.OrderedRewardTypes.Select(type=>(PowerUpButtonView)list.GetPowerUpButton(type)).ToArray();
                foreach(var type in PowerUpTypeUtility.OrderedRewardTypes)
                {
                    var button=(PowerUpButtonView)list.GetPowerUpButton(type);list.SetPowerUpCount(type,(int)type+7);
                    Require(button.Type==type && button.CountText.text==((int)type+7).ToString(),"Typed power data binds the right icon and count: "+type);
                    var iconPosition=button.Icon.rectTransform.anchoredPosition;
                    button.SetButtonDown();button.SetButtonUp();
                    Require(button.Icon.rectTransform.anchoredPosition==iconPosition,"Power press feedback moves the face, not the fruit/tool icon");
                }
                for(int repeat=0;repeat<3;repeat++)list.ApplyLabLayout(331,52);
                Require(list.GetComponentsInChildren<PowerUpButtonView>(true).Count(button=>assigned.Contains(button))==7,"Repeated layout never duplicates assigned buttons");
                Layer(canvas.transform);Draw(camera);Capture(camera,Folder+"/preview-buttons-game.png");
            }
            var root=new GameObject("SharedPowerFactoryCheck",typeof(RectTransform),typeof(PowerUpListHolderView));root.transform.SetParent(canvas.transform,false);
            try
            {
                var list=root.GetComponent<PowerUpListHolderView>();var data=new SerializedObject(list);
                data.FindProperty("rectTransform").objectReferenceValue=root.transform;
                data.FindProperty("buttonPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<PowerUpButtonView>(ButtonPrefabEditorTools.SharedPower);data.ApplyModifiedPropertiesWithoutUndo();
                list.ApplyLabLayout(331,52);
                var buttons=list.GetComponentsInChildren<PowerUpButtonView>(true);Require(buttons.Length==7,"All seven missing powers instantiate the common prefab");
                foreach(var button in buttons)
                {
                    button.name="Renamed power";button.Icon.name="Renamed picture";button.CountHolder.name="Renamed badge";
                    button.ApplyLayout(button.Type,45,52);
                }
                list.ApplyLabLayout(331,120,2);Require(list.GetComponentsInChildren<PowerUpButtonView>(true).Length==7,"Two-row layout works without name lookups");
                var labels=buttons.Select(button=>button.CountText).ToArray();
                var broken=new SerializedObject(buttons[0]);broken.FindProperty("countText").objectReferenceValue=null;broken.ApplyModifiedPropertiesWithoutUndo();
                var problems=new List<string>();int before=root.GetComponentsInChildren<Transform>(true).Length;
                ButtonPrefabEditorTools.ValidateTree(root,"Intentional missing reference",problems);
                Require(problems.Any(problem=>problem.Contains("missing power button binding")) && root.GetComponentsInChildren<Transform>(true).Length==before,
                    "Read-only validation catches a missing part without trying to recreate it");
                broken.Update();broken.FindProperty("countText").objectReferenceValue=labels[0];broken.ApplyModifiedPropertiesWithoutUndo();
            }
            finally{Object.DestroyImmediate(root);}
            Require(prefs==Profile(),"Button checks preserve real player preferences");Require(errors.Count==0,"No button runtime errors: "+string.Join(" | ",errors));
            File.WriteAllText(Folder+"/button-prefab-review.json",JsonConvert.SerializeObject(new {passed=true,
                checks=new[]{"report directory created on clean install","typed appearance survives renamed objects","one callback per press","press/release surfaces","seven common power instances","typed icons/counts","two-row home layout","stable instance count","explicit references survive renamed children","read-only missing-binding validation","scene/prefab GUID references","real preferences preserved"}},Formatting.Indented));
        }
        finally{Application.logMessageReceived-=collect;}
    }
}
