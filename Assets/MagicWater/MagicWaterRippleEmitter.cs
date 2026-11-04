using System;
using UnityEngine;

namespace MagicWater
{
    public class MagicWaterRippleEmitter : MonoBehaviour
    {
        [SerializeField] private Renderer _water;
        [SerializeField] private float _maxDistanceFromSurface = 0.5f;
        [SerializeField] private float _idleInterval = 2f;

        [Header("Steps' settings")]
        [SerializeField]
        private float _stepDistance = 0.8f;

        [SerializeField] private float _footSpacing = 0.12f;
        [SerializeField] private float _forwardOffset = 0.3f;

        private Vector3 _last;
        private float _idleTime;
        private bool _inWater;
        private bool _leftFoot = true;
        private Vector3 _prevPos;

        private void Update()
        {
            if (_water == null) return;

            var p = transform.position;
            var waterY = _water.transform.position.y;
            var b = _water.bounds;
            var wasInWater = _inWater;

            _inWater = IsInBounds(p, b) && Mathf.Abs(p.y - waterY) < _maxDistanceFromSurface;

            if (!_inWater)
            {
                _leftFoot = true;
                return;
            }

            var onSurface = new Vector3(p.x, waterY, p.z);

            var stopped = (onSurface - _prevPos).sqrMagnitude < 0.000001f; // brak ruchu w tej klatce
            _prevPos = onSurface;
            if (stopped) _leftFoot = true;

            if (!wasInWater)
            {
                _last = onSurface;
                MagicWaterRipples.Add(onSurface);
                return;
            }

            var moved = onSurface - _last;
            if (moved.sqrMagnitude >= _stepDistance * _stepDistance)
            {
                var right = Vector3.Cross(Vector3.up, moved.normalized) * _footSpacing;
                var forward = moved.normalized * _forwardOffset;

                MagicWaterRipples.Add(onSurface + forward + (_leftFoot ? -right : right));

                _leftFoot = !_leftFoot;
                _last = onSurface;
                _idleTime = 0;
                return;
            }

            if (!(_idleInterval > 0)) return;

            _idleTime += Time.deltaTime;
            if (_idleTime >= _idleInterval)
            {
                MagicWaterRipples.Add(onSurface);
                _idleTime = 0;
            }
        }

        private static bool IsInBounds(Vector3 p, Bounds b)
        {
            return p.x > b.min.x
                   && p.x < b.max.x
                   && p.z > b.min.z
                   && p.z < b.max.z;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_water == null) return;

            var p = transform.position;
            var waterY = _water.transform.position.y;
            var onSurface = new Vector3(p.x, waterY, p.z);
            var inWater = IsInBounds(p, _water.bounds)
                          && Mathf.Abs(p.y - waterY) < _maxDistanceFromSurface;
            var up = Vector3.up * _maxDistanceFromSurface;

            // 1. Strefa wysokości (Max Distance From Surface)
            UnityEditor.Handles.color = inWater ? Color.green : Color.red;
            UnityEditor.Handles.DrawLine(onSurface - up, onSurface + up);
            UnityEditor.Handles.DrawWireDisc(onSurface + up, Vector3.up, 0.3f);
            UnityEditor.Handles.DrawWireDisc(onSurface - up, Vector3.up, 0.3f);

            // 2. Odległość kroku (Step Distance), liczona od ostatniego kręgu
            var stepCenter = Application.isPlaying && _inWater ? _last : onSurface;
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(stepCenter, Vector3.up, _stepDistance);

            // 3. Maksymalny zasięg kręgu z materiału (RippleSpeed x RippleLifetime)
            var mat = _water.sharedMaterial;
            if (mat != null && mat.HasFloat("_RippleSpeed") && mat.HasFloat("_RippleLifetime"))
            {
                var maxRadius = mat.GetFloat("_RippleSpeed") * mat.GetFloat("_RippleLifetime");
                UnityEditor.Handles.color = new Color(0.8f, 0.4f, 1f);
                UnityEditor.Handles.DrawWireDisc(onSurface, Vector3.up, maxRadius);
            }

            // 4. Gdzie powstaną kręgi kroków (Forward Offset + Foot Spacing)
            //    Kierunek: przód postaci spłaszczony do poziomu
            var forwardDir = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, forwardDir) * _footSpacing;
            var stepPoint = onSurface + forwardDir * _forwardOffset;

            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.DrawLine(onSurface, stepPoint); // przesunięcie do przodu
            Gizmos.color = new Color(1f, 0.6f, 0f); // pomarańczowa = lewa
            Gizmos.DrawSphere(stepPoint - right, 0.05f);
            Gizmos.color = Color.yellow; // żółta = prawa
            Gizmos.DrawSphere(stepPoint + right, 0.05f);

            // 5. Licznik postoju (Idle Interval)
            if (Application.isPlaying && _idleInterval > 0)
            {
                UnityEditor.Handles.Label(p + Vector3.up * 2f,
                    $"Idle: {_idleTime:0.0} / {_idleInterval:0.0} s");
            }
        }
#endif
    }
}