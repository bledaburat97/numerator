using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Scripts;
using UnityEditor;
using UnityEngine;

public static class FruitLabFriendInviteEditorTools
{
    public static string Review()
    {
        AssetDatabase.ImportAsset("Assets/Resources/Art/Multiplayer",ImportAssetOptions.ImportRecursive|ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
        foreach(string path in Directory.GetFiles("Assets/Resources/Art/Multiplayer","*.png",SearchOption.AllDirectories))
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            Require(AssetDatabase.LoadAssetAtPath<Sprite>(path)!=null,"Generated artwork imports as a sprite: "+path);
        }
        FriendInviteBuildTools.EnsureSettings();
        var config = ScriptableObject.CreateInstance<FriendInviteSettings>(); config.httpsOrigin = "https://friends.example.com";
        try
        {
            var tokens = Enumerable.Range(0, 1000).Select(_ => FriendInvitation.NewToken("123456")).ToArray();
            Require(tokens.Distinct().Count() == tokens.Length, "Random invite tokens are distinct");
            var invite = new FriendInvitation(tokens[0]);
            Require(FriendInvitation.TryParse(invite.Url(config), config, out var web) && web.Token == invite.Token, "HTTPS link round trip");
            Require(FriendInvitation.TryParse("fruitfriends://invite/" + invite.Token, config, out var app) && app.Token == invite.Token, "Custom scheme link round trip");
            foreach (string value in new[] { "https://evil.example/i/" + invite.Token, "https://friends.example.com.evil/i/" + invite.Token,
                "http://friends.example.com/i/" + invite.Token, "https://friends.example.com/i/" + invite.Token + "?x=1",
                "https://friends.example.com/i/%2F" + invite.Token, "fruitfriends://other/" + invite.Token, "fruitfriends://invite/../" + invite.Token, "fruitfriends://invite/123456", "file:///i/" + invite.Token })
                Require(!FriendInvitation.TryParse(value, config, out _), "Invalid or foreign links rejected");
            Require(PlayerIdentityStore.NormalizeName("  Elif\n<test>  ") == "Eliftest", "Names are bounded and sanitized");
            string realIdentity = PlayerPrefs.GetString("player_identity_v1", ""), realName = PlayerPrefs.GetString("player_name_multiplayer", "");
            var store = new Dictionary<string, string> { { "player_name_multiplayer", "Elif" } };
            string id;
            using (PlayerIdentityStore.UseForTests(store))
            {
                Require(PlayerIdentityStore.Current.name == "Elif", "Legacy name migrates without asking for a form");
                id = PlayerIdentityStore.Current.id; PlayerIdentityStore.Set("Deniz", 3);
            }
            using (PlayerIdentityStore.UseForTests(store))
            {
                Require(PlayerIdentityStore.Current.id == id && PlayerIdentityStore.Current.name == "Deniz" && PlayerIdentityStore.Current.avatar == 3,
                    "Cold reload remembers the same identity, selected name and avatar");
                PlayerIdentityStore.Set("", 99);
                Require(PlayerIdentityStore.Current.name == PlayerIdentityStore.GuestName(id) && PlayerIdentityStore.Current.avatar == PlayerIdentityStore.AvatarCount-1,
                    "Empty input yields a stable guest name and bounds avatar IDs");
            }
            store["player_identity_v1"]=JsonUtility.ToJson(new PlayerIdentity {id=id,name="Deniz",avatar=5});
            using(PlayerIdentityStore.UseForTests(store))
                Require(PlayerIdentityStore.Current.id==id && PlayerIdentityStore.Current.name=="Deniz" && PlayerIdentityStore.Current.avatar==3,"Legacy fruit avatar IDs migrate into the four gardener choices without losing the profile");
            Require(realIdentity == PlayerPrefs.GetString("player_identity_v1", "") && realName == PlayerPrefs.GetString("player_name_multiplayer", ""),
                "Identity checks never alter the real player's preferences");
            Require(FriendInvitation.Lookup(tokens[0]) != FriendInvitation.Lookup(tokens[1]), "Invitation lookup is scoped to random token");
            string iosFolder = "Library/FruitLabEditor/InviteExportCheck";
            Directory.CreateDirectory(iosFolder);
            File.WriteAllText(iosFolder + "/Info.plist", "<?xml version=\"1.0\" encoding=\"UTF-8\"?><plist version=\"1.0\"><dict><key>CFBundleURLTypes</key><array><dict><key>CFBundleURLSchemes</key><array><string>keepme</string></array></dict></array></dict></plist>");
            FriendInviteBuildTools.ConfigureIos(BuildTarget.iOS, iosFolder); FriendInviteBuildTools.ConfigureIos(BuildTarget.iOS, iosFolder);
            var ios = new System.Xml.XmlDocument(); ios.Load(iosFolder + "/Info.plist");
            Require(ios.SelectNodes("//string[text()='fruitfriends']").Count == 1 && ios.SelectNodes("//string[text()='keepme']").Count == 1,
                "iOS URL registration runs regardless of the Editor's active platform and preserves other schemes");
            string file = "Library/FruitLabEditor/invite-android-manifest.xml";
            File.WriteAllText(file, "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"><application><activity android:name=\"com.unity3d.player.UnityPlayerGameActivity\" /></application></manifest>");
            FriendInviteBuildTools.ConfigureAndroidManifest(file, config); FriendInviteBuildTools.ConfigureAndroidManifest(file, config);
            string xml = File.ReadAllText(file);
            Require(xml.Split("android.intent.action.VIEW").Length == 3 && xml.Contains("autoVerify=\"true\""), "Android build registers both link types without duplicates");
        }
        finally { UnityEngine.Object.DestroyImmediate(config); }
        return "PASS: 1,000 invitation tokens, HTTPS/custom link validation, bounded identity and repeatable Android manifest generation.\n" + FruitLabMultiplayerEditorTools.Bake(true) + "\n" + FruitLabMultiplayerEditorTools.RenderOnlineScenes();
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Invitation check: " + message); }
}
