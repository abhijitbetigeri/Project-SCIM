using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectScim
{
    /// <summary>
    /// Builds and runs the whole simulation procedurally — no prefabs, no imported art,
    /// no scene setup. Drop the scripts into a fresh Unity project and press Play.
    ///
    /// Three views, matching the three things a judge should see:
    ///   1  Restaurant branches       — the supply chain as physical space
    ///   2  Distributor warehouse     — where the purchased stock comes from
    ///   3  Pick and place            — the robot executing the Mise ticket up close
    /// </summary>
    public class SupplyChainSim : MonoBehaviour
    {
        public static SupplyChainSim Instance { get; private set; }

        public enum View { Branches = 0, Warehouse = 1, PickAndPlace = 2 }

        public View CurrentView { get; private set; } = View.Branches;
        public List<MiseScenario.Branch> Branches { get; private set; }
        public string StatusLine { get; private set; } = "Mise agents deciding…";
        public string PhaseLabel { get; private set; } = "Coordination";
        public bool TransferDone { get; private set; }
        public bool PurchaseDone { get; private set; }

        /// <summary>
        /// What the robot is doing right now, so an outbound "go and fetch" leg is never
        /// mistaken for a delivery. Empty when the robot is idle.
        /// </summary>
        public string RobotActivity
        {
            get
            {
                if (_robot == null) return "";
                switch (_robot.Current)
                {
                    case RobotAgent.Phase.ToSource:  return "robot en route to collect — empty";
                    case RobotAgent.Phase.Picking:   return "picking up the crate";
                    case RobotAgent.Phase.ToDest:    return "CARRYING to Downtown";
                    case RobotAgent.Phase.Handoff:   return "handing off to the cook";
                    case RobotAgent.Phase.Placing:   return "placing on the shelf";
                    default: return "";
                }
            }
        }

        readonly Dictionary<string, Transform> _anchors = new Dictionary<string, Transform>();
        RobotAgent _robot;
        Transform _cook;
        Transform _transferCrate, _purchaseCrate;
        SimCameraRig _camera;

        // Palette — kept muted so the accent colours (shortage red, resolve green) read.
        static readonly Color Floor = new Color(0.22f, 0.23f, 0.26f);
        static readonly Color Wall = new Color(0.32f, 0.33f, 0.37f);
        static readonly Color Fixture = new Color(0.45f, 0.42f, 0.38f);
        static readonly Color ShortageRed = new Color(0.91f, 0.29f, 0.24f);
        static readonly Color SurplusGreen = new Color(0.34f, 0.77f, 0.50f);
        static readonly Color CrateTan = new Color(0.80f, 0.55f, 0.28f);
        static readonly Color RobotBlue = new Color(0.25f, 0.55f, 0.92f);
        static readonly Color HumanWarm = new Color(0.93f, 0.72f, 0.45f);

        /// <summary>
        /// Bootstraps without any scene setup, so "create project, add scripts, press Play"
        /// is genuinely the whole procedure.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("ProjectSCIM");
            go.AddComponent<SimTelemetry>();
            Instance = go.AddComponent<SupplyChainSim>();
            go.AddComponent<SimHUD>();
        }

        void Start()
        {
            Instance = this;
            Branches = MiseScenario.NewBranches();

            BuildEnvironment();
            BuildLighting();

            _camera = SimCameraRig.Create();
            _camera.Focus(SimCameraRig.Preset.Branches);

            StartCoroutine(RunScenario());
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetView(View.Branches);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetView(View.Warehouse);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetView(View.PickAndPlace);
            if (Input.GetKeyDown(KeyCode.R)) RestartSim();
        }

        public void SetView(View v)
        {
            CurrentView = v;
            switch (v)
            {
                case View.Branches: _camera.Focus(SimCameraRig.Preset.Branches); break;
                case View.Warehouse: _camera.Focus(SimCameraRig.Preset.Warehouse); break;
                case View.PickAndPlace: _camera.FollowTarget = _robot != null ? _robot.transform : null;
                    _camera.Focus(SimCameraRig.Preset.Follow); break;
            }
        }

        void RestartSim()
        {
            // Reload the scene rather than tearing down by hand — version-safe, and
            // guarantees a clean telemetry session for the next run.
            Instance = null;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.buildIndex);
        }

        // ── the scenario ────────────────────────────────────────────────────────

        IEnumerator RunScenario()
        {
            PhaseLabel = "Coordination — decided";
            StatusLine = $"Downtown short {Branches[0].Shortfall:F0} kg {MiseScenario.Product}. " +
                         "Mise agents negotiating…";
            yield return new WaitForSeconds(2.5f);

            StatusLine = MiseScenario.DecisionRationale;
            yield return new WaitForSeconds(3.5f);

            // ── Transfer: Marina -> Downtown, executed by the robot ──────────────
            PhaseLabel = "Embodied execution — transfer";
            StatusLine = $"Ticket: transfer {MiseScenario.TransferQty:F0} kg Marina → Downtown " +
                         "(nearest expiry first).";
            SimTelemetry.Instance?.GameplayEvent("mise", "transfer",
                $"qty={MiseScenario.TransferQty} from=marina to=downtown");

            _robot.AssignTask(_transferCrate, _anchors["marina_shelf"], _anchors["downtown_shelf"],
                              _cook, "transfer_marina_downtown");
            yield return new WaitUntil(() => _robot.Current == RobotAgent.Phase.Done);

            Branches[1].OnHand -= MiseScenario.TransferQty;
            Branches[0].OnHand += MiseScenario.TransferQty;
            TransferDone = true;
            RecolorShelves();
            StatusLine = $"{MiseScenario.TransferQty:F0} kg delivered · waste avoided · " +
                         $"net still short {Branches[0].Shortfall:F0} kg.";
            yield return new WaitForSeconds(2.5f);

            // ── Purchase: warehouse -> Downtown ─────────────────────────────────
            PhaseLabel = "Embodied execution — procurement";
            StatusLine = $"Ticket: buy {MiseScenario.PurchaseQty:F0} kg from {MiseScenario.Supplier} " +
                         $"at ${MiseScenario.UnitPrice:F2}/kg.";
            SimTelemetry.Instance?.GameplayEvent("mise", "purchase_order",
                $"qty={MiseScenario.PurchaseQty} total={MiseScenario.PurchaseTotal}");

            SetView(View.Warehouse);
            yield return new WaitForSeconds(1.5f);
            SetView(View.PickAndPlace);

            _robot.AssignTask(_purchaseCrate, _anchors["warehouse_dock"], _anchors["downtown_shelf"],
                              _cook, "po_warehouse_downtown");
            yield return new WaitUntil(() => _robot.Current == RobotAgent.Phase.Done);

            Branches[0].OnHand += MiseScenario.PurchaseQty;
            PurchaseDone = true;
            RecolorShelves();

            PhaseLabel = "Behavioral dataset — learned";
            StatusLine = $"Resolved · {MiseScenario.TransferQty + MiseScenario.PurchaseQty:F0} kg restocked · " +
                         $"${MiseScenario.PurchaseTotal:F2} spent · " +
                         $"{SimTelemetry.Instance.TrajectoryCount:N0} trajectory samples captured.";
            SimTelemetry.Instance?.GameplayEvent("mise", "scenario", "resolved");
        }

        // ── world building ──────────────────────────────────────────────────────

        void BuildEnvironment()
        {
            Box("Ground", new Vector3(0f, -0.5f, 18f), new Vector3(110f, 1f, 90f), Floor, transform);

            foreach (var b in Branches) BuildBranch(b);
            BuildWarehouse();
            BuildActors();
        }

        void BuildBranch(MiseScenario.Branch b)
        {
            var root = new GameObject($"Branch_{b.Name}").transform;
            root.SetParent(transform, false);
            root.position = b.Origin;

            // Room: floor pad, back wall, side walls left open so the camera can see in.
            Box("Pad", b.Origin + new Vector3(0f, 0.02f, 0f), new Vector3(16f, 0.1f, 12f),
                Wall * 0.9f, root);
            Box("BackWall", b.Origin + new Vector3(0f, 1.6f, -6f), new Vector3(16f, 3.2f, 0.3f), Wall, root);
            Box("SideWallL", b.Origin + new Vector3(-8f, 1.6f, 0f), new Vector3(0.3f, 3.2f, 12f), Wall, root);
            Box("SideWallR", b.Origin + new Vector3(8f, 1.6f, 0f), new Vector3(0.3f, 3.2f, 12f), Wall, root);

            // The stock shelf — colour encodes shortage/surplus, which is the whole read.
            var shelfPos = b.Origin + new Vector3(0f, 0.6f, -4.6f);
            var shelf = Box($"Shelf_{b.Name}", shelfPos, new Vector3(6f, 1.2f, 1.2f), Fixture, root);
            _anchors[$"{b.Name.ToLower()}_shelf"] = shelf.transform;

            // Bar / pass — the pinch point the robot must negotiate.
            Box("Bar", b.Origin + new Vector3(0f, 0.55f, 0.5f), new Vector3(9f, 1.1f, 0.9f), Fixture * 0.9f, root);
            Box("BarGapL", b.Origin + new Vector3(-5.6f, 0.55f, 0.5f), new Vector3(1.4f, 1.1f, 0.9f), Fixture * 0.7f, root);

            // A few tables so the space reads as a restaurant, and so the whiskers have work.
            for (int i = -1; i <= 1; i++)
            {
                Box($"Table{i}", b.Origin + new Vector3(i * 4f, 0.4f, 3.6f),
                    new Vector3(1.6f, 0.8f, 1.6f), Fixture * 0.8f, root);
            }

            AddZone($"{b.Name}", b.Origin + new Vector3(0f, 1.2f, -3.4f), new Vector3(12f, 2.5f, 5f), root);
            MakeLabel(b.Name.ToUpper(), b.Origin + new Vector3(0f, 3.4f, -5.9f), root);
        }

        void BuildWarehouse()
        {
            var o = MiseScenario.WarehouseOrigin;
            var root = new GameObject("Warehouse").transform;
            root.SetParent(transform, false);

            Box("Pad", o + new Vector3(0f, 0.02f, 0f), new Vector3(34f, 0.1f, 20f), Wall * 0.8f, root);
            Box("BackWall", o + new Vector3(0f, 3f, -10f), new Vector3(34f, 6f, 0.4f), Wall, root);
            Box("SideL", o + new Vector3(-17f, 3f, 0f), new Vector3(0.4f, 6f, 20f), Wall, root);
            Box("SideR", o + new Vector3(17f, 3f, 0f), new Vector3(0.4f, 6f, 20f), Wall, root);

            // Racking — the distributor's inventory, and the visual identity of view 2.
            for (int row = 0; row < 4; row++)
            {
                float z = -7f + row * 4.2f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * 8f;
                    for (int level = 0; level < 3; level++)
                    {
                        Box($"Rack_{row}_{side}_{level}", o + new Vector3(x, 0.6f + level * 1.5f, z),
                            new Vector3(9f, 0.18f, 2.4f), Fixture * 0.75f, root);
                    }
                    Box($"Post_{row}_{side}", o + new Vector3(x, 2.2f, z),
                        new Vector3(0.25f, 4.4f, 0.25f), Fixture * 0.55f, root);

                    // Pallets of stock on the racks.
                    for (int p = 0; p < 3; p++)
                    {
                        Box($"Pallet_{row}_{side}_{p}",
                            o + new Vector3(x - 3f + p * 3f, 1.15f, z),
                            new Vector3(1.9f, 0.9f, 1.9f), CrateTan * (0.8f + p * 0.06f), root);
                    }
                }
            }

            var dock = Box("Dock", o + new Vector3(0f, 0.45f, 8.4f), new Vector3(6f, 0.9f, 2.4f),
                           Fixture, root);
            _anchors["warehouse_dock"] = dock.transform;
            AddZone("Warehouse_Dock", o + new Vector3(0f, 1.4f, 8.4f), new Vector3(9f, 3f, 5f), root);
            MakeLabel("DISTRIBUTOR WAREHOUSE", o + new Vector3(0f, 6.4f, -9.8f), root);
        }

        void BuildActors()
        {
            // The robot: a body, a sensor mast, and a colour that reads as machine.
            var robotGo = new GameObject("RestockRobot");
            robotGo.transform.SetParent(transform, false);
            // Starts at Downtown — the branch that is short. Its first leg is then an
            // outbound trip to fetch, and the return leg is the actual transfer. Starting
            // at the warehouse made the first leg look like a supplier delivery.
            robotGo.transform.position = Branches[0].Origin + new Vector3(5f, 0f, 3f);

            Box("Chassis", Vector3.zero, new Vector3(1.1f, 0.55f, 1.5f), RobotBlue, robotGo.transform,
                localOffset: new Vector3(0f, 0.35f, 0f));
            Box("Mast", Vector3.zero, new Vector3(0.28f, 1.0f, 0.28f), RobotBlue * 0.8f, robotGo.transform,
                localOffset: new Vector3(0f, 1.05f, -0.3f));
            var head = Prim("Sensor", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.34f,
                            Color.white, robotGo.transform);
            head.transform.localPosition = new Vector3(0f, 1.6f, -0.3f);

            var cap = robotGo.AddComponent<CapsuleCollider>();
            cap.height = 1.8f; cap.radius = 0.55f; cap.center = new Vector3(0f, 0.9f, 0f);
            cap.isTrigger = false;
            var rb = robotGo.AddComponent<Rigidbody>();
            rb.isKinematic = true; rb.useGravity = false;

            _robot = robotGo.AddComponent<RobotAgent>();
            _robot.ActorName = "restock-robot-01";
            SimTelemetry.Instance?.Track(robotGo.transform, "restock-robot-01");

            // The cook at the Downtown pass — the human half of the collaboration.
            var cookGo = Prim("Cook_Downtown", PrimitiveType.Capsule,
                              Branches[0].Origin + new Vector3(1.8f, 1f, -1.4f),
                              new Vector3(0.8f, 1f, 0.8f), HumanWarm, transform);
            _cook = cookGo.transform;
            SimTelemetry.Instance?.Track(_cook, "cook-downtown");

            // The two crates the tickets move.
            _transferCrate = Box("Crate_Transfer_10kg",
                Branches[1].Origin + new Vector3(0f, 1.45f, -4.6f),
                new Vector3(1.0f, 0.7f, 1.0f), CrateTan, transform).transform;
            _purchaseCrate = Box("Crate_PO_26kg",
                MiseScenario.WarehouseOrigin + new Vector3(0f, 1.35f, 8.4f),
                new Vector3(1.3f, 0.9f, 1.3f), CrateTan * 0.9f, transform).transform;

            RecolorShelves();
        }

        void RecolorShelves()
        {
            foreach (var b in Branches)
            {
                if (!_anchors.TryGetValue($"{b.Name.ToLower()}_shelf", out var shelf)) continue;
                var r = shelf.GetComponent<Renderer>();
                if (r == null) continue;

                // Red while below par, green once resolved, amber in between.
                Color c;
                if (b.Shortfall > 0.01f)
                    c = b.OnHand < b.Reorder ? ShortageRed : Color.Lerp(ShortageRed, SurplusGreen, 0.45f);
                else
                    c = SurplusGreen;
                r.material.color = c;
            }
        }

        void BuildLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.05f;
            sun.color = new Color(1f, 0.97f, 0.92f);
            sun.transform.rotation = Quaternion.Euler(48f, 34f, 0f);
            sun.shadows = LightShadows.Soft;
            RenderSettings.ambientLight = new Color(0.38f, 0.40f, 0.45f);
        }

        // ── primitives ──────────────────────────────────────────────────────────

        GameObject Box(string name, Vector3 pos, Vector3 size, Color c, Transform parent,
                       Vector3? localOffset = null)
        {
            var go = Prim(name, PrimitiveType.Cube, pos, size, c, parent);
            if (localOffset.HasValue) go.transform.localPosition = localOffset.Value;
            return go;
        }

        // Loaded from Assets/Resources so the build stripper keeps the shader. Primitives
        // created at runtime reference nothing in the saved scene, so URP/Lit gets
        // stripped and everything renders magenta in a player build.
        static Material _litSource;

        GameObject Prim(string name, PrimitiveType type, Vector3 pos, Vector3 size, Color c,
                        Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                if (_litSource == null) _litSource = Resources.Load<Material>("SimLit");
                if (_litSource != null) r.material = new Material(_litSource);
                r.material.color = c;
            }
            return go;
        }

        void AddZone(string zoneName, Vector3 center, Vector3 size, Transform parent)
        {
            var go = new GameObject($"Zone_{zoneName}");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            bc.isTrigger = true;
            go.AddComponent<SimZone>().ZoneName = zoneName;
        }

        /// <summary>A thin coloured plate standing in for signage — no font dependency.</summary>
        void MakeLabel(string text, Vector3 pos, Transform parent)
        {
            var go = Box($"Sign_{text}", pos, new Vector3(6.5f, 0.9f, 0.15f),
                         new Color(0.12f, 0.13f, 0.16f), parent);
            var t = go.AddComponent<SimWorldLabel>();
            t.Text = text;
        }
    }

    /// <summary>Draws a world-space label via OnGUI — avoids a TextMeshPro dependency.</summary>
    public class SimWorldLabel : MonoBehaviour
    {
        public string Text = "";
        static GUIStyle _style;

        void OnGUI()
        {
            var cam = Camera.main;
            if (cam == null || string.IsNullOrEmpty(Text)) return;

            Vector3 sp = cam.WorldToScreenPoint(transform.position);
            if (sp.z <= 0f) return;

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 15,
                    fontStyle = FontStyle.Bold
                };
                _style.normal.textColor = Color.white;
            }

            var rect = new Rect(sp.x - 130f, Screen.height - sp.y - 12f, 260f, 24f);
            GUI.Label(rect, Text, _style);
        }
    }
}
