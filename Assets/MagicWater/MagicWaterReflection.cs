using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MagicWater
{
    public class MagicWaterReflection : MonoBehaviour
    {
        [SerializeField] [Range(0.25f, 1f)] private float _resolutionScale = 0.5f;
        [SerializeField] private LayerMask _reflectionLayers = ~0;
        [SerializeField] private float _clipPlaneOffset = 0.05f;

        private static readonly int REFLECTION_TEX_ID = Shader.PropertyToID("_MagicWaterReflectionTex");

        private Camera _reflectionCamera;
        private RenderTexture _texture;

        private void OnEnable()
        {
            RenderPipelineManager.beginContextRendering += OnBeginFrame;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginContextRendering -= OnBeginFrame;
            _texture.Release();
            Destroy(_texture);
            if (_reflectionCamera) Destroy(_reflectionCamera.gameObject);
        }

        private void OnBeginFrame(ScriptableRenderContext arg1, List<Camera> arg2)
        {
            var main = Camera.main;
            if (!main) return;

            EnsureResources(main);

            var normal = transform.up;
            var surface = transform.position;
            var camPos = main.transform.position;

            var distance = Vector3.Dot(camPos - surface, normal);
            var reflectionPos = camPos - 2f * distance * normal;
            var reflectionForward = Vector3.Reflect(main.transform.forward, normal);
            var reflectedUp = Vector3.Reflect(main.transform.up, normal);

            _reflectionCamera.transform.SetLocalPositionAndRotation(reflectionPos,
                Quaternion.LookRotation(reflectionForward, reflectedUp));

            _reflectionCamera.fieldOfView = main.fieldOfView;
            _reflectionCamera.nearClipPlane = main.nearClipPlane;
            _reflectionCamera.farClipPlane = main.farClipPlane;
            _reflectionCamera.aspect = main.aspect;
            _reflectionCamera.ResetProjectionMatrix();

            var clipPlane = GetCameraSpacePlane(_reflectionCamera, surface + normal * _clipPlaneOffset, normal);
            _reflectionCamera.projectionMatrix = _reflectionCamera.CalculateObliqueMatrix(clipPlane);
            _reflectionCamera.cullingMask = _reflectionLayers.value & ~(1 << gameObject.layer); //do not reflect itself
        }

        private void EnsureResources(Camera mainCamera)
        {
            var width = (int)Mathf.Max(64, mainCamera.pixelWidth * _resolutionScale);
            var height = (int)Mathf.Max(64, mainCamera.pixelHeight * _resolutionScale);

            if (_texture == null || _texture.width != width || _texture.height != height)
            {
                SetupTexture(width, height);
            }

            if (_reflectionCamera == null)
            {
                SetupReflectionCamera(mainCamera);
            }

            _reflectionCamera.targetTexture = _texture;
        }

        private void SetupReflectionCamera(Camera mainCamera)
        {
            var go = new GameObject("MagicWater Reflection Camera")
            {
                hideFlags = HideFlags.DontSave
            };

            _reflectionCamera = go.AddComponent<Camera>();
            _reflectionCamera.targetTexture = _texture;
            _reflectionCamera.depth = mainCamera.depth - 10;

            _reflectionCamera.clearFlags = mainCamera.clearFlags;
            _reflectionCamera.backgroundColor = mainCamera.backgroundColor;

            var data = _reflectionCamera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false; //skip bloom
            data.renderShadows = false; // not visible in reflection
            data.requiresDepthTexture = false;
            data.requiresColorTexture = false;
        }

        private void SetupTexture(int width, int height)
        {
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }

            _texture = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR)
            {
                name = "MagicWater Reflection"
            };

            Shader.SetGlobalTexture(REFLECTION_TEX_ID, _texture);
        }

        private Vector4 GetCameraSpacePlane(Camera cam, Vector3 point, Vector3 normal)
        {
            var m = cam.worldToCameraMatrix;
            var cPoint = m.MultiplyPoint(point);
            var cNormal = m.MultiplyVector(normal).normalized;

            return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPoint, cNormal));
        }
    }
}