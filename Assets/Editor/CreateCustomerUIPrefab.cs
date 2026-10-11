using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SandwichGame;
[InitializeOnLoad]
public static class CreateCustomerUIPrefab
{
    const string PathName = "Assets/SandwichGame/Prefabs/CustomerOrderUI.prefab";
    static CreateCustomerUIPrefab() { EditorApplication.delayCall += Create; }
    static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/CustomerUIPrefab.complete")) return;
        try
        {
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            if (!scene.IsValid() || !scene.isLoaded) return;
            var customers = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CustomerAgent>(true)).ToArray();
            if (customers.Length != 3) throw new Exception("Expected three customers");
            var first = customers.OrderBy(c => c.SlotIndex).First();
            if (!File.Exists(PathName))
            {
                var copy = UnityEngine.Object.Instantiate(first.Bubble.gameObject);
                try
                {
                    copy.name = "CustomerOrderUI";
                    var ui = copy.AddComponent<CustomerOrderUI>();
                    ui.Canvas = copy.GetComponent<Canvas>();
                    ui.Canvas.worldCamera = null;
                    ui.OrderIcon = copy.transform.Find("BurgerOrderIcon").GetComponent<UnityEngine.UI.Image>();
                    ui.Quantity = copy.transform.Find("Order").GetComponent<UnityEngine.UI.Text>();
                    ui.TimerFill = copy.GetComponentsInChildren<UnityEngine.UI.Image>(true).Single(i => i.name == "Fill");
                    PrefabUtility.SaveAsPrefabAsset(copy, PathName);
                }
                finally { UnityEngine.Object.DestroyImmediate(copy); }
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathName);
            void Connect(CustomerAgent customer)
            {
                var old = customer.Bubble != null ? customer.Bubble.transform : customer.transform.Find("Bubble");
                var position = old != null ? old.localPosition : new Vector3(0, 2.15f, 0);
                var rotation = old != null ? old.localRotation : Quaternion.identity;
                if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, customer.transform);
                instance.name = "Bubble";
                instance.transform.localPosition = position;
                instance.transform.localRotation = rotation;
                var ui = instance.GetComponent<CustomerOrderUI>();
                customer.Bubble = ui.Canvas;
                customer.OrderIcon = ui.OrderIcon;
                customer.OrderLabel = ui.Quantity;
                customer.TimerFill = ui.TimerFill;
                customer.RepairBubble();
                PrefabUtility.RecordPrefabInstancePropertyModifications(customer);
                EditorUtility.SetDirty(customer);
                if (ui.OrderIcon == null || ui.Quantity == null || ui.TimerFill == null || ui.Canvas == null)
                    throw new Exception("UI references incomplete");
                customer.SetOrder(2, GameConstants.CustomerWait);
                if (ui.Quantity.text != "x2") throw new Exception("Order quantity failed");
                customer.SetOrder(1, GameConstants.CustomerWait);
            }
            foreach (var customer in customers) Connect(customer);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Scene save failed");
            using (var scope = new PrefabUtility.EditPrefabContentsScope("Assets/SandwichGame/Prefabs/Kitchen.prefab"))
                foreach (var customer in scope.prefabContentsRoot.GetComponentsInChildren<CustomerAgent>(true)) Connect(customer);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/CustomerUIPrefab.complete", "CustomerOrderUI prefab created; three scene customers and Kitchen linked; icon, quantity and timer references verified.");
        }
        catch (Exception e) { File.WriteAllText("Temp/CustomerUIPrefab.error.txt", e.ToString()); Debug.LogException(e); }
    }
}
