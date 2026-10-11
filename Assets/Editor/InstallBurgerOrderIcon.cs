using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class InstallBurgerOrderIcon
{
    static InstallBurgerOrderIcon() { EditorApplication.delayCall += Install; }
    static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/BurgerOrderIcon.complete")) return;
        try
        {
            const string icon = "Assets/SandwichGame/Resources/BurgerOrder.png";
            AssetDatabase.ImportAsset(icon);
            var importer = (TextureImporter)AssetImporter.GetAtPath(icon);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            if (AssetDatabase.LoadAssetAtPath<Sprite>(icon) == null) throw new Exception("Order sprite missing");
            using (var scope = new PrefabUtility.EditPrefabContentsScope("Assets/SandwichGame/Prefabs/Kitchen.prefab"))
                foreach (var customer in scope.prefabContentsRoot.GetComponentsInChildren<SandwichGame.CustomerAgent>(true))
                {
                    customer.SetOrder(1, SandwichGame.GameConstants.CustomerWait);
                    if (customer.OrderIcon == null || customer.OrderIcon.sprite == null || customer.OrderLabel.text != "x1") throw new Exception("Icon validation failed");
                    customer.SetOrder(2, SandwichGame.GameConstants.CustomerWait);
                    if (customer.OrderLabel.text != "x2") throw new Exception("Quantity validation failed");
                    customer.SetOrder(1, SandwichGame.GameConstants.CustomerWait);
                }
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (var customer in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SandwichGame.CustomerAgent>(true)))
                {
                    customer.SetOrder(1, SandwichGame.GameConstants.CustomerWait);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(customer);
                    EditorUtility.SetDirty(customer);
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/BurgerOrderIcon.complete", "Burger image imported; Kitchen and SampleScene updated; x1 and x2 verified.");
        }
        catch (Exception e) { File.WriteAllText("Temp/BurgerOrderIcon.error.txt", e.ToString()); Debug.LogException(e); }
    }
}
