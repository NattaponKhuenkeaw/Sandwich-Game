using UnityEditor;

// A runtime UI object can disappear while the Inspector still has its preview cached.
// Rebuild stale editors before the next Inspector repaint (Editor only).
[InitializeOnLoad]
internal static class DestroyedInspectorPreviewGuard
{
    private static bool queued;

    static DestroyedInspectorPreviewGuard()
    {
        EditorApplication.hierarchyChanged += QueueRepair;
        EditorApplication.playModeStateChanged += _ => QueueRepair();
        QueueRepair();
    }

    private static void QueueRepair()
    {
        if (queued) return;
        queued = true;
        EditorApplication.delayCall += Repair;
    }

    private static void Repair()
    {
        queued = false;
        var tracker = ActiveEditorTracker.sharedTracker;
        foreach (var editor in tracker.activeEditors)
        {
            if (editor == null || editor.target == null)
            {
                tracker.ForceRebuild();
                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                return;
            }
        }
    }
}
