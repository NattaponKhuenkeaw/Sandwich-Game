using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RepairSampleKitchen
{
    static RepairSampleKitchen()
    {
        if (!File.Exists("Temp/RepairSampleKitchen.complete")) EditorApplication.delayCall += Repair;
    }

    [MenuItem("Sandwich Game/Repair SampleScene Kitchen References")]
    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        try
        {
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            if (!scene.IsValid() || !scene.isLoaded) return;
            var kitchens = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SandwichGame.KitchenScene>(true)).ToArray();
            if (kitchens.Length != 1) throw new Exception("Expected one KitchenScene, found " + kitchens.Length);
            var kitchen = kitchens[0];
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SandwichGame/Prefabs/Kitchen.prefab");
            if (template == null) throw new Exception("Kitchen template missing");
            var report = new StringBuilder();
            Transform Find(string name)
            {
                var direct = kitchen.transform.Find(name);
                if (direct != null) return direct;
                var matches = kitchen.GetComponentsInChildren<Transform>(true).Where(t => t.name == name && (t.GetComponent<SandwichGame.StationTarget>() != null || name == "Grill" && t.Find("GrillSlot0") != null)).ToArray();
                if (matches.Length > 1) throw new Exception("Ambiguous object: " + name);
                if (matches.Length == 1) return matches[0];
                var source = template.transform.Find(name);
                if (source == null) throw new Exception("Missing template object: " + name);
                var copy = UnityEngine.Object.Instantiate(source.gameObject, kitchen.transform, false);
                copy.name = name;
                Undo.RegisterCreatedObjectUndo(copy, "Restore kitchen station");
                report.AppendLine("Restored missing object: " + name);
                return copy.transform;
            }
            SandwichGame.StationTarget Station(string name, SandwichGame.StationKind kind)
            {
                var obj = Find(name).gameObject;
                var station = obj.GetComponent<SandwichGame.StationTarget>();
                if (station == null) station = Undo.AddComponent<SandwichGame.StationTarget>(obj);
                Undo.RecordObject(station, "Repair kitchen station");
                station.Kind = kind;
                if (obj.GetComponent<Collider>() == null)
                {
                    var source = template.GetComponentsInChildren<SandwichGame.StationTarget>(true).First(s => s.Kind == kind);
                    var original = source.GetComponent<BoxCollider>();
                    var collider = Undo.AddComponent<BoxCollider>(obj);
                    if (original != null) { collider.center = original.center; collider.size = original.size; }
                }
                if (station.HighlightRenderer == null) station.HighlightRenderer = obj.GetComponentInChildren<Renderer>();
                PrefabUtility.RecordPrefabInstancePropertyModifications(station);
                return station;
            }
            Undo.RecordObject(kitchen, "Repair kitchen references");
            kitchen.MeatPile = Station("MeatPile", SandwichGame.StationKind.MeatPile);
            kitchen.BunPile = Station("BunPile", SandwichGame.StationKind.BunPile);
            kitchen.Trash = Station("Trash", SandwichGame.StationKind.Trash);
            kitchen.Plate = Station("Plate", SandwichGame.StationKind.Plate);
            kitchen.PlateAnchor = kitchen.Plate.transform.Find("Anchor");
            if (kitchen.PlateAnchor == null)
            {
                var anchor = new GameObject("Anchor");
                Undo.RegisterCreatedObjectUndo(anchor, "Restore plate anchor");
                anchor.transform.SetParent(kitchen.Plate.transform, false);
                var original = template.transform.Find("Plate/Anchor");
                if (original != null) anchor.transform.localPosition = original.localPosition;
                kitchen.PlateAnchor = anchor.transform;
            }
            var grill = Find("Grill");
            kitchen.GrillSlots = new SandwichGame.StationTarget[3];
            kitchen.GrillAnchors = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = grill.Find("GrillSlot" + i);
                if (slot == null)
                {
                    var original = template.transform.Find("Grill/GrillSlot" + i);
                    if (original == null) throw new Exception("Missing grill slot template");
                    var copy = UnityEngine.Object.Instantiate(original.gameObject, grill, false);
                    copy.name = original.name;
                    Undo.RegisterCreatedObjectUndo(copy, "Restore grill slot");
                    slot = copy.transform;
                }
                var target = slot.GetComponent<SandwichGame.StationTarget>();
                if (target == null) target = Undo.AddComponent<SandwichGame.StationTarget>(slot.gameObject);
                Undo.RecordObject(target, "Repair grill slot");
                target.Kind = SandwichGame.StationKind.Grill;
                target.SlotIndex = i;
                PrefabUtility.RecordPrefabInstancePropertyModifications(target);
                kitchen.GrillSlots[i] = target;
                kitchen.GrillAnchors[i] = slot;
            }
            kitchen.GrillLight = grill.GetComponentInChildren<Light>(true);
            if (kitchen.Camera == null) kitchen.Camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c => c.CompareTag("MainCamera"));
            if (kitchen.Camera == null) throw new Exception("Main camera missing");
            PrefabUtility.RecordPrefabInstancePropertyModifications(kitchen);
            EditorUtility.SetDirty(kitchen);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed");
            report.AppendLine("Saved SampleScene. MeatPile, BunPile, Trash, Plate, PlateAnchor, 3 GrillSlots and 3 GrillAnchors verified.");
            report.AppendLine("Camera: " + kitchen.Camera.name + "; GrillLight: " + (kitchen.GrillLight != null ? kitchen.GrillLight.name : "none (optional)"));
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/RepairSampleKitchen.complete", report.ToString());
            File.WriteAllText("Temp/RepairSampleKitchen.txt", report.ToString());
            Debug.Log(report.ToString());
        }
        catch (Exception e) { Directory.CreateDirectory("Temp"); File.WriteAllText("Temp/RepairSampleKitchen.txt", e.ToString()); Debug.LogException(e); }
    }
}
