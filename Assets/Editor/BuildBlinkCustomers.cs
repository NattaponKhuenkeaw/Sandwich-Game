using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SandwichGame;
[InitializeOnLoad]
public static class BuildBlinkCustomers
{
    const string Folder = "Assets/SandwichGame/Customers";
    const string Source = "Assets/store/Blink/Art/Characters/LowPoly/FREE_HumanLowPoly/Prefabs_Humans/HumanMale_Character_FREE.prefab";
    const string IdleSource = "Assets/store/Blink/Art/Animations/Animations_Starter_Pack/Movement/Idle.fbx";
    static BuildBlinkCustomers() { EditorApplication.delayCall += Build; }
    static AnimationClip Clip(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
    static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/BlinkCustomers.complete")) return;
        try
        {
            foreach (var dir in new[] { "Models", "Materials", "Textures", "Animations", "Prefabs" }) Directory.CreateDirectory(Folder + "/" + dir);
            AssetDatabase.Refresh();
            var map = new Dictionary<string,string>();
            var dependencies = AssetDatabase.GetDependencies(Source, true).Concat(new[] { IdleSource }).Distinct();
            foreach (var path in dependencies)
            {
                string ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                string dir = ext == ".fbx" ? "Models" : ext == ".mat" ? "Materials" : ext == ".png" ? "Textures" : null;
                if (dir == null) continue;
                if (ext == ".fbx" && path.Contains("Animations") && path != IdleSource) continue;
                string dest = Folder + "/" + (path == IdleSource ? "Animations" : dir) + "/" + System.IO.Path.GetFileName(path);
                if (!File.Exists(dest) && !AssetDatabase.CopyAsset(path, dest)) throw new Exception("Copy failed: " + path);
                map[path] = dest;
            }
            UnityEngine.Object Remap(UnityEngine.Object obj)
            {
                if (obj == null) return null;
                string sourcePath = AssetDatabase.GetAssetPath(obj);
                if (!map.TryGetValue(sourcePath, out string targetPath)) return obj;
                return AssetDatabase.LoadAllAssetsAtPath(targetPath).FirstOrDefault(a => a.GetType() == obj.GetType() && a.name == obj.name) ?? obj;
            }
            foreach (var path in map.Values.Where(p => p.EndsWith(".mat")))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                var texture = mat.GetTexture("_BaseMap");
                if (texture == null && mat.HasProperty("_MainTex")) texture = mat.GetTexture("_MainTex");
                var color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
                mat.shader = Shader.Find("Universal Render Pipeline/Lit");
                mat.SetTexture("_BaseMap", (Texture)Remap(texture)); mat.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(mat);
            }
            var idle = Clip(map[IdleSource]);
            var settings = AnimationUtility.GetAnimationClipSettings(idle); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(idle, settings);
            string idleControllerPath = Folder + "/Animations/CustomerIdle.controller";
            var idleController = AssetDatabase.LoadAssetAtPath<AnimatorController>(idleControllerPath) ?? AnimatorController.CreateAnimatorControllerAtPath(idleControllerPath);
            var sm = idleController.layers[0].stateMachine;
            foreach (var state in sm.states) sm.RemoveState(state.state);
            var idleState = sm.AddState("Waiting"); idleState.motion = idle; sm.defaultState = idleState;
            string reactionsPath = Folder + "/Animations/CustomerReactions.controller";
            var reactions = AssetDatabase.LoadAssetAtPath<AnimatorController>(reactionsPath) ?? AnimatorController.CreateAnimatorControllerAtPath(reactionsPath);
            var machine = reactions.layers[0].stateMachine;
            foreach (var state in machine.states) machine.RemoveState(state.state);
            foreach (var stateName in new[] { "Waiting", "Received", "NotReceived" })
            {
                string clipPath = Folder + "/Animations/" + stateName + ".anim";
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                if (clip == null) { clip = new AnimationClip(); AssetDatabase.CreateAsset(clip, clipPath); }
                clip.name = stateName;
                var y = stateName == "Received" ? new AnimationCurve(new Keyframe(0,0),new Keyframe(.2f,.10f),new Keyframe(.4f,0),new Keyframe(.65f,.06f),new Keyframe(.85f,0),new Keyframe(1.25f,0)) : AnimationCurve.Constant(0,1.25f,0);
                var tilt = stateName == "NotReceived" ? new AnimationCurve(new Keyframe(0,0),new Keyframe(.2f,-6),new Keyframe(.4f,6),new Keyframe(.6f,-4),new Keyframe(.8f,4),new Keyframe(1.25f,0)) : AnimationCurve.Constant(0,1.25f,0);
                var nod = stateName == "Received" ? new AnimationCurve(new Keyframe(0,0),new Keyframe(.2f,8),new Keyframe(.4f,0),new Keyframe(.65f,6),new Keyframe(.85f,0),new Keyframe(1.25f,0)) : AnimationCurve.Constant(0,1.25f,0);
                clip.SetCurve("",typeof(Transform),"localPosition.y",y);
                clip.SetCurve("",typeof(Transform),"localEulerAnglesRaw.z",tilt);
                clip.SetCurve("",typeof(Transform),"localEulerAnglesRaw.x",nod);
                var st = machine.AddState(stateName); st.motion = clip;
                if (stateName == "Waiting") machine.defaultState = st;
                EditorUtility.SetDirty(clip);
            }
            var root = new GameObject("BlinkCustomerVisual");
            try
            {
                var behaviour = root.AddComponent<BlinkCustomerVisual>();
                behaviour.Variants = new GameObject[4];
                behaviour.ReactionAnimator = root.AddComponent<Animator>();
                behaviour.ReactionAnimator.runtimeAnimatorController = reactions;
                behaviour.ReactionAnimator.applyRootMotion = false;
                behaviour.ReactionAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                Color[] palette = { new Color(.25f,.48f,.78f), new Color(.72f,.25f,.22f), new Color(.27f,.58f,.37f), new Color(.62f,.39f,.74f) };
                for (int i=0;i<4;i++)
                {
                    var character = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Source),root.transform,false);
                    character.name = "Customer_" + new[] { "Blue", "Red", "Green", "Purple" }[i];
                    character.transform.localPosition = Vector3.zero;
                    character.transform.localRotation = Quaternion.Euler(0,180,0);
                    foreach (var t in character.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.StartsWith("PlateSet1") || t.name == "Underwear" || t.name == "Arms" || t.name == "Chest" || t.name == "Legs" || t.name == "Feet") t.gameObject.SetActive(false);
                        if (t.name.StartsWith("Starter_")) t.gameObject.SetActive(true);
                        if (t.name == "Beard6") t.gameObject.SetActive(i == 1 || i == 3);
                        if (t.name == "Moustache4") t.gameObject.SetActive(i == 2 || i == 3);
                        if (t.name == "Hair7") t.gameObject.SetActive(i != 2);
                    }
                    var renderers = character.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    foreach (var renderer in renderers)
                    {
                        renderer.sharedMesh = (Mesh)Remap(renderer.sharedMesh);
                        var mats = renderer.sharedMaterials;
                        for (int m=0;m<mats.Length;m++)
                        {
                            mats[m] = (Material)Remap(mats[m]);
                            if (renderer.name.StartsWith("Starter_"))
                            {
                                string matPath = Folder + "/Materials/" + character.name + "_" + renderer.name + "_" + m + ".mat";
                                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                                if (mat == null) { mat = new Material(mats[m]); AssetDatabase.CreateAsset(mat,matPath); }
                                mat.SetColor("_BaseColor", renderer.name.Contains("Chest") ? palette[i] : Color.Lerp(palette[i],Color.black,.5f));
                                mats[m] = mat;
                            }
                        }
                        renderer.sharedMaterials = mats;
                    }
                    var animator = character.GetComponent<Animator>();
                    animator.avatar = (Avatar)Remap(animator.avatar);
                    if (animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman) throw new Exception("Invalid humanoid avatar");
                    animator.runtimeAnimatorController = idleController;
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    var active = renderers.Where(r=>r.gameObject.activeInHierarchy).ToArray();
                    var bounds = active[0].bounds; foreach (var r in active) bounds.Encapsulate(r.bounds);
                    character.transform.localScale *= 2.21f / bounds.size.y;
                    bounds = active[0].bounds; foreach (var r in active) bounds.Encapsulate(r.bounds);
                    character.transform.localPosition -= new Vector3(bounds.center.x-root.transform.position.x,bounds.min.y-root.transform.position.y,bounds.center.z-root.transform.position.z);
                    behaviour.Variants[i]=character;
                    character.SetActive(i==0);
                }
                behaviour.VariantIndex = -1;
                PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Prefabs/BlinkCustomerVisual.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            var visualPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Prefabs/BlinkCustomerVisual.prefab");
            void Attach(CustomerAgent customer)
            {
                var old = customer.transform.Find("BlinkCustomerVisual"); if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
                foreach(var renderer in customer.GetComponentsInChildren<MeshRenderer>(true))
                    if(renderer.GetComponentInParent<Canvas>()==null)renderer.enabled=false;
                var visual=(GameObject)PrefabUtility.InstantiatePrefab(visualPrefab,customer.transform);
                visual.name="BlinkCustomerVisual"; visual.transform.localPosition=Vector3.zero;
                visual.transform.localScale=Vector3.one / customer.transform.lossyScale.y;
                var script=visual.GetComponent<BlinkCustomerVisual>();
                for(int i=0;i<4;i++)script.Variants[i].SetActive(i==customer.SlotIndex%4);
                script.VariantIndex=-1;
                PrefabUtility.RecordPrefabInstancePropertyModifications(script);
                foreach(var r in customer.GetComponentsInChildren<MeshRenderer>(true)) if(!r.enabled)PrefabUtility.RecordPrefabInstancePropertyModifications(r);
            }
            using(var scope=new PrefabUtility.EditPrefabContentsScope("Assets/SandwichGame/Prefabs/Kitchen.prefab"))
                foreach(var customer in scope.prefabContentsRoot.GetComponentsInChildren<CustomerAgent>(true))Attach(customer);
            var scene=SceneManager.GetSceneByPath("Assets/Scenes/SampleScene.unity");
            if(scene.IsValid()&&scene.isLoaded)
            {
                foreach(var customer in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CustomerAgent>(true)))Attach(customer);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
            File.WriteAllText("Temp/BlinkCustomers.complete","Four Blink customer appearances created, humanoid avatars validated, idle and reaction controllers linked, Kitchen and SampleScene saved.");
        }
        catch(Exception e){File.WriteAllText("Temp/BlinkCustomers.error.txt",e.ToString());Debug.LogException(e);}
    }
}
