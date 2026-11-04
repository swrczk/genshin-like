using System;
using UnityEngine;

namespace MagicWater
{
    public class MagicWaterRippleEmitter : MonoBehaviour
    {
        [SerializeField] private Renderer _water;
        [SerializeField] private float _stepDistance = 0.8f;
        [SerializeField] private float _maxDistanceFromSurface = 0.5f;
        [SerializeField] private float _idleInterval = 2f;
        [SerializeField] private float _footSpacing = 0.12f;

        private Vector3 _last;
        private float _idleTime;
        private bool _inWater;
        private bool _leftFoot;

        private void Update()
        {
            if (_water == null) return;

            var p = transform.position;
            var waterY = _water.transform.position.y;
            var b = _water.bounds;
            var wasInWater = _inWater;

            _inWater = IsInBounds(p, b) && Mathf.Abs(p.y - waterY) < _maxDistanceFromSurface;

            if (!_inWater) return;

            var onSurface = new Vector3(p.x, waterY, p.z);
            if (!wasInWater)
            {
                _last = onSurface;
                MagicWaterRipples.Add(onSurface);
                return;
            }

            var moved = onSurface - _last;
            if (moved.sqrMagnitude >= _stepDistance * _stepDistance)
            {
                var dir = Vector3.Cross(Vector3.up, moved.normalized)
                          * (_leftFoot ? _footSpacing : -_footSpacing);
                _leftFoot = !_leftFoot;
                MagicWaterRipples.Add(onSurface + dir);
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
    }
}