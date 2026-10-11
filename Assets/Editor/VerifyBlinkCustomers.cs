using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SandwichGame;
[InitializeOnLoad]
public static class VerifyBlinkCustomers
{
    static VerifyBlinkCustomers(){EditorApplication.delayCall+=Verify;}
    static void Verify()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || File.Exists("Temp/BlinkCustomersVerified.complete"))return;
        if(!File.Exists("Assets/SandwichGame/Customers/Prefabs/BlinkCustomerVisual.prefab"))return;
        var scene=EditorSceneManager.NewPreviewScene();
        try
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SandwichGame/Customers/Prefabs/BlinkCustomerVisual.prefab");
            var tests=new BlinkCustomerVisual[3];
            for(int i=0;i<3;i++)
            {
                var obj=UnityEngine.Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(obj,scene);
                tests[i]=obj.GetComponent<BlinkCustomerVisual>();tests[i].ReactionAnimator=null;
            }
            for(int pass=0;pass<100;pass++)
            {
                foreach(var test in tests)
                {
                    int previous=test.VariantIndex;
                    test.ChooseAppearance(tests);
                    if(test.VariantIndex==previous)throw new Exception("Appearance repeated");
                    if(test.Variants.Count(v=>v.activeSelf)!=1)throw new Exception("Multiple active variants");
                }
                if(tests.Select(t=>t.VariantIndex).Distinct().Count()!=3)throw new Exception("Customers have duplicate appearances");
            }
            var root=tests[0].gameObject;
            foreach(var clipName in new[]{"Received","NotReceived"})
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/SandwichGame/Customers/Animations/"+clipName+".anim");
                clip.SampleAnimation(root,.2f);
                if(clipName=="Received"&&root.transform.localPosition.y<.05f)throw new Exception("Happy reaction missing");
                if(clipName=="NotReceived"&&Mathf.Abs(Mathf.DeltaAngle(0,root.transform.localEulerAngles.z))<2)throw new Exception("Unhappy reaction missing");
                clip.SampleAnimation(root,clip.length);
            }
            var idleController=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/SandwichGame/Customers/Animations/CustomerIdle.controller");
            var idle=(AnimationClip)idleController.layers[0].stateMachine.defaultState.motion;
            if(!idle.isHumanMotion)throw new Exception("Idle is not humanoid");
            File.WriteAllText("Temp/BlinkCustomersVerified.complete","100 randomization cycles passed: 3 unique appearances; previous appearance excluded. Happy/unhappy motion sampled successfully. Humanoid idle: "+idle.name);
        }
        catch(Exception e){File.WriteAllText("Temp/BlinkCustomersVerified.error.txt",e.ToString());Debug.LogException(e);}
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
}
