using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace ProjectScim
{
    /// <summary>
    /// Records the same telemetry channels VLGE emits (see docs/telemetry.md), so the
    /// dataset story survives the engine switch and scripts/extract_trajectory.py stays
    /// meaningful.
    ///
    /// Critically, this fills the six channels every supplied VLGE session left empty:
    /// zone access, interaction, gameplay events. Those are the Track-2 argument.
    /// </summary>
    public class SimTelemetry : MonoBehaviour
    {
        public static SimTelemetry Instance { get; private set; }

        [Tooltip("Trajectory samples per second. VLGE captured ~195 Hz; 50 is plenty here.")]
        public float SampleRate = 50f;

        readonly List<string> _traj = new List<string>();
        readonly List<string> _events = new List<string>();
        readonly List<Transform> _tracked = new List<Transform>();
        readonly List<string> _trackedNames = new List<string>();

        float _next;
        float _t0;

        public int TrajectoryCount => _traj.Count;
        public int EventCount => _events.Count;
        public IReadOnlyList<string> RecentEvents => _events;

        void Awake()
        {
            Instance = this;
            _t0 = Time.time;
            _traj.Add("t_rel_s,actor,x,y,z,qx,qy,qz,qw,speed_mps,action");
            _events.Add("t_rel_s,channel,actor,subject,detail");
        }

        public void Track(Transform t, string actorName)
        {
            _tracked.Add(t);
            _trackedNames.Add(actorName);
        }

        /// <summary>zoneIdentifierAccesses — entering/leaving a named zone.</summary>
        public void ZoneAccess(string actor, string zone, bool entering) =>
            Event("zoneIdentifierAccesses", actor, zone, entering ? "enter" : "exit");

        /// <summary>interactionSnapshots / interactionEventRecords — pick and place.</summary>
        public void Interaction(string actor, string subject, string verb) =>
            Event("interactionEventRecords", actor, subject, verb);

        /// <summary>gamePlayEventRecords — task-level milestones.</summary>
        public void GameplayEvent(string actor, string subject, string detail) =>
            Event("gamePlayEventRecords", actor, subject, detail);

        /// <summary>The handoff — the collaboration-timing signal the dataset is about.</summary>
        public void Handoff(string from, string to, string detail) =>
            Event("handoffEvents", from, to, detail);

        void Event(string channel, string actor, string subject, string detail)
        {
            float t = Time.time - _t0;
            _events.Add(string.Format(CultureInfo.InvariantCulture,
                "{0:F3},{1},{2},{3},{4}", t, channel, actor, subject, detail));
            Debug.Log($"[telemetry] {t:F2}s  {channel}  {actor} -> {subject}  ({detail})");
        }

        void Update()
        {
            if (Time.time < _next) return;
            _next = Time.time + 1f / Mathf.Max(1f, SampleRate);

            float t = Time.time - _t0;
            for (int i = 0; i < _tracked.Count; i++)
            {
                var tr = _tracked[i];
                if (tr == null) continue;

                // Speed and action label are derived the same way VLGE's dm_* signals are.
                var agent = tr.GetComponent<RobotAgent>();
                float speed = agent != null ? agent.CurrentSpeed : 0f;
                string action = agent != null ? agent.CurrentAction : "idle";

                var p = tr.position;
                var q = tr.rotation;
                _traj.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0:F3},{1},{2:F4},{3:F4},{4:F4},{5:F4},{6:F4},{7:F4},{8:F4},{9:F3},{10}",
                    t, _trackedNames[i], p.x, p.y, p.z, q.x, q.y, q.z, q.w, speed, action));
            }
        }

        /// <summary>Writes both CSVs next to the build. Bound to a key in SimHUD.</summary>
        public string Export()
        {
            string dir = Application.persistentDataPath;
            string stamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string trajPath = Path.Combine(dir, $"scim_trajectory_{stamp}.csv");
            string evPath = Path.Combine(dir, $"scim_events_{stamp}.csv");

            File.WriteAllText(trajPath, string.Join("\n", _traj), Encoding.UTF8);
            File.WriteAllText(evPath, string.Join("\n", _events), Encoding.UTF8);

            Debug.Log($"[telemetry] exported {_traj.Count - 1} samples -> {trajPath}");
            Debug.Log($"[telemetry] exported {_events.Count - 1} events -> {evPath}");
            return dir;
        }
    }
}
