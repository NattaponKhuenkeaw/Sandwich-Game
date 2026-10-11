using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class BurgerPrefabDiagnostics
{
    static BurgerPrefabDiagnostics() { EditorApplication.delayCall += Check; }
    [MenuItem("Tools/Burger/Check Prefabs")]
    public static void Check()
    {
        var report = new StringBuilder();
        string folder = "Assets/store/Pack_FREE_Burger";
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(folder + "/Models/burgers.fbx"))
            if (asset is Mesh mesh)
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long id);
                report.AppendLine("MESH " + mesh.name + " ID " + id + " vertices " + mesh.vertexCount);
            }
        foreach (var path in Directory.GetFiles(folder + "/Prefabs", "*.prefab"))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path.Replace('\\', '/'));
            if (prefab == null) { report.AppendLine(path + " FAILED TO LOAD"); continue; }
            var filter = prefab.GetComponent<MeshFilter>();
            var renderer = prefab.GetComponent<Renderer>();
            var mat = renderer != null ? renderer.sharedMaterial : null;
            report.AppendLine(prefab.name + " mesh=" + (filter != null && filter.sharedMesh != null ? filter.sharedMesh.name : "MISSING") +
                " material=" + (mat != null ? mat.name : "MISSING") + " shader=" + (mat != null && mat.shader != null ? mat.shader.name : "MISSING"));
        }
        Directory.CreateDirectory("Temp");
        File.WriteAllText("Temp/BurgerPrefabDiagnostics.txt", report.ToString());
    }
}
