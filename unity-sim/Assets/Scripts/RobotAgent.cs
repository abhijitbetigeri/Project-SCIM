using System.Collections.Generic;
using UnityEngine;

namespace ProjectScim
{
    /// <summary>
    /// The physical-AI half: an autonomous agent that executes a Mise transfer ticket
    /// as pick -> carry -> place. Deliberately uses simple steering rather than NavMesh
    /// so the scene needs no bake and no extra packages.
    ///
    /// Obstacle avoidance is a short whisker cast — crude, but it produces the
    /// path-curvature and clearance behavior the dataset is actually about.
    /// </summary>
    public class RobotAgent : MonoBehaviour
    {
        public enum Phase { Idle, ToSource, Picking, ToDest, Placing, Handoff, Done }

        [Header("Motion")]
        public float MaxSpeed = 3.2f;
        public float Accel = 6f;
        public float TurnRate = 220f;      // deg/sec
        public float ArriveRadius = 1.2f;

        [Header("Avoidance")]
        public float LookAhead = 3.0f;
        public float AvoidStrength = 1.6f;
        public LayerMask ObstacleMask = ~0;

        [Header("Task timing — these become the collaboration-timing data")]
        public float PickDuration = 1.1f;
        public float PlaceDuration = 1.1f;
        public float HandoffPause = 1.6f;

        public string ActorName = "robot-01";
        public Phase Current { get; private set; } = Phase.Idle;
        public float CurrentSpeed { get; private set; }
        public string CurrentAction { get; private set; } = "idle";
        public Transform Carried { get; private set; }

        public System.Action<RobotAgent> OnTaskComplete;

        Vector3 _velocity;
        float _timer;
        Transform _source, _dest, _payload, _handoffPartner;
        string _taskLabel = "";
        Transform _carryAnchor;

        readonly HashSet<string> _zonesInside = new HashSet<string>();

        void Awake()
        {
            _carryAnchor = new GameObject("CarryAnchor").transform;
            _carryAnchor.SetParent(transform, false);
            _carryAnchor.localPosition = new Vector3(0f, 1.15f, 0.55f);
        }

        /// <summary>Dispatch a transfer ticket. This is the Mise record made physical.</summary>
        public void AssignTask(Transform payload, Transform source, Transform dest,
                               Transform handoffPartner, string label)
        {
            _payload = payload;
            _source = source;
            _dest = dest;
            _handoffPartner = handoffPartner;
            _taskLabel = label;
            Current = Phase.ToSource;
            CurrentAction = "walk";
            SimTelemetry.Instance?.GameplayEvent(ActorName, label, "task_assigned");
        }

        void Update()
        {
            switch (Current)
            {
                case Phase.ToSource:
                    if (Steer(_source.position)) BeginPick();
                    break;

                case Phase.Picking:
                    if (Tick(PickDuration)) CompletePick();
                    break;

                case Phase.ToDest:
                    if (Steer(_dest.position)) BeginPlace();
                    break;

                case Phase.Placing:
                    if (Tick(PlaceDuration)) CompletePlace();
                    break;

                case Phase.Handoff:
                    if (Tick(HandoffPause)) CompleteHandoff();
                    break;

                default:
                    Decelerate();
                    break;
            }
        }

        // ── task steps ──────────────────────────────────────────────────────────

        void BeginPick()
        {
            Current = Phase.Picking;
            CurrentAction = "pick";
            _timer = 0f;
            SimTelemetry.Instance?.Interaction(ActorName, _payload.name, "pick_begin");
        }

        void CompletePick()
        {
            Carried = _payload;
            _payload.SetParent(_carryAnchor, true);
            _payload.localPosition = Vector3.zero;
            _payload.localRotation = Quaternion.identity;

            var rb = _payload.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            Current = Phase.ToDest;
            CurrentAction = "carry";
            SimTelemetry.Instance?.Interaction(ActorName, _payload.name, "pick_complete");
        }

        void BeginPlace()
        {
            // A handoff partner means a human is receiving — pause before releasing,
            // which is exactly the timing distribution the dataset exists to capture.
            if (_handoffPartner != null)
            {
                Current = Phase.Handoff;
                CurrentAction = "handoff_approach";
                _timer = 0f;
                float d = Vector3.Distance(transform.position, _handoffPartner.position);
                SimTelemetry.Instance?.Handoff(ActorName, _handoffPartner.name,
                    $"approach_distance_m={d:F2}");
                return;
            }

            Current = Phase.Placing;
            CurrentAction = "place";
            _timer = 0f;
            SimTelemetry.Instance?.Interaction(ActorName, Carried != null ? Carried.name : "?", "place_begin");
        }

