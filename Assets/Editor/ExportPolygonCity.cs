using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExportPolygonCity
{
    private const string Output = "Assets/Prefabs/POLYGON_City_Objects.prefab";

    [MenuItem("Tools/POLYGON City/Export City Objects Prefab")]
    public static void Export()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Scene preview = default;
        try
        {
            preview = EditorSceneManager.OpenPreviewScene("Assets/store/POLYGON city pack/scene/DemoScene.unity");
            var objects = preview.GetRootGameObjects();
            var city = new GameObject("POLYGON_City_Objects");
            SceneManager.MoveGameObjectToScene(city, preview);
            foreach (var obj in objects)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(obj))
                    PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                obj.transform.SetParent(city.transform, true);
            }
            foreach (var t in city.GetComponentsInChildren<Transform>(true))
                if (PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))
                    PrefabUtility.UnpackPrefabInstance(t.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            int removedLights = 0;
            foreach (var behaviour in city.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour != null && behaviour.GetType().Name.Contains("AdditionalLightData"))
                    UnityEngine.Object.DestroyImmediate(behaviour);
            foreach (var light in city.GetComponentsInChildren<Light>(true))
            {
                UnityEngine.Object.DestroyImmediate(light);
                removedLights++;
            }
            foreach (var component in city.GetComponentsInChildren<Component>(true))
            {
                if (component == null) continue;
                string type = component.GetType().Name;
                if (component is Camera || component is AudioListener || component is ReflectionProbe ||
                    component is LightProbeGroup || type.Contains("AdditionalLightData") ||
                    type.Contains("AdditionalCameraData") || type == "Volume" || component is FlareLayer)
                    UnityEngine.Object.DestroyImmediate(component);
            }
            // Remove empty lighting/camera objects, while retaining every geometry hierarchy.
            var transforms = city.GetComponentsInChildren<Transform>(true);
            for (int i = transforms.Length - 1; i >= 0; i--)
            {
                var t = transforms[i];
                if (t == city.transform || t.childCount != 0) continue;
                if (t.GetComponents<Component>().Length == 1)
                    UnityEngine.Object.DestroyImmediate(t.gameObject);
            }
            Directory.CreateDirectory("Assets/Prefabs");
            var prefab = PrefabUtility.SaveAsPrefabAsset(city, Output, out bool success);
            if (!success || prefab == null) throw new Exception("Prefab save failed");
            if (prefab.GetComponentsInChildren<Light>(true).Length != 0)
                throw new Exception("Prefab still contains lights");
            var report = "Saved " + Output + "\nTransforms: " + prefab.GetComponentsInChildren<Transform>(true).Length +
                "\nRenderers: " + prefab.GetComponentsInChildren<Renderer>(true).Length +
                "\nRemoved lights: " + removedLights + "\nRemaining lights: 0\n";
            File.WriteAllText("Temp/PolygonCityExport.txt", report);
            File.WriteAllText("Temp/PolygonCityExport.complete", report);
            Debug.Log(report);
        }
        catch (Exception ex)
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/PolygonCityExport.txt", ex.ToString());
            Debug.LogException(ex);
        }
        finally
        {
            if (preview.IsValid()) EditorSceneManager.ClosePreviewScene(preview);
        }
    }
}
