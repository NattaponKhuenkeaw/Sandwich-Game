using UnityEngine;
using UnityEngine.UI;

namespace SandwichGame
{
    public static class KitchenFactory
    {
        public static System.Func<string, Color, float, float, Material> MaterialLookup;

        static Shader litShader;

        public static void Build(KitchenScene scene)
        {
            EnsureShader();
            SetupLighting();
            var root = scene.transform;
            scene.Root = root;
            scene.Camera = SetupCamera();
            scene.GrillSlots = new StationTarget[GameConstants.GrillSlotCount];
            scene.GrillAnchors = new Transform[GameConstants.GrillSlotCount];
            scene.GrillItems = new ItemView[GameConstants.GrillSlotCount];
            scene.Customers = new CustomerAgent[GameConstants.CustomerCount];

            FloorWalls(root);
            BuildCounter(root);
            scene.MeatPile = BuildBin(root, "MeatPile", StationKind.MeatPile, new Vector3(-1.55f, 1.02f, 0.52f), new Color(0.55f, 0.18f, 0.22f), "เนื้อ", "MeatBin");
            StackDisplayMeats(scene.MeatPile.transform);
            scene.Trash = BuildBin(root, "Trash", StationKind.Trash, new Vector3(-0.82f, 1.02f, 0.52f), new Color(0.18f, 0.28f, 0.20f), "ทิ้ง", "TrashBin");
            BuildTrashBag(scene.Trash.transform);

            var grill = BuildGrill(root, new Vector3(0.05f, 1.02f, 0.58f));
            scene.GrillLight = grill.GetComponentInChildren<Light>();
            float[] slotX = { -0.28f, 0f, 0.28f };
            for (int i = 0; i < GameConstants.GrillSlotCount; i++)
            {
                var slot = new GameObject("GrillSlot" + i).transform;
                slot.SetParent(grill, false);
                slot.localPosition = new Vector3(slotX[i], 0.1f, 0f);
                var col = slot.gameObject.AddComponent<BoxCollider>();
                col.size = new Vector3(0.28f, 0.22f, 0.46f);
                var target = slot.gameObject.AddComponent<StationTarget>();
                target.Kind = StationKind.Grill;
                target.SlotIndex = i;
                scene.GrillSlots[i] = target;
                scene.GrillAnchors[i] = slot;
            }

            scene.BunPile = BuildBin(root, "BunPile", StationKind.BunPile, new Vector3(1.55f, 1.02f, 0.52f), new Color(0.72f, 0.52f, 0.28f), "ขนมปัง", "BunBin");
            StackDisplayBuns(scene.BunPile.transform);

            scene.Plate = BuildPlate(root, new Vector3(0.78f, 1.02f, 0.12f));
            scene.PlateAnchor = scene.Plate.transform.Find("Anchor");

            var plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
            plane.name = "DragPlane";
            plane.transform.SetParent(root, false);
            plane.transform.position = new Vector3(0f, 1.12f, 0.4f);
            plane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            plane.transform.localScale = new Vector3(6f, 4f, 1f);
            SafeDestroy(plane.GetComponent<MeshRenderer>());
            scene.DragPlane = plane.transform;

            BuildCustomers(root, scene, scene.Camera);
        }

        public static ItemView SpawnItem(ItemKind kind, Vector3 worldPos, KitchenScene kitchen)
        {
            var prefab = PrefabFor(kind, kitchen);
            if (prefab != null)
            {
                var spawned = Object.Instantiate(prefab, worldPos, Quaternion.identity, kitchen.Root);
                spawned.name = kind.ToString();
                spawned.Kind = kind;
                spawned.GrillTime = 0f;
                spawned.GrillSlot = -1;
                spawned.Held = false;
                spawned.HomeKind = kind == ItemKind.Bun ? StationKind.BunPile : StationKind.MeatPile;
                spawned.ApplyKindVisual();
                spawned.RememberHome();
                return spawned;
            }

            return SpawnPrimitiveItem(kind, worldPos, kitchen != null ? kitchen.Root : null);
        }

