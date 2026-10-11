using UnityEngine;
using UnityEngine.SceneManagement;

namespace SandwichGame
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (SceneManager.GetActiveScene().name != "SampleScene") return;
            if (Object.FindFirstObjectByType<SandwichGameController>() != null)
                return;

            var go = new GameObject("SandwichGame");
            go.AddComponent<SandwichGameController>();
        }
    }
}
