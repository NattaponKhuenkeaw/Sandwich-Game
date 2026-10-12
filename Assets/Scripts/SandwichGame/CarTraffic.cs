using System.Collections.Generic;
using UnityEngine;

namespace SandwichGame
{
    public class CarTraffic : MonoBehaviour
    {
        public GameObject[] CarPrefabs;
        public Transform RoadEnd;
        public float LaneSpacing = 2.7f;
        public Vector2 SpeedRange = new Vector2(4f, 6f);
        public Vector2 SpawnInterval = new Vector2(4f, 7f);
        public int MaxCars = 4;
        class Trip { public GameObject Car; public Vector3 Start, End; public float Speed; }
        readonly List<Trip> trips = new List<Trip>();
        readonly Dictionary<GameObject, Stack<GameObject>> pool = new Dictionary<GameObject, Stack<GameObject>>();
        readonly Dictionary<GameObject, GameObject> sources = new Dictionary<GameObject, GameObject>();
        SandwichGameController controller;
        float nextSpawn;
        int previousType = -1;
        bool reverse;
        void Start() { controller = FindFirstObjectByType<SandwichGameController>(); nextSpawn = 1f; }
        void Update()
        {
            if (controller == null || !controller.IsGameplayActive || Time.deltaTime <= 0f || RoadEnd == null) return;
            for (int i = trips.Count - 1; i >= 0; i--)
            {
                var trip = trips[i];
                trip.Car.transform.position = Vector3.MoveTowards(trip.Car.transform.position, trip.End, trip.Speed * Time.deltaTime);
                if ((trip.Car.transform.position - trip.End).sqrMagnitude < .01f)
                {
                    trip.Car.SetActive(false);
                    pool[sources[trip.Car]].Push(trip.Car);
                    trips.RemoveAt(i);
                }
            }
            nextSpawn -= Time.deltaTime;
            if (nextSpawn <= 0f && trips.Count < MaxCars)
            {
                SpawnCar(); nextSpawn = Random.Range(SpawnInterval.x, SpawnInterval.y);
            }
        }
        void SpawnCar()
        {
            if (CarPrefabs == null || CarPrefabs.Length == 0) return;
            int type = Random.Range(0, CarPrefabs.Length);
            if (CarPrefabs.Length > 1 && type == previousType) type = (type + Random.Range(1, CarPrefabs.Length)) % CarPrefabs.Length;
            previousType = type;
            var prefab = CarPrefabs[type]; if (prefab == null) return;
            if (!pool.TryGetValue(prefab, out var available)) { available = new Stack<GameObject>(); pool.Add(prefab, available); }
            var car = available.Count > 0 ? available.Pop() : Instantiate(prefab, transform);
            sources[car] = prefab;
            Vector3 direction = RoadEnd.position - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude < .01f) { car.SetActive(false); available.Push(car); return; }
            Vector3 offset = Vector3.Cross(direction.normalized, Vector3.up) * (reverse ? LaneSpacing : 0f);
            Vector3 start = (reverse ? RoadEnd.position : transform.position) + offset;
            Vector3 end = (reverse ? transform.position : RoadEnd.position) + offset;
            car.transform.SetPositionAndRotation(start, Quaternion.LookRotation(end - start, Vector3.up));
            car.SetActive(true);
            trips.Add(new Trip { Car = car, Start = start, End = end, Speed = Random.Range(SpeedRange.x, SpeedRange.y) });
            reverse = !reverse;
        }
        void OnDrawGizmosSelected()
        {
            if (RoadEnd == null) return;
            Gizmos.color = Color.cyan; Gizmos.DrawLine(transform.position, RoadEnd.position);
            Vector3 direction = RoadEnd.position - transform.position;
            Vector3 offset = Vector3.Cross(direction.normalized, Vector3.up) * LaneSpacing;
            Gizmos.color = Color.yellow; Gizmos.DrawLine(transform.position + offset, RoadEnd.position + offset);
        }
    }
}
