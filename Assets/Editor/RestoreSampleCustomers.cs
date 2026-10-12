using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RestoreSampleCustomers
{
    [MenuItem("Sandwich Game/Setup/Restore Customer Templates")]
    static void Restore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/CustomersRestored.complete")) return;
        try
        {
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            if (!scene.IsValid() || !scene.isLoaded) return;
            var kitchen = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<SandwichGame.KitchenScene>(true)).Single();
            if (kitchen.Customers.Length != 3 || kitchen.Customers.Any(c => c == null)) throw new Exception("Customer references incomplete");
            foreach (var customer in kitchen.Customers)
            {
                Undo.RecordObject(customer.gameObject, "Restore customers");
                customer.gameObject.SetActive(true);
                PrefabUtility.RecordPrefabInstancePropertyModifications(customer.gameObject);
                if (!customer.gameObject.activeInHierarchy) throw new Exception("Customer parent is inactive");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed");
            File.WriteAllText("Temp/CustomersRestored.complete", "All three customers active, references verified, SampleScene saved.");
        }
        catch (Exception e) { File.WriteAllText("Temp/CustomersRestored.error.txt", e.ToString()); Debug.LogException(e); }
    }
}