        public static ItemView SpawnPrimitiveItem(ItemKind kind, Vector3 worldPos, Transform parent)
        {
            EnsureShader();
            var root = new GameObject(kind.ToString());
            root.transform.SetParent(parent, false);
            root.transform.position = worldPos;
            var view = root.AddComponent<ItemView>();
            view.Kind = kind;
            view.HomeKind = kind == ItemKind.Bun ? StationKind.BunPile : StationKind.MeatPile;

            switch (kind)
            {
                case ItemKind.Bun:
                    view.ColorRenderers = new[] { MakePrimitive(root.transform, PrimitiveType.Sphere, Vector3.zero, new Vector3(0.18f, 0.08f, 0.18f), GameRules.ColorFor(kind), 0.55f, 0f, false, "Bun").GetComponent<Renderer>() };
                    break;
                case ItemKind.Sandwich:
                    MakePrimitive(root.transform, PrimitiveType.Sphere, new Vector3(0f, -0.03f, 0f), new Vector3(0.19f, 0.07f, 0.19f), GameRules.ColorFor(ItemKind.Bun), 0.55f, 0f, false, "Bun");
                    var meat = MakePrimitive(root.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.16f, 0.025f, 0.16f), GameRules.ColorFor(ItemKind.CookedMeat), 0.35f, 0f, false, "CookedMeat");
                    MakePrimitive(root.transform, PrimitiveType.Sphere, new Vector3(0f, 0.04f, 0f), new Vector3(0.19f, 0.07f, 0.19f), GameRules.ColorFor(ItemKind.Bun), 0.55f, 0f, false, "Bun");
                    view.ColorRenderers = new[] { meat.GetComponent<Renderer>() };
                    break;
                default:
                    view.ColorRenderers = new[] { MakePrimitive(root.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.16f, 0.03f, 0.16f), GameRules.ColorFor(kind), 0.25f, 0f, false, kind.ToString()).GetComponent<Renderer>() };
                    break;
            }

            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.22f, 0.12f, 0.22f);
            view.RememberHome();
            return view;
        }

        static ItemView PrefabFor(ItemKind kind, KitchenScene kitchen)
        {
            if (kitchen == null) return null;
            switch (kind)
            {
                case ItemKind.RawMeat: return kitchen.RawMeatPrefab;
                case ItemKind.CookedMeat: return kitchen.CookedMeatPrefab;
                case ItemKind.BurnedMeat: return kitchen.BurnedMeatPrefab;
                case ItemKind.Bun: return kitchen.BunPrefab;
                case ItemKind.Sandwich: return kitchen.SandwichPrefab;
                default: return null;
            }
        }

        static void FloorWalls(Transform root)
        {
            MakePrimitive(root, PrimitiveType.Cube, new Vector3(0f, 0f, 1.2f), new Vector3(8f, 0.05f, 7f), new Color(0.62f, 0.45f, 0.32f), 0.2f, 0f, true, "Floor");
            MakePrimitive(root, PrimitiveType.Cube, new Vector3(0f, 1.7f, 3.4f), new Vector3(8f, 3.4f, 0.12f), new Color(0.93f, 0.78f, 0.52f), 0.15f, 0f, true, "Wall");
            MakePrimitive(root, PrimitiveType.Cube, new Vector3(-3.6f, 1.7f, 1.2f), new Vector3(0.12f, 3.4f, 7f), new Color(0.90f, 0.72f, 0.46f), 0.15f, 0f, true, "WallSide");
            MakePrimitive(root, PrimitiveType.Cube, new Vector3(3.6f, 1.7f, 1.2f), new Vector3(0.12f, 3.4f, 7f), new Color(0.90f, 0.72f, 0.46f), 0.15f, 0f, true, "WallSide");
            MakePrimitive(root, PrimitiveType.Cube, new Vector3(0f, 3.35f, 1.2f), new Vector3(8f, 0.08f, 7f), new Color(0.85f, 0.74f, 0.58f), 0.1f, 0f, true, "Ceiling");
            MakePrimitive(root, PrimitiveType.Quad, new Vector3(0f, 2.35f, 3.33f), new Vector3(2.4f, 1.1f, 1f), new Color(0.45f, 0.75f, 0.88f), 0.05f, 0f, true, "Window");
        }

        static void BuildCounter(Transform root)
        {
            MakePrimitive(root, PrimitiveType.Cube, new Vector3(0f, 0.48f, 0.45f), new Vector3(4.6f, 0.96f, 1.35f), new Color(0.38f, 0.22f, 0.13f), 0.28f, 0f, true, "Counter");
            MakePrimitive(root, PrimitiveType.Cube, new Vector3(0f, 0.98f, 0.45f), new Vector3(4.7f, 0.08f, 1.42f), new Color(0.55f, 0.32f, 0.18f), 0.45f, 0f, true, "CounterTop");
            MakePrimitive(root, PrimitiveType.Cube, new Vector3(0f, 0.72f, 1.12f), new Vector3(4.7f, 0.55f, 0.08f), new Color(0.32f, 0.18f, 0.10f), 0.25f, 0f, true, "CounterFront");
        }

        static StationTarget BuildBin(Transform root, string name, StationKind kind, Vector3 pos, Color color, string label, string matName)
        {
            var bin = new GameObject(name);
            bin.transform.SetParent(root, false);
            bin.transform.position = pos;
            var box = bin.AddComponent<BoxCollider>();
            box.size = new Vector3(0.58f, 0.36f, 0.58f);
            box.center = new Vector3(0f, 0.08f, 0f);
            var vis = MakePrimitive(bin.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.52f, 0.16f, 0.52f), color, 0.3f, 0f, false, matName);
            var target = bin.AddComponent<StationTarget>();
            target.Kind = kind;
            target.HighlightRenderer = vis.GetComponent<Renderer>();
            target.BaseColor = color;
            AddWorldLabel(bin.transform, label, new Vector3(0f, 0.28f, -0.32f));
            return target;
        }

        static Transform BuildGrill(Transform root, Vector3 pos)
        {
            var grill = new GameObject("Grill");
            grill.transform.SetParent(root, false);
            grill.transform.position = pos;
            MakePrimitive(grill.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.95f, 0.14f, 0.62f), new Color(0.12f, 0.12f, 0.13f), 0.15f, 0.85f, false, "Grill");
            MakePrimitive(grill.transform, PrimitiveType.Cube, new Vector3(0f, 0.09f, 0f), new Vector3(0.9f, 0.02f, 0.08f), new Color(0.35f, 0.12f, 0.05f), 0.2f, 0f, false, "GrillEmber");
            for (int i = 0; i < 6; i++)
            {
                float z = -0.22f + i * 0.09f;
                MakePrimitive(grill.transform, PrimitiveType.Cube, new Vector3(0f, 0.08f, z), new Vector3(0.88f, 0.015f, 0.03f), new Color(0.18f, 0.18f, 0.2f), 0.4f, 0.8f, false, "GrillGrate");
            }

            var lightGo = new GameObject("GrillGlow");
            lightGo.transform.SetParent(grill.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.32f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.2f);
            light.intensity = 1.6f;
            light.range = 2.4f;
            AddWorldLabel(grill.transform, "เตา", new Vector3(0f, 0.28f, -0.4f));
            return grill.transform;
        }

        static StationTarget BuildPlate(Transform root, Vector3 pos)
        {
            var plate = new GameObject("Plate");
            plate.transform.SetParent(root, false);
            plate.transform.position = pos;
            var vis = MakePrimitive(plate.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.42f, 0.018f, 0.42f), new Color(0.93f, 0.93f, 0.90f), 0.7f, 0f, false, "Plate");
            var target = plate.AddComponent<StationTarget>();
            target.Kind = StationKind.Plate;
            target.HighlightRenderer = vis.GetComponent<Renderer>();
            target.BaseColor = new Color(0.93f, 0.93f, 0.90f);
            var box = plate.AddComponent<BoxCollider>();
            box.size = new Vector3(0.55f, 0.28f, 0.55f);
            box.center = new Vector3(0f, 0.08f, 0f);
            var anchor = new GameObject("Anchor").transform;
            anchor.SetParent(plate.transform, false);
            anchor.localPosition = new Vector3(0f, 0.06f, 0f);
            AddWorldLabel(plate.transform, "จาน", new Vector3(0f, 0.22f, -0.3f));
            return target;
        }

        static void BuildCustomers(Transform root, KitchenScene scene, Camera cam)
        {
            float[] xs = { -1.4f, 0f, 1.4f };
            var palette = new[]
            {
                new Color(0.22f, 0.45f, 0.78f),
                new Color(0.78f, 0.32f, 0.42f),
                new Color(0.28f, 0.62f, 0.40f)
            };
            var matNames = new[] { "ShirtBlue", "ShirtRed", "ShirtGreen" };

            for (int i = 0; i < GameConstants.CustomerCount; i++)
            {
                var customer = new GameObject("Customer" + i);
                customer.transform.SetParent(root, false);
                customer.transform.position = new Vector3(xs[i], 0f, 2.25f);
                var body = MakePrimitive(customer.transform, PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0f), new Vector3(0.42f, 0.55f, 0.42f), palette[i], 0.35f, 0f, false, matNames[i]);
                MakePrimitive(customer.transform, PrimitiveType.Sphere, new Vector3(0f, 1.52f, 0f), Vector3.one * 0.34f, new Color(0.96f, 0.82f, 0.70f), 0.35f, 0f, false, "Skin");
                MakePrimitive(customer.transform, PrimitiveType.Cube, new Vector3(0f, 0.22f, 0f), new Vector3(0.5f, 0.12f, 0.28f), new Color(0.18f, 0.16f, 0.16f), 0.2f, 0f, false, "Shoes");

                var agent = customer.AddComponent<CustomerAgent>();
                agent.SlotIndex = i;
                agent.Body = customer.transform;
                var target = customer.AddComponent<StationTarget>();
                target.Kind = StationKind.Customer;
                target.SlotIndex = i;
                target.HighlightRenderer = body.GetComponent<Renderer>();
                target.BaseColor = palette[i];
                var hit = customer.AddComponent<BoxCollider>();
                hit.center = new Vector3(0f, 1.0f, 0f);
                hit.size = new Vector3(0.7f, 2.0f, 0.7f);

                BuildBubble(agent, cam);
                scene.Customers[i] = agent;
            }
        }

        static void BuildBubble(CustomerAgent agent, Camera cam)
        {
            var canvasGo = new GameObject("Bubble");
            canvasGo.transform.SetParent(agent.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            canvasGo.transform.localRotation = Quaternion.identity;
            canvasGo.transform.localScale = Vector3.one * 0.008f;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvasGo.AddComponent<CanvasScaler>();
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220f, 90f);
            canvas.overrideSorting = true;
            canvas.sortingOrder = 20;

            var bg = MakeUiImage(canvasGo.transform, new Vector2(220f, 90f), Color.white);
            bg.canvasRenderer.cullTransparentMesh = false;
            var timerBg = MakeUiImage(canvasGo.transform, new Vector2(190f, 12f), new Color(0.15f, 0.15f, 0.15f, 1f));
            timerBg.rectTransform.anchoredPosition = new Vector2(0f, -28f);
            timerBg.canvasRenderer.cullTransparentMesh = false;
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(timerBg.transform, false);
            var fill = fillGo.AddComponent<UnityEngine.UI.Image>();
            fill.sprite = UiUtil.White();
            fill.color = new Color(0.28f, 0.72f, 0.38f);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.canvasRenderer.cullTransparentMesh = false;
            var fillRt = fill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var labelGo = new GameObject("Order");
            labelGo.transform.SetParent(canvasGo.transform, false);
            var label = labelGo.AddComponent<Text>();
            label.font = FontUtil.Thai();
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.15f, 0.12f, 0.10f);
            label.text = "แฮมเบอร์เกอร์ x1";
            var lrt = label.rectTransform;
            lrt.sizeDelta = new Vector2(200f, 50f);
            lrt.anchoredPosition = new Vector2(0f, 10f);

            agent.Bubble = canvas;
            agent.TimerFill = fill;
            agent.OrderLabel = label;
        }

        static UnityEngine.UI.Image MakeUiImage(Transform parent, Vector2 size, Color color)
        {
            var go = new GameObject("Img");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.sprite = UiUtil.White();
            img.color = color;
            img.rectTransform.sizeDelta = size;
            return img;
        }

        static void StackDisplayMeats(Transform bin)
        {
            for (int i = 0; i < 3; i++)
            {
                MakePrimitive(bin, PrimitiveType.Cylinder, new Vector3((i - 1) * 0.12f, 0.12f, 0f), new Vector3(0.16f, 0.03f, 0.16f), GameRules.ColorFor(ItemKind.RawMeat), 0.25f, 0f, false, "RawMeat");
            }
        }

        static void StackDisplayBuns(Transform bin)
        {
            for (int i = 0; i < 3; i++)
            {
                MakePrimitive(bin, PrimitiveType.Sphere, new Vector3((i - 1) * 0.12f, 0.14f, 0f), new Vector3(0.16f, 0.07f, 0.16f), GameRules.ColorFor(ItemKind.Bun), 0.5f, 0f, false, "Bun");
            }
        }

        static void BuildTrashBag(Transform bin)
        {
            MakePrimitive(bin, PrimitiveType.Sphere, new Vector3(0f, 0.14f, 0f), new Vector3(0.36f, 0.2f, 0.36f), new Color(0.12f, 0.2f, 0.14f), 0.15f, 0f, false, "TrashBag");
        }

        static void AddWorldLabel(Transform parent, string text, Vector3 localPos)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = Vector3.one * 0.0065f;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(160f, 40f);
            var label = go.AddComponent<Text>();
            label.font = FontUtil.Thai();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 28;
            label.color = Color.white;
            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.7f);
        }

        static Camera SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                cam = go.AddComponent<Camera>();
                go.tag = "MainCamera";
            }

            cam.transform.position = new Vector3(0f, 1.72f, -1.18f);
            cam.transform.rotation = Quaternion.Euler(24f, 0f, 0f);
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.08f;
            cam.clearFlags = CameraClearFlags.Skybox;
            return cam;
        }

        static void SetupLighting()
        {
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            Light light = null;
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    light = lights[i];
                    break;
                }
            }

            if (light == null)
            {
                var go = new GameObject("Directional Light");
                light = go.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.transform.rotation = Quaternion.Euler(42f, -30f, 0f);
            light.color = new Color(1f, 0.93f, 0.82f);
            light.intensity = 1.15f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.48f, 0.40f);
        }

        static GameObject MakePrimitive(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Color color, float smooth, float metal, bool collider, string matName)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = matName;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(matName, color, smooth, metal);
            if (!collider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) SafeDestroy(col);
            }
            return go;
        }

        static Material Mat(string name, Color color, float smooth, float metal)
        {
            if (MaterialLookup != null)
                return MaterialLookup(name, color, smooth, metal);

            EnsureShader();
            var mat = new Material(litShader);
            mat.name = name;
            ApplyColor(mat, color, smooth, metal);
            return mat;
        }

        public static void ApplyColor(Material mat, Color color, float smooth, float metal)
        {
            mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smooth);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metal);
        }

        public static Shader LitShader()
        {
            EnsureShader();
            return litShader;
        }

        static void EnsureShader()
        {
            if (litShader != null) return;
            litShader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                        ?? Shader.Find("Standard")
                        ?? Shader.Find("Unlit/Color")
                        ?? Shader.Find("Sprites/Default")
                        ?? Shader.Find("Hidden/InternalErrorShader");
        }

        public static void SafeDestroy(Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
