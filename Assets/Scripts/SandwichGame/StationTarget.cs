using UnityEngine;

namespace SandwichGame
{
    public class StationTarget : MonoBehaviour
    {
        public StationKind Kind;
        public int SlotIndex;
        public Renderer HighlightRenderer;
        public Color BaseColor = Color.white;
        Material cachedMat;

        public void SetHighlight(bool on)
        {
            if (HighlightRenderer == null) return;
            if (cachedMat == null) cachedMat = HighlightRenderer.material;
            var c = on ? Color.Lerp(BaseColor, Color.white, 0.35f) : BaseColor;
            cachedMat.SetColor("_BaseColor", c);
            if (cachedMat.HasProperty("_Color")) cachedMat.SetColor("_Color", c);
        }
    }
}
