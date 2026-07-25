using UnityEngine;

namespace ProjectScim
{
    /// <summary>
    /// Judge-facing overlay. Uses IMGUI on purpose — no Canvas to configure, no
    /// TextMeshPro package, nothing that can fail to import on a fresh project.
    ///
    /// It has to answer three questions in the first ten seconds: what am I looking at,
    /// what is happening, and why does it matter for robotics.
    /// </summary>
    public class SimHUD : MonoBehaviour
    {
        GUIStyle _title, _body, _mono, _kicker, _pill;
        Texture2D _panelTex, _accentTex;
        string _exportNote = "";
        float _exportNoteUntil;

        void EnsureStyles()
        {
            if (_title != null) return;

            _panelTex = SolidTex(new Color(0.06f, 0.07f, 0.09f, 0.90f));
            _accentTex = SolidTex(new Color(0.91f, 0.29f, 0.24f, 1f));

            _title = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = false
            };
            _title.normal.textColor = Color.white;

            _kicker = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
            _kicker.normal.textColor = new Color(0.91f, 0.45f, 0.35f);

            _body = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
            _body.normal.textColor = new Color(0.87f, 0.89f, 0.92f);

            _mono = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = false };
            _mono.normal.textColor = new Color(0.72f, 0.78f, 0.85f);

            _pill = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
            };
            _pill.normal.textColor = Color.white;
        }

        static Texture2D SolidTex(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T) && SimTelemetry.Instance != null)
            {
                string dir = SimTelemetry.Instance.Export();
                _exportNote = "Telemetry exported to " + dir;
                _exportNoteUntil = Time.time + 6f;
            }
        }

        void OnGUI()
        {
            EnsureStyles();
            var sim = SupplyChainSim.Instance;
            if (sim == null) return;

            // Kept deliberately narrow — the world has to stay the subject, not the UI.
            float w = Mathf.Min(430f, Screen.width * 0.30f);

            // ── header ──────────────────────────────────────────────────────────
            GUI.DrawTexture(new Rect(0, 0, Screen.width, 74f), _panelTex);
            GUI.DrawTexture(new Rect(0, 71f, Screen.width, 3f), _accentTex);
            GUI.Label(new Rect(20, 10, Screen.width - 40, 30),
                      "Project-SCIM — supply chain as a world model", _title);
            GUI.Label(new Rect(22, 42, Screen.width - 40, 22),
                      "Mise agents decide the restock · a robot executes it in space · every run is training data",
                      _body);

            // ── left: state panel ───────────────────────────────────────────────
            float y = 92f;
            GUI.DrawTexture(new Rect(14, y, w, 232f), _panelTex);
            GUI.Label(new Rect(28, y + 12, w - 28, 18), sim.PhaseLabel.ToUpper(), _kicker);
            GUI.Label(new Rect(28, y + 34, w - 42, 60), sim.StatusLine, _body);

            float ry = y + 100f;
            GUI.Label(new Rect(28, ry, w - 40, 18),
                      string.Format("{0,-10} {1,8} {2,6} {3,14}", "BRANCH", "ON HAND", "PAR", "STATUS"), _mono);
            ry += 20f;
            foreach (var b in sim.Branches)
            {
                string status = b.Shortfall > 0.01f ? $"-{b.Shortfall:F0} kg"
                              : b.Surplus > 0.01f ? $"+{b.Surplus:F0} kg surplus"
                              : "at par";
                var prev = _mono.normal.textColor;
                _mono.normal.textColor = b.Shortfall > 0.01f
                    ? new Color(0.95f, 0.5f, 0.42f) : new Color(0.55f, 0.86f, 0.66f);
                GUI.Label(new Rect(28, ry, w - 40, 18),
                          string.Format("{0,-10} {1,8:F1} {2,6:F0} {3,14}",
                                        b.Name, b.OnHand, b.Par, status), _mono);
                _mono.normal.textColor = prev;
                ry += 19f;
            }

            ry += 8f;
            GUI.Label(new Rect(28, ry, w - 40, 18),
                      $"Ticket  transfer {MiseScenario.TransferQty:F0} kg  ·  buy {MiseScenario.PurchaseQty:F0} kg  ·  ${MiseScenario.PurchaseTotal:F2}",
                      _mono);

            // ── right: telemetry panel — the Track-2 argument, on screen ────────
            float rw = Mathf.Min(320f, Screen.width * 0.23f);
            float rx = Screen.width - rw - 14f;
            float ty = 92f;
            GUI.DrawTexture(new Rect(rx, ty, rw, 186f), _panelTex);
            GUI.Label(new Rect(rx + 14, ty + 12, rw - 28, 18), "BEHAVIORAL DATASET (LIVE)", _kicker);

            var tel = SimTelemetry.Instance;
            if (tel != null)
            {
                GUI.Label(new Rect(rx + 14, ty + 36, rw - 28, 18),
                          $"trajectory samples   {tel.TrajectoryCount:N0}", _mono);
                GUI.Label(new Rect(rx + 14, ty + 55, rw - 28, 18),
                          $"discrete events      {tel.EventCount:N0}", _mono);

                GUI.Label(new Rect(rx + 14, ty + 82, rw - 28, 18), "recent:", _mono);
                int shown = 0;
                for (int i = tel.RecentEvents.Count - 1; i >= 1 && shown < 4; i--, shown++)
                {
                    var parts = tel.RecentEvents[i].Split(',');
                    if (parts.Length < 5) continue;
                    GUI.Label(new Rect(rx + 20, ty + 100 + shown * 18, rw - 34, 18),
                              $"{parts[0]}s  {parts[1]}  {parts[3]}", _mono);
                }
            }

            // ── footer: controls ────────────────────────────────────────────────
            float fy = Screen.height - 44f;
            GUI.DrawTexture(new Rect(0, fy, Screen.width, 44f), _panelTex);
            string keys = "[1] restaurant branches    [2] distributor warehouse    [3] follow the robot" +
                          "        right-drag look · WASD fly · scroll zoom        [T] export telemetry    [R] restart";
            GUI.Label(new Rect(20, fy + 13, Screen.width - 40, 20), keys, _mono);

            if (Time.time < _exportNoteUntil)
            {
                GUI.DrawTexture(new Rect(Screen.width / 2f - 260f, fy - 46f, 520f, 34f), _panelTex);
                GUI.Label(new Rect(Screen.width / 2f - 250f, fy - 38f, 500f, 20f), _exportNote, _body);
            }
        }
    }
}
