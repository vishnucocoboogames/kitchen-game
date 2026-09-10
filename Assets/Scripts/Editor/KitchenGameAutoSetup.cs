using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using KitchenGame.Core;
using KitchenGame.Ingredients;
using KitchenGame.Orders;
using KitchenGame.Player;
using KitchenGame.Stations;
using KitchenGame.UI;

namespace KitchenGame.Editor
{
    [InitializeOnLoad]
    public static class KitchenGameAutoSetup
    {
        private const string SetupCompleteKey = "KitchenGame_NoEmoji_Setup_v3";

        static KitchenGameAutoSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(SetupCompleteKey, false))
                {
                    SessionState.SetBool(SetupCompleteKey, true);
                    SetupEntireProject();
                }
            };
        }

        [MenuItem("Kitchen Game/Setup Entire Game (Play Ready)", priority = 1)]
        public static void SetupEntireProject()
        {
            Debug.Log("<color=cyan><b>[KitchenGame] Starting automated game setup (Clean TextMeshPro, no emojis)...</b></color>");

            EnsureDirectories();
            ImportTMPEssentialsIfNeeded();

            var materials = CreateMaterials();
            var (vegData, cheeseData, meatData) = CreateIngredientDataAssets(materials);
            var ingredientPrefab = CreateIngredientPrefab(materials);
            var rowPrefab = CreateIngredientRowPrefab();

            BuildGameScene(materials, vegData, cheeseData, meatData, ingredientPrefab, rowPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=green><b>[KitchenGame] SETUP COMPLETE! No missing glyphs. Press PLAY in Unity to play!</b></color>");
        }

        private static void EnsureDirectories()
        {
            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets", "Materials");
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets", "Scenes");
        }

        private static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }

        private static void ImportTMPEssentialsIfNeeded()
        {
            if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            {
                string pkgPath = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
                if (File.Exists(pkgPath))
                {
                    AssetDatabase.ImportPackage(pkgPath, false);
                }
            }
        }

        private static Shader GetAppropriateShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Lit");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Mobile/Diffuse");
            if (s == null) s = Shader.Find("Unlit/Color");
            return s;
        }

        private static Material CreateMat(string name, Color color, Shader shader)
        {
            string path = $"Assets/Materials/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        public class GameMaterials
        {
            public Material Floor;
            public Material Wall;
            public Material Fridge;
            public Material Table;
            public Material Stove;
            public Material Trash;
            public Material Window;
            public Material Player;
            public Material ChefHat;
            public Material RawVeg;
            public Material PrepVeg;
            public Material Cheese;
            public Material RawMeat;
            public Material PrepMeat;
            public Material UIWhite;
            public Material UIGray;
        }

        private static GameMaterials CreateMaterials()
        {
            Shader shader = GetAppropriateShader();
            var m = new GameMaterials();

            m.Floor     = CreateMat("FloorMat", new Color(0.85f, 0.88f, 0.92f), shader);      // Slate 200
            m.Wall      = CreateMat("WallMat", new Color(0.33f, 0.41f, 0.52f), shader);       // Slate 600
            m.Fridge    = CreateMat("FridgeMat", new Color(0.14f, 0.65f, 0.95f), shader);     // Sky 500
            m.Table     = CreateMat("TableMat", new Color(0.06f, 0.73f, 0.51f), shader);      // Emerald 500
            m.Stove     = CreateMat("StoveMat", new Color(0.25f, 0.28f, 0.35f), shader);      // Dark slate
            m.Trash     = CreateMat("TrashMat", new Color(0.98f, 0.45f, 0.09f), shader);      // Orange 500
            m.Window    = CreateMat("WindowMat", new Color(0.94f, 0.27f, 0.27f), shader);     // Red 500
            m.Player    = CreateMat("PlayerMat", new Color(0.55f, 0.36f, 0.96f), shader);     // Violet 500
            m.ChefHat   = CreateMat("ChefHatMat", Color.white, shader);
            m.RawVeg    = CreateMat("RawVegMat", new Color(0.13f, 0.65f, 0.26f), shader);     // Forest green
            m.PrepVeg   = CreateMat("PrepVegMat", new Color(0.52f, 0.90f, 0.15f), shader);    // Lime
            m.Cheese    = CreateMat("CheeseMat", new Color(0.98f, 0.80f, 0.08f), shader);     // Yellow
            m.RawMeat   = CreateMat("RawMeatMat", new Color(0.86f, 0.15f, 0.15f), shader);    // Bright red
            m.PrepMeat  = CreateMat("PrepMeatMat", new Color(0.47f, 0.21f, 0.06f), shader);   // Cooked brown
            m.UIWhite   = CreateMat("UIWhiteMat", Color.white, shader);
            m.UIGray    = CreateMat("UIGrayMat", new Color(0.2f, 0.2f, 0.25f), shader);

            return m;
        }

        private static (IngredientData, IngredientData, IngredientData) CreateIngredientDataAssets(GameMaterials m)
        {
            IngredientData veg = AssetDatabase.LoadAssetAtPath<IngredientData>("Assets/Data/VegetableData.asset");
            if (veg == null)
            {
                veg = ScriptableObject.CreateInstance<IngredientData>();
                AssetDatabase.CreateAsset(veg, "Assets/Data/VegetableData.asset");
            }
            veg.ingredientType = IngredientType.Vegetable;
            veg.displayName = "Vegetable";
            veg.scoreValue = 20;
            veg.requiresPreparation = true;
            veg.prepStationTag = "ChoppingTable";
            veg.prepDuration = 2f;
            veg.rawColor = new Color(0.13f, 0.65f, 0.26f);
            veg.preparedColor = new Color(0.52f, 0.90f, 0.15f);
            EditorUtility.SetDirty(veg);

            IngredientData cheese = AssetDatabase.LoadAssetAtPath<IngredientData>("Assets/Data/CheeseData.asset");
            if (cheese == null)
            {
                cheese = ScriptableObject.CreateInstance<IngredientData>();
                AssetDatabase.CreateAsset(cheese, "Assets/Data/CheeseData.asset");
            }
            cheese.ingredientType = IngredientType.Cheese;
            cheese.displayName = "Cheese";
            cheese.scoreValue = 10;
            cheese.requiresPreparation = false;
            cheese.prepStationTag = "";
            cheese.prepDuration = 0f;
            cheese.rawColor = new Color(0.98f, 0.80f, 0.08f);
            cheese.preparedColor = new Color(0.98f, 0.80f, 0.08f);
            EditorUtility.SetDirty(cheese);

            IngredientData meat = AssetDatabase.LoadAssetAtPath<IngredientData>("Assets/Data/MeatData.asset");
            if (meat == null)
            {
                meat = ScriptableObject.CreateInstance<IngredientData>();
                AssetDatabase.CreateAsset(meat, "Assets/Data/MeatData.asset");
            }
            meat.ingredientType = IngredientType.Meat;
            meat.displayName = "Meat";
            meat.scoreValue = 30;
            meat.requiresPreparation = true;
            meat.prepStationTag = "Stove";
            meat.prepDuration = 6f;
            meat.rawColor = new Color(0.86f, 0.15f, 0.15f);
            meat.preparedColor = new Color(0.47f, 0.21f, 0.06f);
            EditorUtility.SetDirty(meat);

            return (veg, cheese, meat);
        }

        private static GameObject CreateIngredientPrefab(GameMaterials m)
        {
            string path = "Assets/Prefabs/IngredientPrefab.prefab";
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "IngredientPrefab";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            go.GetComponent<MeshRenderer>().sharedMaterial = m.RawVeg;
            go.AddComponent<IngredientObject>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateIngredientRowPrefab()
        {
            string path = "Assets/Prefabs/IngredientRowPrefab.prefab";
            GameObject go = new GameObject("IngredientRow", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140, 18);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 15;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            tmp.text = "Vegetable";

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static void BuildGameScene(GameMaterials m, IngredientData veg, IngredientData cheese, IngredientData meat, GameObject ingPrefab, GameObject rowPrefab)
        {
            string scenePath = "Assets/Scenes/Game.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Clean existing root objects
            var roots = scene.GetRootGameObjects();
            foreach (var r in roots)
                Object.DestroyImmediate(r);

            // 1. Lighting & Environment
            var lightGo = new GameObject("Directional Light", typeof(Light));
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.98f, 0.94f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            // 2. Camera (Top-down)
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.11f, 0.17f);
            camGo.transform.position = new Vector3(0f, 16f, 0f);
            camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            // 3. Kitchen Bounds & Floor
            var envRoot = new GameObject("--- ENVIRONMENT ---");

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(envRoot.transform);
            floor.transform.position = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(16f, 0.2f, 10f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = m.Floor;

            // Walls (North, South, East, West)
            CreateWall("WallNorth", new Vector3(0f, 0.75f, 5.1f), new Vector3(16.5f, 1.5f, 0.4f), m.Wall, envRoot.transform);
            CreateWall("WallSouth", new Vector3(0f, 0.75f, -5.1f), new Vector3(16.5f, 1.5f, 0.4f), m.Wall, envRoot.transform);
            CreateWall("WallEast", new Vector3(8.1f, 0.75f, 0f), new Vector3(0.4f, 1.5f, 10.4f), m.Wall, envRoot.transform);
            CreateWall("WallWest", new Vector3(-8.1f, 0.75f, 0f), new Vector3(0.4f, 1.5f, 10.4f), m.Wall, envRoot.transform);

            // 4. Stations
            var stationsRoot = new GameObject("--- STATIONS ---");

            // Refrigerator
            var fridgeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fridgeGo.name = "Refrigerator";
            fridgeGo.transform.SetParent(stationsRoot.transform);
            fridgeGo.transform.position = new Vector3(6f, 0.7f, 2.7f);
            fridgeGo.transform.localScale = new Vector3(1.6f, 1.4f, 1.6f);
            fridgeGo.GetComponent<MeshRenderer>().sharedMaterial = m.Fridge;
            AddStationTrigger(fridgeGo, new Vector3(2.6f, 2f, 2.6f));

            var fridgeComp = fridgeGo.AddComponent<Refrigerator>();
            fridgeComp.vegetableData = veg;
            fridgeComp.cheeseData = cheese;
            fridgeComp.meatData = meat;

            // Refrigerator UI (parent to stationsRoot to keep clean scale)
            var fridgeUI = CreateWorldCanvas("FridgeSelectionCanvas", stationsRoot.transform, new Vector3(4.1f, 1.35f, 2.7f), new Vector2(200, 110));
            var fridgePanel = CreateUIPanel(fridgeUI.transform, "SelectionPanel", new Vector2(200, 110), new Color(0.1f, 0.15f, 0.25f, 0.95f));
            CreateUIText(fridgePanel.transform, "Title", "PICK INGREDIENT", 14, TextAlignmentOptions.Center, new Vector2(0, 36), new Vector2(190, 24), Color.yellow, true);
            var btnVeg = CreateUIButton(fridgePanel.transform, "BtnVeg", "1: Vegetable", 14, new Vector2(0, 12), new Vector2(180, 22), new Color(0.1f, 0.6f, 0.25f));
            var btnChs = CreateUIButton(fridgePanel.transform, "BtnCheese", "2: Cheese", 14, new Vector2(0, -12), new Vector2(180, 22), new Color(0.85f, 0.7f, 0.1f));
            var btnMeat = CreateUIButton(fridgePanel.transform, "BtnMeat", "3: Meat", 14, new Vector2(0, -36), new Vector2(180, 22), new Color(0.8f, 0.2f, 0.2f));
            fridgeComp.selectionPanel = fridgePanel;
            fridgeComp.btnVegetable = btnVeg;
            fridgeComp.btnCheese = btnChs;
            fridgeComp.btnMeat = btnMeat;

            // Chopping Table
            var tableGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tableGo.name = "ChoppingTable";
            tableGo.transform.SetParent(stationsRoot.transform);
            tableGo.transform.position = new Vector3(0.5f, 0.5f, 1.8f);
            tableGo.transform.localScale = new Vector3(2.6f, 1f, 1.4f);
            tableGo.GetComponent<MeshRenderer>().sharedMaterial = m.Table;
            AddStationTrigger(tableGo, new Vector3(3.2f, 2f, 2.2f));

            var tableComp = tableGo.AddComponent<ChoppingTable>();
            var tableDisp = new GameObject("DisplayPoint").transform;
            tableDisp.SetParent(tableGo.transform);
            tableDisp.localPosition = new Vector3(0f, 0.6f, 0f);
            tableComp.ingredientDisplayPoint = tableDisp;
            tableComp.chopDuration = 2f;

            var tableUICanvas = CreateWorldCanvas("TableTimerCanvas", stationsRoot.transform, new Vector3(0.5f, 1.4f, 2.8f), new Vector2(160, 40));
            var tableUIPanel = CreateUIPanel(tableUICanvas.transform, "TimerPanel", new Vector2(160, 40), new Color(0.1f, 0.15f, 0.2f, 0.9f));
            var (tableBar, tableTxt) = CreateProgressBarWithText(tableUIPanel.transform, new Color(0.2f, 0.85f, 0.3f));
            tableComp.timerUI = tableUIPanel;
            tableComp.progressBar = tableBar;
            tableComp.timerText = tableTxt;

            // Stove
            var stoveGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stoveGo.name = "Stove";
            stoveGo.transform.SetParent(stationsRoot.transform);
            stoveGo.transform.position = new Vector3(0.5f, 0.5f, -1.8f);
            stoveGo.transform.localScale = new Vector3(2.6f, 1f, 1.4f);
            stoveGo.GetComponent<MeshRenderer>().sharedMaterial = m.Stove;
            AddStationTrigger(stoveGo, new Vector3(3.2f, 2f, 2.2f));

            var stoveComp = stoveGo.AddComponent<Stove>();
            stoveComp.slots = new Stove.StoveSlot[2];

            // Stove Slot 0
            var s0Disp = new GameObject("Slot0_Disp").transform;
            s0Disp.SetParent(stoveGo.transform);
            s0Disp.localPosition = new Vector3(-0.35f, 0.6f, 0f);
            var s0UICanvas = CreateWorldCanvas("Slot0_TimerCanvas", stationsRoot.transform, new Vector3(-0.3f, 1.4f, -2.8f), new Vector2(100, 36));
            var s0UIPanel = CreateUIPanel(s0UICanvas.transform, "Slot0_Panel", new Vector2(100, 36), new Color(0.1f, 0.15f, 0.2f, 0.9f));
            var (s0Bar, s0Txt) = CreateProgressBarWithText(s0UIPanel.transform, new Color(0.9f, 0.3f, 0.2f));
            stoveComp.slots[0] = new Stove.StoveSlot
            {
                displayPoint = s0Disp,
                progressBar = s0Bar,
                timerText = s0Txt,
                timerUI = s0UIPanel
            };

            // Stove Slot 1
            var s1Disp = new GameObject("Slot1_Disp").transform;
            s1Disp.SetParent(stoveGo.transform);
            s1Disp.localPosition = new Vector3(0.35f, 0.6f, 0f);
            var s1UICanvas = CreateWorldCanvas("Slot1_TimerCanvas", stationsRoot.transform, new Vector3(1.3f, 1.4f, -2.8f), new Vector2(100, 36));
            var s1UIPanel = CreateUIPanel(s1UICanvas.transform, "Slot1_Panel", new Vector2(100, 36), new Color(0.1f, 0.15f, 0.2f, 0.9f));
            var (s1Bar, s1Txt) = CreateProgressBarWithText(s1UIPanel.transform, new Color(0.9f, 0.3f, 0.2f));
            stoveComp.slots[1] = new Stove.StoveSlot
            {
                displayPoint = s1Disp,
                progressBar = s1Bar,
                timerText = s1Txt,
                timerUI = s1UIPanel
            };

            // Trash Bin
            var trashGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trashGo.name = "TrashBin";
            trashGo.transform.SetParent(stationsRoot.transform);
            trashGo.transform.position = new Vector3(6f, 0.45f, -2.7f);
            trashGo.transform.localScale = new Vector3(1.6f, 0.9f, 1.6f);
            trashGo.GetComponent<MeshRenderer>().sharedMaterial = m.Trash;
            AddStationTrigger(trashGo, new Vector3(2.6f, 2f, 2.6f));
            trashGo.AddComponent<TrashBin>();

            // Customer Windows 1..4 (Spaced cleanly, non-overlapping)
            float[] windowZ = { 3.45f, 1.15f, -1.15f, -3.45f };
            var windowComps = new CustomerWindow[4];

            for (int i = 0; i < 4; i++)
            {
                var winGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                winGo.name = $"Window_{i + 1}";
                winGo.transform.SetParent(stationsRoot.transform);
                winGo.transform.position = new Vector3(-7.2f, 0.6f, windowZ[i]);
                winGo.transform.localScale = new Vector3(1.1f, 1.2f, 1.8f);
                winGo.GetComponent<MeshRenderer>().sharedMaterial = m.Window;
                AddStationTrigger(winGo, new Vector3(2.8f, 2f, 2.2f));

                var winComp = winGo.AddComponent<CustomerWindow>();
                winComp.windowIndex = i;
                windowComps[i] = winComp;

                // Window World UI - parent to stationsRoot to avoid non-uniform scale inheritance
                var winUICanvas = CreateWorldCanvas($"WindowUI_{i + 1}", stationsRoot.transform, new Vector3(-5.7f, 1.35f, windowZ[i]), new Vector2(150, 110));
                var winPanel = CreateUIPanel(winUICanvas.transform, "Card", new Vector2(150, 110), new Color(0.12f, 0.16f, 0.23f, 0.95f));

                CreateUIText(winPanel.transform, "Header", $"ORDER #{i + 1}", 14, TextAlignmentOptions.Center, new Vector2(0, 42), new Vector2(140, 20), Color.cyan, true);

                var ingContainer = new GameObject("IngContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
                ingContainer.transform.SetParent(winPanel.transform, false);
                var ingRT = ingContainer.GetComponent<RectTransform>();
                ingRT.anchoredPosition = new Vector2(0, 5);
                ingRT.sizeDelta = new Vector2(140, 50);
                var vlg = ingContainer.GetComponent<VerticalLayoutGroup>();
                vlg.childAlignment = TextAnchor.UpperLeft;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = true;
                vlg.spacing = 2;

                var emptyLabel = CreateUIText(winPanel.transform, "EmptyLabel", "Waiting...", 14, TextAlignmentOptions.Center, new Vector2(0, 5), new Vector2(140, 30), new Color(0.7f, 0.7f, 0.7f));
                var elapsedTxt = CreateUIText(winPanel.transform, "Timer", "0s", 16, TextAlignmentOptions.MidlineRight, new Vector2(0, -42), new Vector2(140, 20), Color.yellow, true);

                var winUIComp = winUICanvas.AddComponent<WindowUI>();
                winUIComp.ingredientContainer = ingContainer.transform;
                winUIComp.ingredientRowPrefab = rowPrefab;
                winUIComp.elapsedTimerText = elapsedTxt;
                winUIComp.emptyLabel = emptyLabel.gameObject;
                winComp.windowUI = winUIComp;

                // Score Popup
                var popupCanvas = CreateWorldCanvas($"ScorePopup_{i + 1}", stationsRoot.transform, new Vector3(-4.4f, 1.4f, windowZ[i]), new Vector2(100, 35));
                var popupTxt = CreateUIText(popupCanvas.transform, "PopupText", "+25", 26, TextAlignmentOptions.Center, Vector2.zero, new Vector2(100, 35), Color.green, true);
                popupTxt.gameObject.SetActive(false);
                var popupComp = popupCanvas.AddComponent<ScorePopup>();
                popupComp.popupText = popupTxt;
                winComp.scorePopup = popupComp;
            }

            // 5. Player
            var playerRoot = new GameObject("--- PLAYER ---");
            var playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerGo.name = "PlayerChef";
            playerGo.transform.SetParent(playerRoot.transform);
            playerGo.transform.position = new Vector3(-2.8f, 0.6f, 0f);
            playerGo.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
            playerGo.GetComponent<MeshRenderer>().sharedMaterial = m.Player;

            Object.DestroyImmediate(playerGo.GetComponent<CapsuleCollider>());
            var cc = playerGo.AddComponent<CharacterController>();
            cc.radius = 0.4f;
            cc.height = 1.2f;
            cc.center = Vector3.zero;

            // Chef Hat
            var hatGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hatGo.name = "ChefHat";
            hatGo.transform.SetParent(playerGo.transform);
            hatGo.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            hatGo.transform.localScale = new Vector3(0.45f, 0.3f, 0.45f);
            hatGo.GetComponent<MeshRenderer>().sharedMaterial = m.ChefHat;
            Object.DestroyImmediate(hatGo.GetComponent<Collider>());

            // Nose visor (shows chef forward direction)
            var noseGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            noseGo.name = "NoseDirection";
            noseGo.transform.SetParent(playerGo.transform);
            noseGo.transform.localPosition = new Vector3(0f, 0.2f, 0.5f);
            noseGo.transform.localScale = new Vector3(0.2f, 0.15f, 0.3f);
            noseGo.GetComponent<MeshRenderer>().sharedMaterial = m.ChefHat;
            Object.DestroyImmediate(noseGo.GetComponent<Collider>());

            // Hold Point
            var holdPoint = new GameObject("HoldPoint").transform;
            holdPoint.SetParent(playerGo.transform);
            holdPoint.localPosition = new Vector3(0f, 1.1f, 0.5f);

            var playerComp = playerGo.AddComponent<PlayerController>();
            playerComp.GetType().GetField("holdPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(playerComp, holdPoint);
            playerComp.GetType().GetField("ingredientPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(playerComp, ingPrefab);

            // 6. Managers
            var managersRoot = new GameObject("--- MANAGERS ---");

            var gmGo = new GameObject("GameManager");
            gmGo.transform.SetParent(managersRoot.transform);
            var gmComp = gmGo.AddComponent<GameManager>();

            var omGo = new GameObject("OrderManager");
            omGo.transform.SetParent(managersRoot.transform);
            var omComp = omGo.AddComponent<OrderManager>();
            omComp.customerWindows = windowComps;

            // 7. Main Screen UI Canvas
            var canvasGo = new GameObject("UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var mainCanvas = canvasGo.GetComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            var uiMgrGo = new GameObject("UIManager");
            uiMgrGo.transform.SetParent(managersRoot.transform);
            var uiMgr = uiMgrGo.AddComponent<UIManager>();
            uiMgr.playerController = playerComp;

            // HUD Panel
            var hudPanel = CreateUIPanel(canvasGo.transform, "HUD_Panel", new Vector2(1920, 1080), Color.clear);
            var topBar = CreateUIPanel(hudPanel.transform, "TopBar", new Vector2(1880, 70), new Color(0.06f, 0.09f, 0.15f, 0.88f));
            topBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 500);

            var scoreTxt = CreateUIText(topBar.transform, "ScoreText", "Score: 0", 26, TextAlignmentOptions.MidlineLeft, new Vector2(-750, 0), new Vector2(300, 50), new Color(0.98f, 0.8f, 0.2f), true);
            var highTxt = CreateUIText(topBar.transform, "HighText", "High Score: 0", 20, TextAlignmentOptions.MidlineLeft, new Vector2(-450, 0), new Vector2(300, 50), new Color(0.7f, 0.75f, 0.85f));
            var timerTxt = CreateUIText(topBar.transform, "TimerText", "03:00", 38, TextAlignmentOptions.Center, new Vector2(0, 0), new Vector2(250, 50), Color.white, true);
            var btnPause = CreateUIButton(topBar.transform, "BtnPause", "Pause", 20, new Vector2(650, 0), new Vector2(140, 46), new Color(0.2f, 0.45f, 0.85f));
            var btnQuit = CreateUIButton(topBar.transform, "BtnQuit", "Quit", 20, new Vector2(810, 0), new Vector2(120, 46), new Color(0.85f, 0.25f, 0.25f));

            var bottomBar = CreateUIPanel(hudPanel.transform, "BottomBar", new Vector2(800, 50), new Color(0.06f, 0.09f, 0.15f, 0.88f));
            bottomBar.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -500);
            var hintTxt = CreateUIText(bottomBar.transform, "HintText", "Press E to interact", 22, TextAlignmentOptions.Center, Vector2.zero, new Vector2(780, 40), new Color(0.3f, 0.95f, 0.6f), true);

            uiMgr.hudPanel = hudPanel;
            uiMgr.scoreText = scoreTxt;
            uiMgr.highScoreText = highTxt;
            uiMgr.timerText = timerTxt;
            uiMgr.pauseButton = btnPause;
            uiMgr.pauseButtonText = btnPause.GetComponentInChildren<TextMeshProUGUI>();
            uiMgr.quitButton = btnQuit;
            uiMgr.interactHintText = hintTxt;

            // Main Menu Panel
            var menuPanel = CreateUIPanel(canvasGo.transform, "MainMenu_Panel", new Vector2(1920, 1080), new Color(0.04f, 0.06f, 0.1f, 0.94f));
            var menuCard = CreateUIPanel(menuPanel.transform, "MenuCard", new Vector2(760, 580), new Color(0.1f, 0.14f, 0.22f, 1f));
            CreateUIText(menuCard.transform, "Title", "KITCHEN RUSH", 42, TextAlignmentOptions.Center, new Vector2(0, 230), new Vector2(700, 60), new Color(0.98f, 0.8f, 0.2f), true);
            CreateUIText(menuCard.transform, "Subtitle", "Fast-Paced 3D Kitchen Management Game", 20, TextAlignmentOptions.Center, new Vector2(0, 185), new Vector2(700, 30), new Color(0.7f, 0.8f, 0.9f));

            var ctrlTxt = CreateUIText(menuCard.transform, "ControlsText", "", 18, TextAlignmentOptions.TopLeft, new Vector2(0, 20), new Vector2(660, 260), Color.white);
            var btnStart = CreateUIButton(menuCard.transform, "BtnStart", "START GAME", 26, new Vector2(0, -220), new Vector2(300, 64), new Color(0.08f, 0.72f, 0.42f));

            uiMgr.mainMenuPanel = menuPanel;
            uiMgr.startButton = btnStart;
            uiMgr.controlsText = ctrlTxt;

            // Pause Overlay Panel
            var pausePanel = CreateUIPanel(canvasGo.transform, "Pause_Panel", new Vector2(1920, 1080), new Color(0.02f, 0.04f, 0.08f, 0.8f));
            var pauseCard = CreateUIPanel(pausePanel.transform, "PauseCard", new Vector2(450, 300), new Color(0.1f, 0.14f, 0.22f, 1f));
            CreateUIText(pauseCard.transform, "Header", "GAME PAUSED", 36, TextAlignmentOptions.Center, new Vector2(0, 80), new Vector2(400, 50), Color.white, true);
            var btnResume = CreateUIButton(pauseCard.transform, "BtnResume", "Resume Game", 22, new Vector2(0, -20), new Vector2(260, 54), new Color(0.2f, 0.5f, 0.9f));
            pausePanel.SetActive(false);

            uiMgr.pausePanel = pausePanel;
            uiMgr.resumeButton = btnResume;

            // Game Over Panel
            var overPanel = CreateUIPanel(canvasGo.transform, "GameOver_Panel", new Vector2(1920, 1080), new Color(0.03f, 0.05f, 0.1f, 0.94f));
            var overCard = CreateUIPanel(overPanel.transform, "OverCard", new Vector2(560, 480), new Color(0.1f, 0.14f, 0.22f, 1f));
            CreateUIText(overCard.transform, "Header", "TIME'S UP!", 40, TextAlignmentOptions.Center, new Vector2(0, 170), new Vector2(500, 55), new Color(0.95f, 0.3f, 0.3f), true);
            var finalScoreTxt = CreateUIText(overCard.transform, "FinalScore", "Final Score: 0", 30, TextAlignmentOptions.Center, new Vector2(0, 95), new Vector2(500, 45), new Color(0.98f, 0.8f, 0.2f), true);
            var finalHighTxt = CreateUIText(overCard.transform, "FinalHigh", "High Score: 0", 24, TextAlignmentOptions.Center, new Vector2(0, 45), new Vector2(500, 40), Color.white);
            var newHighTxt = CreateUIText(overCard.transform, "NewHighBadge", "NEW HIGH SCORE!", 24, TextAlignmentOptions.Center, new Vector2(0, -5), new Vector2(500, 40), new Color(0.2f, 0.95f, 0.4f), true);
            var btnRestart = CreateUIButton(overCard.transform, "BtnRestart", "Play Again", 24, new Vector2(0, -140), new Vector2(280, 60), new Color(0.08f, 0.72f, 0.42f));
            overPanel.SetActive(false);

            uiMgr.gameOverPanel = overPanel;
            uiMgr.finalScoreText = finalScoreTxt;
            uiMgr.finalHighScoreText = finalHighTxt;
            uiMgr.newHighScoreText = newHighTxt;
            uiMgr.restartButton = btnRestart;

            // 8. EventSystem
            var esGo = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
#if ENABLE_INPUT_SYSTEM
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif

            // Save Scene
            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void CreateWall(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        private static void AddStationTrigger(GameObject go, Vector3 triggerSize)
        {
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = triggerSize;
            int interactLayer = LayerMask.NameToLayer("Interactable");
            if (interactLayer != -1)
                go.layer = interactLayer;
        }

        private static GameObject CreateWorldCanvas(string name, Transform parent, Vector3 worldPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            return go;
        }

        private static GameObject CreateUIPanel(Transform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        private static TextMeshProUGUI CreateUIText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Vector2 pos, Vector2 size, Color color, bool bold = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.text = text;
            tmp.color = color;
            if (bold) tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }

        private static Button CreateUIButton(Transform parent, string name, string label, float fontSize, Vector2 pos, Vector2 size, Color bgColor)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = btnGo.GetComponent<Image>();
            img.color = bgColor;

            var btn = btnGo.GetComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = bgColor * 1.2f;
            colors.pressedColor = bgColor * 0.8f;
            btn.colors = colors;

            var txtGo = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.sizeDelta = size;
            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = label;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;

            return btn;
        }

        private static (Image, TextMeshProUGUI) CreateProgressBarWithText(Transform parent, Color barColor)
        {
            // Background
            var bgGo = new GameObject("BarBG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGo.transform.SetParent(parent, false);
            var bgRT = bgGo.GetComponent<RectTransform>();
            bgRT.anchoredPosition = new Vector2(0, 8);
            bgRT.sizeDelta = new Vector2(130, 14);
            bgGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

            // Fill Bar
            var fillGo = new GameObject("BarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRT = fillGo.GetComponent<RectTransform>();
            fillRT.sizeDelta = new Vector2(130, 14);
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.color = barColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 0f;

            // Text
            var txtGo = new GameObject("TimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(parent, false);
            var txtRT = txtGo.GetComponent<RectTransform>();
            txtRT.anchoredPosition = new Vector2(0, -10);
            txtRT.sizeDelta = new Vector2(130, 18);
            var txt = txtGo.GetComponent<TextMeshProUGUI>();
            txt.fontSize = 14;
            txt.alignment = TextAlignmentOptions.Center;
            txt.text = "0.0s";
            txt.color = Color.white;
            txt.fontStyle = FontStyles.Bold;

            return (fillImg, txt);
        }
    }
}
