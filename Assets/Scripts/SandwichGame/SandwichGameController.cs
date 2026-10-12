using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SandwichGame
{
    public class SandwichGameController : MonoBehaviour
    {
        [SerializeField] KitchenScene kitchen;
        [SerializeField] HudView hud;
        [SerializeField] GameAudio gameAudio;
        [SerializeField] GameObject pointerEffectPrefab;
        bool pointerEffectPressed;

        GamePhase phase = GamePhase.Title;
        ItemView held;
        StationTarget hover;
        int score;
        int target = GameConstants.StartingTarget;
        int lives = GameConstants.StartingLives;
        int round = 1;
        float roundLeft = GameConstants.RoundDuration;
        float roundEndDelay;
        Vector3[] customerStartPositions;
        CustomerFlow customerFlow;
        LevelCompleteView levelCompleteView;
        GameOverView gameOverView;
        PauseMenu pauseMenu;
        public bool CanPause => phase == GamePhase.Playing || phase == GamePhase.RoundEnd;
        public bool IsGameplayActive => phase == GamePhase.Playing;

        void Start()
        {
            Application.targetFrameRate = 60;
            BindScene();
            AudioSettings.Initialize();
            BeginGame();
            pauseMenu = Instantiate(Resources.Load<PauseMenu>("UI/PauseCanvas"), transform);
        }

        void BindScene()
        {
            if (kitchen == null)
                kitchen = GetComponentInChildren<KitchenScene>(true);
            if (kitchen == null)
            {
                var root = new GameObject("Kitchen");
                root.transform.SetParent(transform, false);
                kitchen = root.AddComponent<KitchenScene>();
                KitchenFactory.Build(kitchen);
            }

            if (pointerEffectPrefab == null)
                pointerEffectPrefab = Resources.Load<GameObject>("VFX/Click Dust");

            kitchen.PrepareRuntimeSlots();
            customerFlow = kitchen.GetComponent<CustomerFlow>();
            customerStartPositions = new Vector3[kitchen.Customers.Length];
            for (int i = 0; i < kitchen.Customers.Length; i++)
                if (kitchen.Customers[i] != null)
                    customerStartPositions[i] = kitchen.Customers[i].transform.localPosition;

            if (hud == null)
                hud = GetComponentInChildren<HudView>(true);
            if (hud == null)
            {
                hud = HudView.Create();
                hud.transform.SetParent(transform, false);
            }
            else
            {
                hud.EnsureBuilt();
            }

            if (gameAudio == null)
                gameAudio = GetComponentInChildren<GameAudio>(true);
            if (gameAudio == null)
                gameAudio = GameAudio.Create(transform);
        }

        void Update()
        {
            if (pauseMenu != null && pauseMenu.IsPaused) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            HandleLevelTestShortcut();
#endif
            hud.Refresh(round, phase == GamePhase.Playing ? roundLeft : 0f, score, target, lives);
            UpdateHover();

            if (phase == GamePhase.Playing)
            {
                TickRound();
                if (phase == GamePhase.Playing)
                {
                    TickCooking();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    HandleCustomerTestShortcut();
#endif
                    TickCustomers();
                    HandleDrag();
                }
            }
            else if (phase == GamePhase.RoundEnd)
            {
                HandleDragVisualOnly();
                roundEndDelay -= Time.deltaTime;
                if (roundEndDelay <= 0f) FinishRoundPause();
            }
            else
            {
                HandleDragVisualOnly();
            }

            bool fryingRawMeat = false;
            bool fryingCookedMeat = false;
            bool fryingBurnedMeat = false;
            if (phase == GamePhase.Playing)
                foreach (var item in kitchen.GrillItems)
                {
                    if (item == null || item.Held) continue;
                    if (item.Kind == ItemKind.RawMeat) fryingRawMeat = true;
                    if (item.Kind == ItemKind.CookedMeat) fryingCookedMeat = true;
                    if (item.Kind == ItemKind.BurnedMeat) fryingBurnedMeat = true;
                }
            gameAudio.SetRawMeatFrying(fryingRawMeat);
            gameAudio.SetCookedMeatFrying(fryingCookedMeat);
            gameAudio.SetBurnedMeatFrying(fryingBurnedMeat);
            FaceBubblesToCamera();
        }

        void BeginGame()
        {
            StopAllCoroutines();
            if (gameOverView != null) gameOverView.Hide();
            if (levelCompleteView != null) levelCompleteView.Hide();
            if (customerFlow != null) customerFlow.gameObject.SetActive(true);
            score = 0;
            target = GameConstants.StartingTarget;
            lives = GameConstants.StartingLives;
            round = 1;
            StartRound();
            hud.HideOverlay();
        }

        void StartRound()
        {
            // Cancel pending reactions/replacements before discarding the previous round's customers.
            StopAllCoroutines();
            pointerEffectPressed = false;
            hover = null;
            phase = GamePhase.Playing;
            roundLeft = GameConstants.RoundDuration;
            ClearLooseItems();
            if (customerFlow != null && customerFlow.Exit != null)
                customerFlow.Begin(kitchen);
            else
                for (int i = 0; i < kitchen.Customers.Length; i++)
                    ResetCustomer(kitchen.Customers[i], true);
        }

        void TickRound()
        {
            roundLeft -= Time.deltaTime;
            if (roundLeft > 0f) return;

            roundLeft = 0f;
            phase = GamePhase.RoundEnd;
            roundEndDelay = 1.6f;
            if (GameRules.MeetsTarget(score, target))
            {
                gameAudio.RoundWin();
                hud.ShowBanner("ผ่านรอบ! เป้า +500", 1.6f);
            }
            else
            {
                lives--;
                gameAudio.Fail();
                hud.ShowBanner(lives <= 0 ? "ชีวิตหมด" : "เป้าไม่ถึง  -1 ชีวิต", 1.6f);
            }
        }

        void FinishRoundPause()
        {
            if (GameRules.MeetsTarget(score, target))
            {
                StopAllCoroutines();
                phase = GamePhase.LevelComplete;
                if (customerFlow != null) customerFlow.gameObject.SetActive(false);
                if (levelCompleteView == null)
                {
                    var prefab = Resources.Load<LevelCompleteView>("UI/LevelCompleteCanvas");
                    if (prefab == null) throw new System.InvalidOperationException("LevelCompleteCanvas prefab missing");
                    levelCompleteView = Instantiate(prefab, transform);
                }
                levelCompleteView.Show(round, score, target, GameRules.NextTarget(target), AdvanceLevel);
                return;
            }

            if (lives <= 0)
            {
                EnterGameOver();
                return;
            }

            StartRound();
        }

        void EnterGameOver()
        {
            StopAllCoroutines();
            phase = GamePhase.GameOver;
            if (customerFlow != null) customerFlow.gameObject.SetActive(false);
            if (gameOverView == null)
            {
                var prefab = Resources.Load<GameOverView>("UI/GameOverCanvas");
                if (prefab == null) throw new System.InvalidOperationException("GameOverCanvas prefab missing");
                gameOverView = Instantiate(prefab, transform);
            }
            hud.HideOverlay();
            gameOverView.Show(round, score, target, BeginGame);
        }

        void AdvanceLevel()
        {
            if (phase != GamePhase.LevelComplete) return;
            levelCompleteView.Hide();
            if (customerFlow != null) customerFlow.gameObject.SetActive(true);
            round++;
            target = GameRules.NextTarget(target);
            StartRound();
        }

        void TickCooking()
        {
            bool anyCooking = false;
            for (int i = 0; i < kitchen.GrillItems.Length; i++)
            {
                var item = kitchen.GrillItems[i];
                if (item == null || item.Held) continue;
                if (!GameRules.CanPlaceOnGrill(item.Kind) && item.Kind != ItemKind.BurnedMeat) continue;

                anyCooking = true;
                item.GrillTime += Time.deltaTime;
                if (item.Kind == ItemKind.RawMeat)
                {
                    float t = Mathf.Clamp01(item.GrillTime / GameConstants.GrillCookSeconds);
                    item.ApplyColor(Color.Lerp(GameRules.ColorFor(ItemKind.RawMeat), GameRules.ColorFor(ItemKind.CookedMeat), t));
                    if (item.GrillTime >= GameConstants.GrillCookSeconds)
                    {
                        item.Kind = ItemKind.CookedMeat;
                        item.GrillTime = 0f;
                        item.ApplyKindVisual();
                    }
                }
                else if (item.Kind == ItemKind.CookedMeat)
                {
                    float t = Mathf.Clamp01(item.GrillTime / GameConstants.GrillBurnSeconds);
                    item.ApplyColor(Color.Lerp(GameRules.ColorFor(ItemKind.CookedMeat), GameRules.ColorFor(ItemKind.BurnedMeat), t));
                    if (item.GrillTime >= GameConstants.GrillBurnSeconds)
                    {
                        item.Kind = ItemKind.BurnedMeat;
                        item.GrillTime = 0f;
                        item.ApplyKindVisual();
                        hud.ShowBanner("เนื้อไหม้!", 0.8f);
                    }
                }
            }

            if (kitchen.GrillLight != null)
                kitchen.GrillLight.intensity = anyCooking ? 2.3f : 1.2f;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void HandleLevelTestShortcut()
        {
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && phase == GamePhase.Playing)
            {
                lives = 0;
                roundLeft = 0f;
                gameAudio.Fail();
                EnterGameOver();
                return;
            }
            if (Keyboard.current == null || !Keyboard.current.nKey.wasPressedThisFrame) return;
            if (phase == GamePhase.LevelComplete)
            {
                AdvanceLevel();
            }
            else if (phase == GamePhase.Playing)
            {
                // Complete this round immediately so the results screen can be tested.
                score = Mathf.Max(score, target);
                roundLeft = 0f;
                gameAudio.RoundWin();
                FinishRoundPause();
            }
        }

        void HandleCustomerTestShortcut()
        {
            if (phase != GamePhase.Playing || Keyboard.current == null || !Keyboard.current.pKey.wasPressedThisFrame)
                return;
            foreach (var customer in kitchen.Customers)
            {
                if (customer == null || customer.Busy || !customer.gameObject.activeInHierarchy)
                    continue;
                customer.WaitLeft = 0f;
                break;
            }
        }
#endif

        void TickCustomers()
        {
            for (int i = 0; i < kitchen.Customers.Length; i++)
            {
                var c = kitchen.Customers[i];
                if (c.Busy) continue;
                c.WaitLeft -= Time.deltaTime;
                c.RefreshBubble();
                if (!c.TimeLowVoicePlayed && c.WaitLeft > 0f && c.WaitLeft <= GameConstants.CustomerWait * 0.2f)
                {
                    c.TimeLowVoicePlayed = true;
                    gameAudio.CustomerTimeLow();
                }
                if (c.WaitLeft > 0f) continue;
                gameAudio.CustomerNoFood();
                hud.ShowBanner("ลูกค้าหมดเวลา", 0.8f);
                StartCoroutine(ReplaceCustomer(c, false));
            }
        }

        void HandleDrag()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            if (mouse.leftButton.wasPressedThisFrame && !PointerOnUi())
            {
                pointerEffectPressed = true;
                PlayPointerEffect();
                TryPickup();
            }

            if (held != null)
                MoveHeld();

            if (mouse.leftButton.wasReleasedThisFrame)
            {
                if (pointerEffectPressed) PlayPointerEffect();
                pointerEffectPressed = false;
                if (held != null) DropHeld();
            }
        }

        void HandleDragVisualOnly()
        {
            pointerEffectPressed = false;
            if (held == null) return;
            MoveHeld();
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                ReturnHeld();
        }

        void PlayPointerEffect()
        {
            if (pointerEffectPrefab == null || kitchen == null) return;
            float height = kitchen.DragPlane != null ? kitchen.DragPlane.position.y : 1.16f;
            var plane = new Plane(Vector3.up, new Vector3(0f, height + 0.14f, 0f));
            var ray = ScreenRay();
            if (!plane.Raycast(ray, out float distance) || distance < 0f) return;
            var effect = Instantiate(pointerEffectPrefab, ray.GetPoint(distance), pointerEffectPrefab.transform.rotation);
            // Safety cleanup also covers any child particles left running by the effect.
            Destroy(effect, 5f);
        }

        void TryPickup()
        {
            bool hit = RayHit(out var item, out var station, out _);
            // Keep direct food and other stations precise; allow forgiving clicks around grilled meat.
            if (item == null && (station == null || station.Kind == StationKind.Grill))
            {
                var nearbyMeat = NearestGrillItem();
                if (nearbyMeat != null)
                {
                    Grab(nearbyMeat);
                    return;
                }
            }
            if (hit)
            {
                if (item != null)
                {
                    Grab(item);
                    return;
                }

                if (station == null) return;
                if (station.Kind == StationKind.MeatPile)
                {
                    Grab(KitchenFactory.SpawnItem(ItemKind.RawMeat, station.transform.position + Vector3.up * 0.18f, kitchen));
                    return;
                }

                if (station.Kind == StationKind.BunPile)
                {
                    Grab(KitchenFactory.SpawnItem(ItemKind.Bun, station.transform.position + Vector3.up * 0.18f, kitchen));
                    return;
                }

                if (station.Kind == StationKind.Plate && kitchen.PlateItem != null)
                    Grab(kitchen.PlateItem);
                else if (station.Kind == StationKind.Grill && station.SlotIndex >= 0 && station.SlotIndex < kitchen.GrillItems.Length && kitchen.GrillItems[station.SlotIndex] != null)
                    Grab(kitchen.GrillItems[station.SlotIndex]);
            }
        }

        void Grab(ItemView item)
        {
            held = item;
            item.Held = true;
            item.RememberHome();
            Unslot(item);
            item.transform.SetParent(kitchen.Root, true);
            gameAudio.Pickup();
        }

        void MoveHeld()
        {
            var ray = ScreenRay();
            float dragHeight = kitchen.DragPlane != null ? kitchen.DragPlane.position.y : 1.16f;
            // Intersect at the lifted height so the item stays on the cursor's ray.
            var plane = new Plane(Vector3.up, new Vector3(0f, dragHeight + 0.14f, 0f));
            if (plane.Raycast(ray, out float enter))
                held.transform.position = ray.GetPoint(enter);
        }

        void DropHeld()
        {
            RayHit(out _, out var station, out _);
            if (station == null)
                station = NearestStation();

            bool placed = false;
            if (station != null)
            {
                switch (station.Kind)
                {
                    case StationKind.Grill: placed = PlaceOnGrill(station.SlotIndex); break;
                    case StationKind.Plate: placed = PlaceOnPlate(); break;
                    case StationKind.Trash: placed = TrashHeld(); break;
                    case StationKind.Customer: placed = Serve(station.SlotIndex); break;
                }
            }

            if (!placed) ReturnHeld();
            else if (station == null || station.Kind != StationKind.Customer)
                gameAudio.Drop();
        }

        bool PlaceOnGrill(int slot)
        {
            if (slot < 0 || slot >= kitchen.GrillItems.Length || slot >= kitchen.GrillAnchors.Length || kitchen.GrillAnchors[slot] == null)
                return false;
            if (!GameRules.CanPlaceOnGrill(held.Kind) && held.Kind != ItemKind.BurnedMeat)
                return false;
            if (kitchen.GrillItems[slot] != null) return false;

            kitchen.GrillItems[slot] = held;
            held.GrillSlot = slot;
            held.HomeKind = StationKind.Grill;
            held.HomeSlot = slot;
            Snap(held.transform, kitchen.GrillAnchors[slot], Vector3.up * 0.05f);
            held.Held = false;
            held = null;
            return true;
        }

        bool PlaceOnPlate()
        {
            kitchen.PrepareRuntimeSlots();
            if (kitchen.PlateAnchor == null) return false;
            if (kitchen.PlateItem == null)
            {
                if (held.Kind != ItemKind.Bun && held.Kind != ItemKind.CookedMeat && held.Kind != ItemKind.Sandwich)
                    return false;
                kitchen.PlateItem = held;
                held.HomeKind = StationKind.Plate;
                Snap(held.transform, kitchen.PlateAnchor, Vector3.zero);
                held.Held = false;
                held = null;
                return true;
            }

            if (!GameRules.CanCombineOnPlate(kitchen.PlateItem.Kind, held.Kind))
                return false;

            DestroyItem(kitchen.PlateItem);
            DestroyItem(held);
            held = null;
            var sandwich = KitchenFactory.SpawnItem(ItemKind.Sandwich, kitchen.PlateAnchor.position, kitchen);
            kitchen.PlateItem = sandwich;
            sandwich.HomeKind = StationKind.Plate;
            Snap(sandwich.transform, kitchen.PlateAnchor, Vector3.zero);
            gameAudio.Ding();
            return true;
        }

        bool TrashHeld()
        {
            DestroyItem(held);
            held = null;
            return true;
        }

        bool Serve(int customerIndex)
        {
            var customer = kitchen.Customers[customerIndex];
            if (customer.Busy || customer.Remaining <= 0) return false;
            if (!GameRules.CanServe(held.Kind))
            {
                hud.ShowBanner("ลูกค้ารับแค่แฮมเบอร์เกอร์", 0.9f);
                return false;
            }

            DestroyItem(held);
            held = null;
            customer.Received++;
            if (customer.Wanted == 2 && customer.Received == 1)
                customer.WaitLeft = Mathf.Min(GameConstants.CustomerWait,
                    customer.WaitLeft + GameConstants.CustomerWait * GameConstants.PartialOrderWaitBonus);
            customer.RefreshBubble();
            if (customer.Remaining > 0)
            {
                hud.ShowBanner("อีก " + customer.Remaining + " ชิ้น", 0.7f);
                return true;
            }

            score += GameConstants.ScorePerOrder;
            gameAudio.Serve();
            hud.FloatScore("+" + GameConstants.ScorePerOrder);
            StartCoroutine(ReplaceCustomer(customer, true));
            return true;
        }

        void ReturnHeld()
        {
            if (held == null) return;
            if (held.HomeKind == StationKind.Grill && held.HomeSlot >= 0 && held.HomeSlot < kitchen.GrillItems.Length && kitchen.GrillItems[held.HomeSlot] == null)
            {
                var item = held;
                held = null;
                item.Held = false;
                kitchen.GrillItems[item.HomeSlot] = item;
                item.GrillSlot = item.HomeSlot;
                Snap(item.transform, kitchen.GrillAnchors[item.HomeSlot], Vector3.up * 0.05f);
                return;
            }

            if (held.HomeKind == StationKind.Plate && kitchen.PlateItem == null)
            {
                var item = held;
                held = null;
                item.Held = false;
                kitchen.PlateItem = item;
                Snap(item.transform, kitchen.PlateAnchor, Vector3.zero);
                return;
            }

            DestroyItem(held);
            held = null;
        }

        IEnumerator ReplaceCustomer(CustomerAgent customer, bool happy)
        {
            customer.Busy = true;
            var visual = customer.GetComponentInChildren<BlinkCustomerVisual>(true);
            if (visual != null)
            {
                visual.React(happy);
                yield return new WaitForSeconds(visual.ReactionDuration);
            }
            if (customerFlow != null && customerFlow.Exit != null)
            {
                yield return customerFlow.Replace(customer);
                yield break;
            }
            var start = customer.transform.position;
            var down = start + Vector3.down * 1.4f + Vector3.right * (happy ? 0.4f : -0.4f);
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                customer.transform.position = Vector3.Lerp(start, down, t / 0.35f);
                yield return null;
            }

            ResetCustomer(customer, false);
            var upFrom = start + Vector3.down * 1.4f + Vector3.left * 0.5f;
            customer.transform.position = upFrom;
            t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                customer.transform.position = Vector3.Lerp(upFrom, start, t / 0.35f);
                yield return null;
            }

            customer.transform.position = start;
            customer.Busy = false;
        }

        void ResetCustomer(CustomerAgent customer, bool snap)
        {
            customer.gameObject.SetActive(true);
            if (snap) customer.Busy = false;
            var visual = customer.GetComponentInChildren<BlinkCustomerVisual>(true);
            if (visual != null)
            {
                var others = new BlinkCustomerVisual[kitchen.Customers.Length];
                for (int i = 0; i < others.Length; i++)
                    if (kitchen.Customers[i] != null) others[i] = kitchen.Customers[i].GetComponentInChildren<BlinkCustomerVisual>(true);
                visual.ChooseAppearance(others);
            }
            customer.SetOrder(GameRules.RandomOrderSize(), GameConstants.CustomerWait);
            if (snap)
            {
                int index = System.Array.IndexOf(kitchen.Customers, customer);
                if (index >= 0 && customerStartPositions != null && index < customerStartPositions.Length)
                    customer.transform.localPosition = customerStartPositions[index];
            }
        }

        void Unslot(ItemView item)
        {
            if (item.GrillSlot >= 0 && item.GrillSlot < kitchen.GrillItems.Length && kitchen.GrillItems[item.GrillSlot] == item)
                kitchen.GrillItems[item.GrillSlot] = null;
            item.GrillSlot = -1;
            if (kitchen.PlateItem == item) kitchen.PlateItem = null;
        }

        void DestroyItem(ItemView item)
        {
            if (item == null) return;
            Unslot(item);
            Destroy(item.gameObject);
        }

        void ClearLooseItems()
        {
            if (held != null)
            {
                DestroyItem(held);
                held = null;
            }

            for (int i = 0; i < kitchen.GrillItems.Length; i++)
            {
                if (kitchen.GrillItems[i] != null)
                    DestroyItem(kitchen.GrillItems[i]);
            }

            if (kitchen.PlateItem != null)
                DestroyItem(kitchen.PlateItem);
        }

        static void Snap(Transform item, Transform anchor, Vector3 localOffset)
        {
            item.SetParent(anchor, true);
            item.localPosition = localOffset;
            item.localRotation = Quaternion.identity;
        }

        void UpdateHover()
        {
            if (hover != null) hover.SetHighlight(false);
            hover = null;
            if (phase != GamePhase.Playing && phase != GamePhase.RoundEnd) return;
            if (PointerOnUi()) return;
            if (!RayHit(out _, out var station, out _))
                station = NearestStation();
            hover = station;
            if (hover != null) hover.SetHighlight(true);
        }

        bool RayHit(out ItemView item, out StationTarget station, out RaycastHit hit)
        {
            item = null;
            station = null;
            hit = default;
            var hits = Physics.RaycastAll(ScreenRay(), 40f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider.transform == kitchen.DragPlane) continue;
                if (held != null && h.collider.transform.IsChildOf(held.transform)) continue;
                var foundItem = h.collider.GetComponentInParent<ItemView>();
                if (foundItem != null && foundItem != held)
                {
                    item = foundItem;
                    hit = h;
                    station = foundItem.GetComponentInParent<StationTarget>();
                    return true;
                }

                var foundStation = h.collider.GetComponentInParent<StationTarget>();
                if (foundStation != null)
                {
                    station = foundStation;
                    hit = h;
                    return true;
                }
            }

            return false;
        }

        ItemView NearestGrillItem()
        {
            if (kitchen.Camera == null || Mouse.current == null) return null;
            Vector2 mouse = Mouse.current.position.ReadValue();
            // Scale with the game view so the assistance feels consistent at different resolutions.
            float radius = Mathf.Max(24f, kitchen.Camera.pixelHeight * 45f / 1080f);
            float bestDistance = radius * radius;
            ItemView best = null;
            foreach (var meat in kitchen.GrillItems)
            {
                if (meat == null || meat.Held || !meat.gameObject.activeInHierarchy) continue;
                var screen = kitchen.Camera.WorldToScreenPoint(meat.transform.position);
                if (screen.z <= 0f) continue;
                float distance = (mouse - new Vector2(screen.x, screen.y)).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = meat;
                }
            }
            return best;
        }

        StationTarget NearestStation()
        {
            if (kitchen.Camera == null || Mouse.current == null) return null;
            Vector2 mouse = Mouse.current.position.ReadValue();
            StationTarget best = null;
            float bestDist = 110f;
            void Consider(StationTarget s)
            {
                if (s == null) return;
                var sp = kitchen.Camera.WorldToScreenPoint(s.transform.position);
                if (sp.z < 0.1f) return;
                float d = Vector2.Distance(mouse, new Vector2(sp.x, sp.y));
                if (d < bestDist)
                {
                    bestDist = d;
                    best = s;
                }
            }

            Consider(kitchen.MeatPile);
            Consider(kitchen.BunPile);
            Consider(kitchen.Trash);
            Consider(kitchen.Plate);
            for (int i = 0; i < kitchen.GrillSlots.Length; i++) Consider(kitchen.GrillSlots[i]);
            for (int i = 0; i < kitchen.Customers.Length; i++)
                Consider(kitchen.Customers[i].GetComponent<StationTarget>());
            return best;
        }

        Ray ScreenRay()
        {
            Vector2 pos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
            return kitchen.Camera.ScreenPointToRay(pos);
        }

        static bool PointerOnUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        void FaceBubblesToCamera()
        {
            if (kitchen.Camera == null) return;
            for (int i = 0; i < kitchen.Customers.Length; i++)
            {
                var customer = kitchen.Customers[i];
                if (customer != null) customer.FaceCamera(kitchen.Camera);
            }
        }
    }
}
