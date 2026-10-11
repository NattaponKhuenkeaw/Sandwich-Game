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
        public GameObject[] MeatVisuals;

        public void RememberHome()
        {
            RestParent = transform.parent;
            RestPosition = transform.localPosition;
            RestRotation = transform.localRotation;
        }

        public void ApplyKindVisual()
        {
            if (MeatVisuals != null && MeatVisuals.Length == 3 && GameRules.IsMeat(Kind))
            {
                for (int i = 0; i < MeatVisuals.Length; i++)
                    if (MeatVisuals[i] != null) MeatVisuals[i].SetActive(i == (int)Kind);
                return;
            }
            if (ColorRenderers == null) return;
            ApplyColor(GameRules.ColorFor(Kind));
        }

        public void ApplyColor(Color color)
        {
            if (MeatVisuals != null && MeatVisuals.Length == 3 && GameRules.IsMeat(Kind)) return;
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
