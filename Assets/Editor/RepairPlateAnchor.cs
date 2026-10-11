using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class RepairPlateAnchor
{
    static RepairPlateAnchor() { EditorApplication.delayCall += Repair; }
    static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/PlateAnchorRepair.complete")) return;
        try
        {
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            if (!scene.IsValid() || !scene.isLoaded) return;
            var kitchen = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SandwichGame.KitchenScene>(true)).Single();
            if (kitchen.Plate == null) throw new Exception("Plate station missing");
            Undo.RecordObject(kitchen, "Restore plate anchor");
            kitchen.PrepareRuntimeSlots();
            PrefabUtility.RecordPrefabInstancePropertyModifications(kitchen);
            EditorUtility.SetDirty(kitchen);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed");
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var host = new GameObject("Plate placement check");
                SceneManager.MoveGameObjectToScene(host, preview);
                var testKitchen = host.AddComponent<SandwichGame.KitchenScene>();
                var plate = new GameObject("Plate");
                plate.transform.SetParent(host.transform, false);
                testKitchen.Plate = plate.AddComponent<SandwichGame.StationTarget>();
                testKitchen.Plate.Kind = SandwichGame.StationKind.Plate;
                var item = new GameObject("Cooked meat").AddComponent<SandwichGame.ItemView>();
                item.transform.SetParent(host.transform, false);
                item.Kind = SandwichGame.ItemKind.CookedMeat;
                item.Held = true;
                var controller = host.AddComponent<SandwichGame.SandwichGameController>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(SandwichGame.SandwichGameController).GetField("kitchen", flags).SetValue(controller, testKitchen);
                typeof(SandwichGame.SandwichGameController).GetField("held", flags).SetValue(controller, item);
                bool placed = (bool)typeof(SandwichGame.SandwichGameController).GetMethod("PlaceOnPlate", flags).Invoke(controller, null);
                if (!placed || testKitchen.PlateItem != item || item.transform.parent != testKitchen.PlateAnchor || item.Held)
                    throw new Exception("Cooked meat placement check failed");
                File.WriteAllText("Temp/PlateAnchorRepair.complete", "SampleScene saved; missing anchor recreated; cooked meat placement passed.");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        catch (Exception e) { File.WriteAllText("Temp/PlateAnchorRepair.error.txt", e.ToString()); Debug.LogException(e); }
    }
}
