using UnityEngine;

namespace SandwichGame
{
    public static class RuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<SandwichGameController>() != null)
                return;

            var go = new GameObject("SandwichGame");
            go.AddComponent<SandwichGameController>();
        }
    }
}
