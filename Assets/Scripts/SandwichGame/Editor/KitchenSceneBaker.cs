using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace SandwichGame.EditorTools
{
    [InitializeOnLoad]
    public static class KitchenSceneBaker
    {
        const string ScenePath = "Assets/Scenes/SampleScene.unity";
        const string RootFolder = "Assets/SandwichGame";
        const string MatFolder = "Assets/SandwichGame/Materials";
        const string PrefabFolder = "Assets/SandwichGame/Prefabs";

        static KitchenSceneBaker()
        {
            EditorApplication.delayCall += AutoBakeIfNeeded;
        }

        [MenuItem("Sandwich Game/Bake Kitchen Into Scene")]
        public static void BakeFromMenu()
        {
            Bake(true);
        }

        static void AutoBakeIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (Application.isPlaying) return;

            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
                return;

            if (Object.FindFirstObjectByType<KitchenScene>() != null)
                return;

            try
            {
                Bake(true);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }

        public static void Bake(bool save)
        {
            EnsureFolders();
            KitchenFactory.MaterialLookup = GetOrCreateMaterial;

            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            var existing = Object.FindFirstObjectByType<KitchenScene>();
            if (existing != null)
                KitchenFactory.SafeDestroy(existing.gameObject);

            var controller = Object.FindFirstObjectByType<SandwichGameController>();
            if (controller == null)
            {
                var host = new GameObject("SandwichGame");
                controller = host.AddComponent<SandwichGameController>();
            }

            var kitchenGo = new GameObject("Kitchen");
            kitchenGo.transform.SetParent(controller.transform, false);
            var kitchen = kitchenGo.AddComponent<KitchenScene>();
            KitchenFactory.Build(kitchen);

            kitchen.RawMeatPrefab = SaveItemPrefab(ItemKind.RawMeat, kitchen);
            kitchen.CookedMeatPrefab = SaveItemPrefab(ItemKind.CookedMeat, kitchen);
            kitchen.BurnedMeatPrefab = SaveItemPrefab(ItemKind.BurnedMeat, kitchen);
            kitchen.BunPrefab = SaveItemPrefab(ItemKind.Bun, kitchen);
            kitchen.SandwichPrefab = SaveItemPrefab(ItemKind.Sandwich, kitchen);

            if (controller.GetComponent<GameAudio>() == null)
                controller.gameObject.AddComponent<GameAudio>();

            EnsureEventSystem();

            PrefabUtility.SaveAsPrefabAssetAndConnect(kitchenGo, PrefabFolder + "/Kitchen.prefab", InteractionMode.AutomatedAction);

            var so = new SerializedObject(controller);
            so.FindProperty("kitchen").objectReferenceValue = kitchen;
            so.ApplyModifiedPropertiesWithoutUndo();

            CustomerBubbleFixer.FixBubbles(false);

            EditorSceneManager.MarkSceneDirty(scene);
            if (save)
                EditorSceneManager.SaveScene(scene);

            KitchenFactory.MaterialLookup = null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Sandwich Game: baked kitchen into SampleScene, materials, and prefabs.");
        }

        static ItemView SaveItemPrefab(ItemKind kind, KitchenScene kitchen)
        {
            var item = KitchenFactory.SpawnPrimitiveItem(kind, new Vector3(0f, -20f, 0f), kitchen.Root);
            var path = PrefabFolder + "/" + kind + ".prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(item.gameObject, path);
            KitchenFactory.SafeDestroy(item.gameObject);
            return prefab.GetComponent<ItemView>();
        }

        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(RootFolder))
                AssetDatabase.CreateFolder("Assets", "SandwichGame");
            if (!AssetDatabase.IsValidFolder(MatFolder))
                AssetDatabase.CreateFolder(RootFolder, "Materials");
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder(RootFolder, "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/SandwichGame/Resources"))
                AssetDatabase.CreateFolder(RootFolder, "Resources");
            EnsureWhiteSprite();
        }

        static void EnsureWhiteSprite()
        {
            const string path = "Assets/SandwichGame/Resources/UIWhite.png";
            if (File.Exists(path)) return;

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 4f;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        static Material GetOrCreateMaterial(string name, Color color, float smooth, float metal)
        {
            var path = MatFolder + "/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(KitchenFactory.LitShader());
                mat.name = name;
                AssetDatabase.CreateAsset(mat, path);
            }

            KitchenFactory.ApplyColor(mat, color, smooth, metal);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
