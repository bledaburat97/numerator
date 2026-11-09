using System;
using System.IO;
using System.Linq;
using System.Xml;
using Scripts;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Android;
using UnityEngine;
using UnityEditor.iOS.Xcode;

public sealed class FriendInviteBuildTools : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 80;
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var settings = FriendInviteSettings.Load();
        string manifest = Path.Combine(path, "src/main/AndroidManifest.xml");
        ConfigureAndroidManifest(manifest, settings);
    }
    public static void ConfigureAndroidManifest(string path, FriendInviteSettings settings)
    {
        const string ns = "http://schemas.android.com/apk/res/android";
        var xml = new XmlDocument(); xml.Load(path);
        var activity = xml.SelectNodes("/manifest/application/activity").Cast<XmlElement>()
            .FirstOrDefault(item => item.GetAttribute("name", ns).Contains("UnityPlayer"));
        if (activity == null) throw new Exception("Unity activity not found for invitation links");
        string scheme = settings != null ? settings.urlScheme : "fruitfriends";
        AddFilter(scheme, "invite", "/", false);
        if (settings != null && FriendInvitation.IsOrigin(settings.httpsOrigin)) AddFilter("https", new Uri(settings.httpsOrigin).Host, "/i/", true);
        xml.Save(path);
        void AddFilter(string protocol, string host, string prefix, bool verify)
        {
            foreach (XmlElement old in activity.SelectNodes("intent-filter").Cast<XmlElement>().ToArray())
                if (old.SelectNodes("data").Cast<XmlElement>().Any(item => item.GetAttribute("scheme", ns) == protocol && item.GetAttribute("host", ns) == host)) activity.RemoveChild(old);
            var filter = xml.CreateElement("intent-filter"); activity.AppendChild(filter);
            if (verify) filter.SetAttribute("autoVerify", ns, "true");
            Add("action", "name", "android.intent.action.VIEW");
            Add("category", "name", "android.intent.category.DEFAULT"); Add("category", "name", "android.intent.category.BROWSABLE");
            var data = Add("data", "scheme", protocol); data.SetAttribute("host", ns, host); data.SetAttribute("pathPrefix", ns, prefix);
            XmlElement Add(string element, string key, string value) { var node = xml.CreateElement(element); node.SetAttribute(key, ns, value); filter.AppendChild(node); return node; }
        }
    }
    [PostProcessBuild(80)]
    public static void ConfigureIos(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;
        var settings = FriendInviteSettings.Load();
        var plist = new PlistDocument(); string file = Path.Combine(path, "Info.plist"); plist.ReadFromFile(file);
        var types = plist.root.values.TryGetValue("CFBundleURLTypes", out var existing) ? existing.AsArray() : plist.root.CreateArray("CFBundleURLTypes");
        string scheme = settings != null ? settings.urlScheme : "fruitfriends";
        if (!types.values.Any(item => item.AsDict().values.TryGetValue("CFBundleURLSchemes", out var urls) && urls.AsArray().values.Any(url => url.AsString() == scheme)))
        { var entry = types.AddDict(); entry.SetString("CFBundleURLName", "Friend invitation"); entry.CreateArray("CFBundleURLSchemes").AddString(scheme); }
        plist.WriteToFile(file);
        if (settings != null && FriendInvitation.IsOrigin(settings.httpsOrigin))
        {
            string project = PBXProject.GetPBXProjectPath(path);
            var capability = new ProjectCapabilityManager(project, "FriendInvites.entitlements", "Unity-iPhone");
            capability.AddAssociatedDomains(new[] { "applinks:" + new Uri(settings.httpsOrigin).Host }); capability.WriteToFile();
        }
    }
    public static void EnsureSettings()
    {
        const string path = "Assets/Resources/Config/FriendInvites.asset";
        if (AssetDatabase.LoadAssetAtPath<FriendInviteSettings>(path) != null) return;
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<FriendInviteSettings>(), path); AssetDatabase.SaveAssets();
    }
}
