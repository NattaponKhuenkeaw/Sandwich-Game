using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SandwichGame.EditorTools
{
    [InitializeOnLoad]
    public static class CustomerBubbleFixer
    {
        const string SpritePath = "Assets/SandwichGame/Resources/UIWhite.png";
        const string PrefabPath = "Assets/SandwichGame/Prefabs/Kitchen.prefab";
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        static CustomerBubbleFixer()
        {
            EditorApplication.delayCall += FixIfNeeded;
        }

        [MenuItem("Sandwich Game/Fix Customer Bubbles")]
        public static void FixFromMenu()
        {
            FixBubbles(true);
        }

        static void FixIfNeeded()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            FixBubbles(true);
        }

        public static void FixBubbles(bool save)
        {
            var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                Debug.LogWarning("Sandwich Game: UIWhite sprite not found at " + SpritePath);
                return;
            }

            bool changed = false;
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            {
                using (var scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath))
                {
                    if (ApplyToRoot(scope.prefabContentsRoot, sprite))
                        changed = true;
                }
            }

            var scene = SceneManager.GetActiveScene();
            if (scene.path == ScenePath)
            {
                var agents = Object.FindObjectsByType<CustomerAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < agents.Length; i++)
                {
                    if (ApplyToRoot(agents[i].gameObject, sprite))
                    {
                        EditorUtility.SetDirty(agents[i]);
                        changed = true;
                    }
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (save) EditorSceneManager.SaveScene(scene);
                }
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
                Debug.Log("Sandwich Game: customer bubbles now use the opaque UIWhite sprite in the Editor.");
            }
        }

        static bool ApplyToRoot(GameObject root, Sprite sprite)
        {
            bool changed = false;
            var images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                var img = images[i];
                if (img.sprite != sprite)
                {
                    img.sprite = sprite;
                    changed = true;
                }

                var color = img.color;
                if (color.a < 1f && color.r > 0.85f && color.g > 0.85f && color.b > 0.85f)
                {
                    img.color = new Color(color.r, color.g, color.b, 1f);
                    changed = true;
                }

                img.canvasRenderer.cullTransparentMesh = false;
            }

            var canvases = root.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas.name != "Bubble") continue;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 20;
                canvas.transform.localRotation = Quaternion.identity;
                var cam = Camera.main;
                if (cam != null) canvas.worldCamera = cam;
            }

            return changed;
        }
    }
}
