using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Scripts;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class FruitLabThemeEditorTools
{
    private const string AtlasPath = "Assets/Resources/Art/FruitLabIconsV2.png";
    private static readonly string[] Names = { "Apple", "Banana", "Strawberry", "Orange", "Pear", "Grape",
        "Blueberry", "Watermelon", "Kiwi", "Bomb", "Reveal", "Life", "Coin", "Crystal", "Settings" };

    public static void ImportAssets()
    {
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceUpdate);
        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!source.LoadImage(File.ReadAllBytes(AtlasPath))) throw new Exception("Could not read the theme atlas");
            Color32[] pixels = source.GetPixels32();
            if (!pixels.Any(pixel => pixel.a == 0) || !pixels.Any(pixel => pixel.a == 255))
                throw new Exception("Theme atlas must retain transparent gutters and opaque artwork");
            var definitions = new List<SpriteMetaData>();
            for (int index = 0; index < Names.Length; index++)
            {
                int column = index % 3, row = index / 3;
                int left = Mathf.RoundToInt(column * source.width / 3f), right = Mathf.RoundToInt((column + 1) * source.width / 3f);
                int bottom = Mathf.RoundToInt(source.height - (row + 1) * source.height / 5f);
                int top = Mathf.RoundToInt(source.height - row * source.height / 5f);
                int expand = Mathf.CeilToInt(source.height / 5f * .12f);
                left = Mathf.Max(0, left - expand); right = Mathf.Min(source.width, right + expand);
                bottom = Mathf.Max(0, bottom - expand); top = Mathf.Min(source.height, top + expand);
                RectInt bounds = LargestComponent(pixels, source.width, left, right, bottom, top);
                int xMin = bounds.xMin, xMax = bounds.xMax, yMin = bounds.yMin, yMax = bounds.yMax;
                int margin = Mathf.CeilToInt(Mathf.Max(xMax - xMin, yMax - yMin) * .035f);
                xMin = Math.Max(left, xMin - margin); xMax = Math.Min(right, xMax + margin + 1);
                yMin = Math.Max(bottom, yMin - margin); yMax = Math.Min(top, yMax + margin + 1);
                definitions.Add(new SpriteMetaData { name = Names[index], alignment = 0, pivot = Vector2.one * .5f,
                    rect = new Rect(xMin, yMin, xMax - xMin, yMax - yMin) });
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
            Configure(importer, 2048);
            importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618
            importer.spritesheet = definitions.ToArray();
#pragma warning restore CS0618
            importer.SaveAndReimport();
        }
        finally { Object.DestroyImmediate(source); }

        foreach (string material in new[] { "FruitLabSatinSurface", "FruitLabInsetSurface" })
        {
            string path = "Assets/Resources/Art/" + material + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Configure(importer, 64);
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(14, 14, 14, 14);
            importer.SaveAndReimport();
        }
        const string roomPath = "Assets/Resources/Art/FruitLabRoomV1.png";
        if (File.Exists(roomPath))
        {
            AssetDatabase.ImportAsset(roomPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(roomPath);
            Configure(importer, 2048); importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        FruitLabStyle.ResetCachedAssets();
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToArray();
        if (sprites.Length != 15) throw new Exception("Theme atlas must import exactly fifteen named sprites");
        foreach (string name in Names.Take(9))
        {
            var config = AssetDatabase.LoadAssetAtPath<FruitColorConfig>("Assets/ScriptableObjects/Fruits/" + name + ".asset");
            config.fruitImage = sprites.Single(sprite => sprite.name == name);
            EditorUtility.SetDirty(config);
        }
        AssetDatabase.SaveAssets();
        const string sharedBar = "Assets/Prefabs/UI/CrystalProgressBar.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(sharedBar);
        try
        {
            var view = contents.GetComponent<VerticalCrystalProgressView>();
            view.ApplyLabStyle(); view.SetAlpha(1f);
            PrefabUtility.SaveAsPrefabAsset(contents, sharedBar);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }

    private static RectInt LargestComponent(Color32[] pixels, int stride, int left, int right, int bottom, int top)
    {
        int width = right - left;
        var visited = new bool[width * (top - bottom)];
        var queue = new Queue<int>();
        int largest = 0;
        RectInt best = default;
        for (int y = bottom; y < top; y++)
        for (int x = left; x < right; x++)
        {
            int local = (y - bottom) * width + x - left;
            if (visited[local]) continue;
            visited[local] = true;
            if (pixels[y * stride + x].a < 96) continue;
            queue.Enqueue(y * stride + x);
            int count = 0, xMin = x, xMax = x, yMin = y, yMax = y;
            while (queue.Count > 0)
            {
                int current = queue.Dequeue(), cx = current % stride, cy = current / stride;
                count++; xMin = Math.Min(xMin, cx); xMax = Math.Max(xMax, cx);
                yMin = Math.Min(yMin, cy); yMax = Math.Max(yMax, cy);
                for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < left || nx >= right || ny < bottom || ny >= top) continue;
                    int next = (ny - bottom) * width + nx - left;
                    if (visited[next]) continue;
                    visited[next] = true;
                    if (pixels[ny * stride + nx].a >= 96) queue.Enqueue(ny * stride + nx);
                }
            }
            if (count > largest) { largest = count; best = new RectInt(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1); }
        }
        if (largest < 80) throw new Exception("No isolated icon found in theme cell");
        return best;
    }

    private static void Configure(TextureImporter importer, int maxSize)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100f;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = maxSize;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }
}
