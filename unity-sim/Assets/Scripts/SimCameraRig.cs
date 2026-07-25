using UnityEngine;

namespace ProjectScim
{
    /// <summary>
    /// Camera for a judge who has never seen this before: sensible presets on 1/2/3,
    /// free look on right-drag, WASD to fly. Nothing to learn, nothing to get stuck in.
    /// </summary>
    public class SimCameraRig : MonoBehaviour
    {
        public enum Preset { Branches, Warehouse, Follow }

        public Transform FollowTarget;
        public float FlySpeed = 14f;
        public float LookSensitivity = 2.6f;
        public float SmoothTime = 0.45f;

        Vector3 _targetPos;
        Quaternion _targetRot;
        Vector3 _vel;
        Preset _preset = Preset.Branches;
        float _yaw, _pitch;
        bool _manual;

        public static SimCameraRig Create()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.farClipPlane = 500f;
            cam.backgroundColor = new Color(0.09f, 0.10f, 0.13f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            var rig = cam.gameObject.GetComponent<SimCameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<SimCameraRig>();
            return rig;
        }

        public void Focus(Preset p)
        {
            _preset = p;
            _manual = false;

            switch (p)
            {
                case Preset.Branches:
                    // Framed on the three branches, close enough that the shelf colour
                    // reads. Sits left of centre so the HUD panel doesn't cover Downtown.
                    _targetPos = new Vector3(6f, 26f, -22f);
                    _targetRot = Quaternion.Euler(46f, -8f, 0f);
                    break;

                case Preset.Warehouse:
                    _targetPos = MiseScenario.WarehouseOrigin + new Vector3(4f, 14f, -18f);
                    _targetRot = Quaternion.Euler(32f, -6f, 0f);
                    break;

                case Preset.Follow:
                    // Position resolves per-frame in LateUpdate.
                    break;
            }

            var e = _targetRot.eulerAngles;
            _pitch = e.x; _yaw = e.y;
        }

        void LateUpdate()
        {
            HandleManualInput();

            if (_preset == Preset.Follow && FollowTarget != null && !_manual)
            {
                // Trail behind and above — close enough to read the pick and place.
                Vector3 behind = FollowTarget.position
                                 - FollowTarget.forward * 7.5f
                                 + Vector3.up * 4.6f;
                _targetPos = behind;
                Vector3 look = FollowTarget.position + Vector3.up * 1.1f;
                _targetRot = Quaternion.LookRotation((look - behind).normalized, Vector3.up);
            }

            transform.position = Vector3.SmoothDamp(transform.position, _targetPos, ref _vel, SmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRot,
                                                  1f - Mathf.Exp(-6f * Time.deltaTime));
        }

        void HandleManualInput()
        {
            // Right-drag to look. Taking manual control detaches from the preset.
            if (Input.GetMouseButton(1))
            {
                _manual = true;
                _yaw += Input.GetAxis("Mouse X") * LookSensitivity;
                _pitch = Mathf.Clamp(_pitch - Input.GetAxis("Mouse Y") * LookSensitivity, -85f, 85f);
                _targetRot = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += transform.forward;
            if (Input.GetKey(KeyCode.S)) move -= transform.forward;
            if (Input.GetKey(KeyCode.A)) move -= transform.right;
            if (Input.GetKey(KeyCode.D)) move += transform.right;
            if (Input.GetKey(KeyCode.E)) move += Vector3.up;
            if (Input.GetKey(KeyCode.Q)) move -= Vector3.up;

            if (move.sqrMagnitude > 0.001f)
            {
                _manual = true;
                float boost = Input.GetKey(KeyCode.LeftShift) ? 2.6f : 1f;
                _targetPos += move.normalized * FlySpeed * boost * Time.deltaTime;
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _manual = true;
                _targetPos += transform.forward * scroll * 40f;
            }
        }
    }
}
