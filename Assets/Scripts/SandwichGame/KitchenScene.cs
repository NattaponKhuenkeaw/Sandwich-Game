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
            if (GrillItems == null || GrillItems.Length != GameConstants.GrillSlotCount)
                GrillItems = new ItemView[GameConstants.GrillSlotCount];
            if (Root == null) Root = transform;
        }
    }
}
