using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class UseUltimateBurgerPatty
{
    [MenuItem("Sandwich Game/Setup/Rebuild Patty Visuals")]
    static void Convert()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/UltimatePatty.complete")) return;
        try
        {
            string[] sources = { "Raw", "Cooked", "Burned" };
            string[] targets = { "MeatPile", "CookedMeat", "BurnedMeat" };
            Directory.CreateDirectory("Temp/UltimatePattyBackup");
            for (int kind = 0; kind < 3; kind++)
            {
                string path = "Assets/SandwichGame/Prefabs/" + targets[kind] + ".prefab";
                File.Copy(path, "Temp/UltimatePattyBackup/" + targets[kind] + ".prefab", true);
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var item = root.GetComponent<SandwichGame.ItemView>();
                    if (item == null) throw new Exception("ItemView missing: " + path);
                    for (int c = root.transform.childCount - 1; c >= 0; c--)
                        UnityEngine.Object.DestroyImmediate(root.transform.GetChild(c).gameObject);
                    item.MeatVisuals = new GameObject[3];
                    item.ColorRenderers = new Renderer[0];
                    for (int state = 0; state < 3; state++)
                    {
                        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/store/Ultimate_Food_Pack/Prefabs/BurgerPatty_" + sources[state] + ".prefab");
                        if (source == null) throw new Exception("Patty source missing");
                        var visual = UnityEngine.Object.Instantiate(source, root.transform, false);
                        visual.name = "BurgerPatty_" + sources[state];
                        visual.transform.localPosition = Vector3.zero;
                        visual.transform.localRotation = Quaternion.identity;
                        var renderers = visual.GetComponentsInChildren<Renderer>(true);
                        if (renderers.Length == 0) throw new Exception("Patty renderer missing");
                        var bounds = renderers[0].bounds;
                        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                        float diameter = Mathf.Max(bounds.size.x, bounds.size.z);
                        if (diameter <= 0f) throw new Exception("Invalid patty bounds");
                        visual.transform.localScale *= 0.18f / diameter;
                        bounds = renderers[0].bounds;
                        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                        visual.transform.position += root.transform.position - bounds.center;
                        foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                        foreach (var behaviour in visual.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(behaviour);
                        item.MeatVisuals[state] = visual;
                    }
                    item.Kind = (SandwichGame.ItemKind)kind;
                    item.ApplyKindVisual();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<SandwichGame.ItemView>();
                if (saved.MeatVisuals.Length != 3 || !saved.MeatVisuals[kind].activeSelf) throw new Exception("Patty validation failed");
                for (int state = 0; state < 3; state++)
                {
                    saved.Kind = (SandwichGame.ItemKind)state;
                    saved.ApplyKindVisual();
                    for (int v = 0; v < 3; v++) if (saved.MeatVisuals[v].activeSelf != (v == state)) throw new Exception("State transition failed");
                }
                saved.Kind = (SandwichGame.ItemKind)kind;
                saved.ApplyKindVisual();
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/UltimatePatty.complete", "Converted MeatPile, CookedMeat and BurnedMeat; all three visual states verified.");
        }
        catch (Exception e) { File.WriteAllText("Temp/UltimatePatty.error.txt", e.ToString()); Debug.LogException(e); }
    }
}
