using UnityEngine;

namespace SandwichGame
{
    public class KitchenScene : MonoBehaviour
    {
        public Camera Camera;
        public Transform Root;
        public Transform DragPlane;
        public StationTarget MeatPile;
        public StationTarget BunPile;
        public StationTarget Trash;
        public StationTarget Plate;
        public StationTarget[] GrillSlots = new StationTarget[GameConstants.GrillSlotCount];
        public Transform[] GrillAnchors = new Transform[GameConstants.GrillSlotCount];
        public Transform PlateAnchor;
        public CustomerAgent[] Customers = new CustomerAgent[GameConstants.CustomerCount];
        public Light GrillLight;
        public ItemView RawMeatPrefab;
        public ItemView CookedMeatPrefab;
        public ItemView BurnedMeatPrefab;
        public ItemView BunPrefab;
        public ItemView SandwichPrefab;

        [System.NonSerialized] public ItemView[] GrillItems;
        [System.NonSerialized] public ItemView PlateItem;

        void Awake()
        {
            PrepareRuntimeSlots();
        }

        public void PrepareRuntimeSlots()
        {
            var slots = new System.Collections.Generic.List<StationTarget>();
            if (GrillSlots != null)
                foreach (var slot in GrillSlots)
                    if (slot != null && slot.Kind == StationKind.Grill && !slots.Contains(slot)) slots.Add(slot);
            foreach (var slot in GetComponentsInChildren<StationTarget>(true))
                if (slot.Kind == StationKind.Grill && !slots.Contains(slot)) slots.Add(slot);
            var anchors = new Transform[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                anchors[i] = GrillAnchors != null && i < GrillAnchors.Length && GrillAnchors[i] != null
                    ? GrillAnchors[i] : slots[i].transform;
                slots[i].SlotIndex = i;
            }
            GrillSlots = slots.ToArray();
            GrillAnchors = anchors;
            if (GrillItems == null || GrillItems.Length != slots.Count)
                System.Array.Resize(ref GrillItems, slots.Count);
            if (Root == null) Root = transform;
            if (Plate != null && (PlateAnchor == null || !PlateAnchor.IsChildOf(Plate.transform)))
            {
                PlateAnchor = Plate.transform.Find("Anchor");
                if (PlateAnchor == null)
                {
                    PlateAnchor = new GameObject("Anchor").transform;
                    PlateAnchor.SetParent(Plate.transform, false);
                    PlateAnchor.localPosition = new Vector3(0f, 0.06f, 0f);
                }
            }

        }
    }
}
