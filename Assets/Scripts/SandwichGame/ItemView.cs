using UnityEngine;

namespace SandwichGame
{
    public class ItemView : MonoBehaviour
    {
        public ItemKind Kind;
        public float GrillTime;
        public int GrillSlot = -1;
        public bool Held;
        public StationKind HomeKind = StationKind.MeatPile;
        public int HomeSlot = -1;
        public Vector3 RestPosition;
        public Quaternion RestRotation;
        public Transform RestParent;
        public Renderer[] ColorRenderers;

        public void RememberHome()
        {
            RestParent = transform.parent;
            RestPosition = transform.localPosition;
            RestRotation = transform.localRotation;
        }

        public void ApplyKindVisual()
        {
            if (ColorRenderers == null) return;
            var color = GameRules.ColorFor(Kind);
            ApplyColor(color);
        }

        public void ApplyColor(Color color)
        {
            if (ColorRenderers == null) return;
            for (int i = 0; i < ColorRenderers.Length; i++)
            {
                var r = ColorRenderers[i];
                if (r == null) continue;
                var mat = r.material;
                mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            }
        }
    }
}
