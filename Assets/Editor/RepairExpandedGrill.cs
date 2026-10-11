using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SandwichGame;
[InitializeOnLoad]
public static class RepairExpandedGrill
{
    static RepairExpandedGrill(){EditorApplication.delayCall+=Repair;}
    static void Repair()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/ExpandedGrill.complete"))return;
        try
        {
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            if(!scene.IsValid()||!scene.isLoaded)return;
            var kitchen=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<KitchenScene>(true)).Single();
            Undo.RecordObject(kitchen,"Connect expanded grill");
            kitchen.PrepareRuntimeSlots();
            foreach(var slot in kitchen.GrillSlots){PrefabUtility.RecordPrefabInstancePropertyModifications(slot);EditorUtility.SetDirty(slot);}
            PrefabUtility.RecordPrefabInstancePropertyModifications(kitchen);EditorUtility.SetDirty(kitchen);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            var testScene=EditorSceneManager.NewPreviewScene();
            try
            {
                var root=new GameObject("Six grill slots check");SceneManager.MoveGameObjectToScene(root,testScene);
                var test=root.AddComponent<KitchenScene>();var controller=root.AddComponent<SandwichGameController>();
                for(int i=0;i<6;i++)
                {
                    var obj=new GameObject("GrillSlot"+i);obj.transform.SetParent(root.transform,false);
                    var station=obj.AddComponent<StationTarget>();station.Kind=StationKind.Grill;station.SlotIndex=0;
                }
                test.PrepareRuntimeSlots();
                if(test.GrillItems.Length!=6||test.GrillSlots.Select(s=>s.SlotIndex).Distinct().Count()!=6)throw new Exception("Six-slot initialization failed");
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;var type=typeof(SandwichGameController);
                type.GetField("kitchen",flags).SetValue(controller,test);
                for(int i=0;i<6;i++)
                {
                    var obj=new GameObject("Meat"+i);obj.transform.SetParent(root.transform,false);
                    var item=obj.AddComponent<ItemView>();item.Kind=ItemKind.RawMeat;item.Held=true;
                    type.GetField("held",flags).SetValue(controller,item);
                    if(!(bool)type.GetMethod("PlaceOnGrill",flags).Invoke(controller,new object[]{i})||test.GrillItems[i]!=item||item.GrillSlot!=i)throw new Exception("Placement failed at slot "+i);
                }
                test.PrepareRuntimeSlots();
                if(test.GrillItems.Any(i=>i==null))throw new Exception("Preparation cleared grill items");
                File.WriteAllText("Temp/ExpandedGrill.complete","Connected scene grill slots: "+kitchen.GrillSlots.Length+". Six-slot placement and occupied-slot preservation passed.");
            }
            finally{EditorSceneManager.ClosePreviewScene(testScene);}
        }
        catch(Exception e){File.WriteAllText("Temp/ExpandedGrill.error.txt",e.ToString());Debug.LogException(e);}
    }
}