        void CompleteHandoff()
        {
            SimTelemetry.Instance?.Handoff(ActorName, _handoffPartner.name,
                $"released_after_s={HandoffPause:F2}");
            Current = Phase.Placing;
            CurrentAction = "place";
            _timer = 0f;
        }

        void CompletePlace()
        {
            if (Carried != null)
            {
                Carried.SetParent(null, true);
                Carried.position = _dest.position + Vector3.up * 0.45f;
                Carried.rotation = Quaternion.identity;
                var rb = Carried.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;

                SimTelemetry.Instance?.Interaction(ActorName, Carried.name, "place_complete");
                Carried = null;
            }

            Current = Phase.Done;
            CurrentAction = "idle";
            SimTelemetry.Instance?.GameplayEvent(ActorName, _taskLabel, "task_complete");
            OnTaskComplete?.Invoke(this);
        }

        bool Tick(float duration)
        {
            Decelerate();
            _timer += Time.deltaTime;
            return _timer >= duration;
        }

        // ── steering ────────────────────────────────────────────────────────────

        /// <summary>Move toward target. Returns true on arrival.</summary>
        bool Steer(Vector3 target)
        {
            Vector3 flat = new Vector3(target.x, transform.position.y, target.z);
            Vector3 toTarget = flat - transform.position;

            if (toTarget.magnitude <= ArriveRadius)
            {
                Decelerate();
                return true;
            }

            Vector3 desired = toTarget.normalized + Avoidance() * AvoidStrength;
            desired = desired.normalized;

            // Slow into the target so arrival isn't a hard stop — reads as deliberate,
            // and keeps the speed trace realistic.
            float slow = Mathf.Clamp01(toTarget.magnitude / (ArriveRadius * 4f));
            Vector3 wanted = desired * MaxSpeed * Mathf.Max(0.25f, slow);

            _velocity = Vector3.MoveTowards(_velocity, wanted, Accel * Time.deltaTime);
            transform.position += _velocity * Time.deltaTime;

            if (_velocity.sqrMagnitude > 0.001f)
            {
                Quaternion want = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, want, TurnRate * Time.deltaTime);
            }

            CurrentSpeed = _velocity.magnitude;
            CurrentAction = Carried != null ? "carry" : "walk";
            return false;
        }

        void Decelerate()
        {
            _velocity = Vector3.MoveTowards(_velocity, Vector3.zero, Accel * 2f * Time.deltaTime);
            transform.position += _velocity * Time.deltaTime;
            CurrentSpeed = _velocity.magnitude;
        }

        /// <summary>Three whiskers; returns a lateral push away from anything close.</summary>
        Vector3 Avoidance()
        {
            Vector3 push = Vector3.zero;
            Vector3 origin = transform.position + Vector3.up * 0.6f;

            for (int i = -1; i <= 1; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 32f, 0f) * transform.forward;
                if (Physics.Raycast(origin, dir, out RaycastHit hit, LookAhead, ObstacleMask,
                                    QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                    float weight = 1f - (hit.distance / LookAhead);
                    push -= new Vector3(dir.x, 0f, dir.z) * weight;
                    push += Vector3.Cross(Vector3.up, dir) * weight * 0.8f;
                }
            }
            return push;
        }

        // ── zone tracking, so zoneIdentifierAccesses actually fills ─────────────

        void OnTriggerEnter(Collider other)
        {
            var z = other.GetComponent<SimZone>();
            if (z == null || _zonesInside.Contains(z.ZoneName)) return;
            _zonesInside.Add(z.ZoneName);
            SimTelemetry.Instance?.ZoneAccess(ActorName, z.ZoneName, true);
        }

        void OnTriggerExit(Collider other)
        {
            var z = other.GetComponent<SimZone>();
            if (z == null || !_zonesInside.Contains(z.ZoneName)) return;
            _zonesInside.Remove(z.ZoneName);
            SimTelemetry.Instance?.ZoneAccess(ActorName, z.ZoneName, false);
        }
    }

    /// <summary>Marks a trigger volume as a named zone. The Unity analogue of EventVolume.</summary>
    public class SimZone : MonoBehaviour
    {
        public string ZoneName = "zone";
    }
}
