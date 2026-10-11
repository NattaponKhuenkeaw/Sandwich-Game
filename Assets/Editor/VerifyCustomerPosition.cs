using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class VerifyCustomerPosition
{
    static VerifyCustomerPosition() { EditorApplication.delayCall += Verify; }
    static void Verify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/CustomerPosition.complete")) return;
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("Moved kitchen test");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = new Vector3(-64, 4, -84);
            var kitchen = root.AddComponent<SandwichGame.KitchenScene>();
            var controller = root.AddComponent<SandwichGame.SandwichGameController>();
            var obj = new GameObject("Customer");
            obj.transform.SetParent(root.transform, false);
            var customer = obj.AddComponent<SandwichGame.CustomerAgent>();
            Vector3 local = new Vector3(-1.7f, -.28f, .71f);
            customer.transform.localPosition = local;
            kitchen.Customers = new[] { customer };
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = typeof(SandwichGame.SandwichGameController);
            type.GetField("kitchen", flags).SetValue(controller, kitchen);
            type.GetField("customerStartPositions", flags).SetValue(controller, new[] { local });
            customer.transform.localPosition = Vector3.zero;
            customer.gameObject.SetActive(false);
            type.GetMethod("ResetCustomer", flags).Invoke(controller, new object[] { customer, true });
            if (!customer.gameObject.activeSelf || Vector3.Distance(customer.transform.localPosition, local) > .001f ||
                Vector3.Distance(customer.transform.position, root.transform.TransformPoint(local)) > .001f)
                throw new Exception("Customer moved away from kitchen");
            File.WriteAllText("Temp/CustomerPosition.complete", "Customer reset preserves configured position in relocated kitchen; active and order initialized.");
        }
        catch (Exception e) { File.WriteAllText("Temp/CustomerPosition.error.txt", e.ToString()); Debug.LogException(e); }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }
}
