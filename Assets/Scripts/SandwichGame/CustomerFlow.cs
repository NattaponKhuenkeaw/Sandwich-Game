using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SandwichGame
{
    public class CustomerFlow : MonoBehaviour
    {
        public Transform Exit;
        public Transform EntryLane;
        public Transform DepartureLane;
        public Transform QueueStart;
        public Transform DespawnPoint;
        [Min(1)] public int QueueSize = 3;
        [Min(.5f)] public float QueueSpacing = 1.35f;
        [Min(.1f)] public float MoveSpeed = 1.7f;
        public Vector3 QueueDirection = Vector3.right;
        public int WaitingCount => queue.Count;
        public int VisibleCount => actors.Count;

        KitchenScene kitchen;
        CustomerAgent[] originals;
        Vector3[] seats;
        Quaternion[] seatRotations;
        readonly List<CustomerAgent> queue = new List<CustomerAgent>();
        readonly List<CustomerAgent> actors = new List<CustomerAgent>();
        readonly Dictionary<CustomerAgent, bool> walking = new Dictionary<CustomerAgent, bool>();

        public void Begin(KitchenScene scene)
        {
            StopAllCoroutines();
            foreach (var actor in actors) if (actor != null) { actor.gameObject.SetActive(false); Destroy(actor.gameObject); }
            actors.Clear(); queue.Clear(); walking.Clear();
            kitchen = scene;
            if (originals == null)
            {
                originals = (CustomerAgent[])scene.Customers.Clone();
                seats = new Vector3[originals.Length];
                seatRotations = new Quaternion[originals.Length];
                for (int i = 0; i < originals.Length; i++)
                { seats[i] = originals[i].transform.position; seatRotations[i] = originals[i].transform.rotation; }
            }
            scene.Customers = (CustomerAgent[])originals.Clone();
            foreach (var original in originals) { original.Busy = true; original.gameObject.SetActive(false); }
            for (int i = 0; i < QueueSize; i++) AddWaiting(true);
            StartCoroutine(FillInitialSeats());
        }

        IEnumerator FillInitialSeats()
        {
            for (int i = 0; i < seats.Length; i++)
            {
                StartCoroutine(Enter(TakeNext(i), i));
                yield return new WaitForSeconds(.8f);
            }
        }

        Vector3 QueuePosition(int index)
        {
            Vector3 direction = QueueDirection; direction.y = 0f;
            if (direction.sqrMagnitude < .01f) direction = Vector3.right;
            var position = (QueueStart != null ? QueueStart.position : Exit.position) + direction.normalized * QueueSpacing * index;
            position.y = seats[0].y;
            return position;
        }

        void AddWaiting(bool initial)
        {
            var customer = Instantiate(originals[0], kitchen.Root);
            customer.name = "QueuedCustomer";
            customer.Busy = true;
            customer.SlotIndex = -1;
            customer.transform.position = QueuePosition(queue.Count + (initial ? 0 : 1));
            customer.transform.rotation = seatRotations[0];
            HideOrder(customer);
            foreach (var collider in customer.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var target = customer.GetComponent<StationTarget>();
            if (target != null) { target.SlotIndex = -1; target.SetHighlight(false); }
            customer.gameObject.SetActive(true);
            customer.RepairBubble();
            HideOrder(customer);
            var visual = customer.GetComponentInChildren<BlinkCustomerVisual>(true);
            if (visual != null)
            {
                var others = new List<BlinkCustomerVisual>();
                foreach (var actor in actors) if (actor != null) others.Add(actor.GetComponentInChildren<BlinkCustomerVisual>(true));
                visual.ChooseAppearance(others.ToArray());
            }
            actors.Add(customer); queue.Add(customer);
            SetWalking(customer, false);
        }

        CustomerAgent TakeNext(int index)
        {
            if (queue.Count == 0) AddWaiting(true);
            var customer = queue[0]; queue.RemoveAt(0);
            customer.name = "Customer" + index;
            customer.SlotIndex = index;
            var target = customer.GetComponent<StationTarget>();
            if (target != null) target.SlotIndex = index;
            kitchen.Customers[index] = customer;
            AddWaiting(false);
            return customer;
        }

        void Update()
        {
            for (int i = 0; i < queue.Count; i++)
            {
                var customer = queue[i];
                Vector3 destination = QueuePosition(i);
                bool moving = (destination - customer.transform.position).sqrMagnitude > .0025f;
                SetWalking(customer, moving);
                if (moving) Step(customer, destination);
            }
        }

        static void HideOrder(CustomerAgent customer)
        {
            foreach (var canvas in customer.GetComponentsInChildren<Canvas>(true))
                canvas.gameObject.SetActive(false);
        }

        void SetWalking(CustomerAgent customer, bool moving)
        {
            if (walking.TryGetValue(customer, out bool current) && current == moving) return;
            walking[customer] = moving;
            var visual = customer.GetComponentInChildren<BlinkCustomerVisual>(true);
            if (visual != null) visual.SetMoving(moving);
        }

        void Step(CustomerAgent customer, Vector3 destination)
        {
            Vector3 direction = destination - customer.transform.position; direction.y = 0f;
            if (direction.sqrMagnitude > .0001f)
                customer.transform.rotation = Quaternion.RotateTowards(customer.transform.rotation, Quaternion.LookRotation(-direction), 540f * Time.deltaTime);
            customer.transform.position = Vector3.MoveTowards(customer.transform.position, destination, MoveSpeed * Time.deltaTime);
        }

        IEnumerator MoveTo(CustomerAgent customer, Vector3 destination)
        {
            destination.y = seats[0].y;
            SetWalking(customer, true);
            while (customer != null && (customer.transform.position - destination).sqrMagnitude > .0025f)
            { Step(customer, destination); yield return null; }
            if (customer != null) customer.transform.position = destination;
        }

        IEnumerator Enter(CustomerAgent customer, int index)
        {
            if (EntryLane != null) yield return MoveTo(customer, EntryLane.position);
            yield return MoveTo(customer, seats[index]);
            SetWalking(customer, false);
            customer.transform.rotation = seatRotations[index];
            foreach (var collider in customer.GetComponentsInChildren<Collider>(true)) collider.enabled = true;
            if (customer.Bubble != null) customer.Bubble.gameObject.SetActive(true);
            customer.SetOrder(GameRules.RandomOrderSize(), GameConstants.CustomerWait);
            customer.Busy = false;
        }

        public IEnumerator Replace(CustomerAgent customer)
        {
            int index = System.Array.IndexOf(kitchen.Customers, customer);
            if (index < 0) yield break;
            customer.Busy = true;
            HideOrder(customer);
            foreach (var collider in customer.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            StartCoroutine(Leave(customer));
            yield return Enter(TakeNext(index), index);
        }

        IEnumerator Leave(CustomerAgent customer)
        {
            yield return MoveTo(customer, DepartureLane != null ? DepartureLane.position : Exit.position);
            var end = DespawnPoint != null ? DespawnPoint.position : Exit.position - (QueueDirection.sqrMagnitude > .01f ? new Vector3(QueueDirection.x, 0f, QueueDirection.z).normalized : Vector3.right) * 10f;
            yield return MoveTo(customer, end);
            actors.Remove(customer); walking.Remove(customer);
            Destroy(customer.gameObject);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Draw(QueueStart, EntryLane);
            var scene = GetComponent<KitchenScene>();
            if (scene != null)
                foreach (var customer in scene.Customers)
                    if (customer != null) Draw(EntryLane, customer.transform);
            Gizmos.color = Color.cyan;
            if (scene != null)
                foreach (var customer in scene.Customers)
                    if (customer != null) Draw(customer.transform, DepartureLane);
            Draw(DepartureLane, DespawnPoint);
        }
        static void Draw(Transform a, Transform b)
        { if (a != null && b != null) { Gizmos.DrawLine(a.position,b.position); Gizmos.DrawWireSphere(b.position,.2f); } }
    }
}
