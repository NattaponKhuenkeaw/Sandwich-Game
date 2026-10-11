using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class StyleCustomerOrders
{
    static StyleCustomerOrders() { EditorApplication.delayCall += Apply; }
    static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/CustomerOrderStyleV2.complete")) return;
        try
        {
            const string path = "Assets/SandwichGame/Resources/OrderPanelRounded.png";
            var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            var colors = new Color[128 * 128];
            for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
            {
                float dx = Mathf.Max(16f - (x + .5f), x + .5f - 112f, 0f);
                float dy = Mathf.Max(16f - (y + .5f), y + .5f - 112f, 0f);
                float alpha = Mathf.Clamp01(16.5f - Mathf.Sqrt(dx * dx + dy * dy));
                colors[y * 128 + x] = new Color(1f, 1f, 1f, alpha);
            }
            texture.SetPixels(colors); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(18, 18, 18, 18);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            using (var scope = new PrefabUtility.EditPrefabContentsScope("Assets/SandwichGame/Prefabs/Kitchen.prefab"))
                foreach (var customer in scope.prefabContentsRoot.GetComponentsInChildren<SandwichGame.CustomerAgent>(true))
                {
                    customer.EnsureOrderIcon(); customer.StyleOrderBubble(); customer.RefreshBubble();
                    var panel = customer.Bubble.transform.Find("OrderPanelFrame");
                    if (panel == null) throw new Exception("Frame missing");
                }
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (var customer in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SandwichGame.CustomerAgent>(true)))
                {
                    customer.EnsureOrderIcon(); customer.StyleOrderBubble(); customer.RefreshBubble();
                    EditorUtility.SetDirty(customer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(customer);
                }
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/CustomerOrderStyleV2.complete", "Rounded frame, translucent cream background, shadow and count styling applied to Kitchen and SampleScene.");
        }
        catch (Exception e) { File.WriteAllText("Temp/CustomerOrderStyle.error.txt", e.ToString()); Debug.LogException(e); }
    }
}
