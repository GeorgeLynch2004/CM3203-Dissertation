/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.XR;
using Debug = UnityEngine.Debug;
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("meta.xr.mrutilitykit")]

namespace Meta.XR.EnvironmentDepth
{
    /// <summary>
    /// Surfaces _EnvironmentDepthTexture and complementary information
    /// for reprojection and movement compensation to shaders globally.
    /// </summary>
    /// <remarks>
    /// Enabling this class will allow you to make depth textures available from the system asynchronously.
    /// For more information, see [Depth API Overview](https://developers.meta.com/horizon/documentation/unity/unity-depthapi-overview).
    /// <example> Enabling depth textures:
    /// <code><![CDATA[
    /// [SerializeField] private EnvironmentDepthManager _environmentDepthManager;
    /// private IEnumerator Example()
    /// {
    ///     //Check if this feature is supported on your platform
    ///     if(!EnvironmentDepthManager.IsSupported)
    ///     {
    ///         Debug.Log("This feature is not supported");
    ///         yield break;
    ///     }
    ///     //enables the feature and makes depth textures available
    ///     //depth textures will be available from the system asynchronously
    ///     _environmentDepthManager.enabled = true;
    ///
    ///     while(!_environmentDepthManager.IsDepthAvailable)
    ///         yield return null;
    ///
    ///     Debug.Log("Depth textures are now available);
    /// }
    /// ]]></code></example>
    /// <example> Using Depth Manager features:
    /// <code><![CDATA[
    /// private void ExampleOccl()
    /// {
    ///   //sets occlusion mode to "SoftOcclusion" -- this is the default value
    ///   _environmentDepthManager.OcclusionShadersMode = OcclusionShadersMode.SoftOcclusion;
    ///
    ///  //sets occlusion mode to "HardOcclusion"
    ///  _environmentDepthManager.OcclusionShadersMode = OcclusionShadersMode.HardOcclusion;
    ///
    ///  //sets occlusion mode to "None" -- it's a good idea to disable environmentDepthManager to save resources in this case
    ///  _environmentDepthManager.OcclusionShadersMode = OcclusionShadersMode.None;
    ///
    ///  //remove hands from the depth map
    ///  _environmentDepthManager.RemoveHands = true;
    ///
    ///  //disables the feature. Frees up resources by not requesting depth textures from the system
    ///  _environmentDepthManager.enabled = false;
    /// }
    /// ]]></code></example>
    /// </remarks>
    public class EnvironmentDepthManager : MonoBehaviour
    {
        /// <summary>
        /// The shader keyword for enabling hard occlusions.
        /// </summary>

        public const string HardOcclusionKeyword = "HARD_OCCLUSION";
        /// <summary>
        /// The shader keyword for enabling soft occlusions.
        /// </summary>

        public const string SoftOcclusionKeyword = "SOFT_OCCLUSION";
        private const int numViews = 2;
        private static readonly int DepthTextureID = Shader.PropertyToID("_EnvironmentDepthTexture");
        private static readonly int ReprojectionMatricesID = Shader.PropertyToID("_EnvironmentDepthReprojectionMatrices");
        private static readonly int ZBufferParamsID = Shader.PropertyToID("_EnvironmentDepthZBufferParams");
        private static readonly int PreprocessedEnvironmentDepthTexture = Shader.PropertyToID("_PreprocessedEnvironmentDepthTexture");

        private static readonly int MvpMatricesID = Shader.PropertyToID("_DepthMask_MVP_Matrices");
        private static readonly int MaskTextureID = Shader.PropertyToID("_MaskTexture");
        private static readonly int MaskBiasID = Shader.PropertyToID("_MaskBias");

        [SerializeField] private OcclusionShadersMode _occlusionShadersMode = OcclusionShadersMode.SoftOcclusion;
        [SerializeField] private bool _removeHands;
        /// <summary>
        /// This transform allows you to override the default tracking space.
        /// </summary>
        [SerializeField] public Transform CustomTrackingSpace;
        /// <summary>
        /// A list of mesh filters to be used with the Depth Masking feature. If this list is left empty, the feature is disabled.
        /// </summary>
        [field: SerializeField] public List<MeshFilter> MaskMeshFilters { get; set; } = new List<MeshFilter>();

        private bool _isCameraRigCached;
        [SerializeField, HideInInspector] private OVRCameraRig _cameraRig;
        internal static readonly IDepthProvider _provider = CreateProvider();
        private bool _hasPermission;
        private uint? _prevTextureId;
        private Material _preprocessMaterial;
        [CanBeNull] private RenderTexture _preprocessTexture;
        private RenderTargetSetup _preprocessRenderTargetSetup;

        private float _maskBias = 0.1f;
        private Mask _mask;

        [NotNull]
        private static IDepthProvider CreateProvider()
        {
#if DEPTH_API_SUPPORTED
            return new DepthProvider();
#endif
#pragma warning disable CS0162 // Unreachable code detected
            return new DepthProviderNotSupported();
#pragma warning restore CS0162
        }

        /// <summary>
        /// Returns true if the current platform supports Environment Depth.
        /// </summary>
        public static bool IsSupported => _provider.IsSupported;

        /// <summary>
        /// Returns true if depth textures were made available from the system.
        /// </summary>
        public bool IsDepthAvailable { get; private set; }

        /// <summary>
        /// If <see cref="OcclusionShadersMode"/> is specified, this component will enable a global shader keyword after receiving the depth texture.<br/>
        /// To enable per-object occlusion, use a provided occlusion shader or modify your custom shader to support <see cref="HardOcclusionKeyword"/> or <see cref="SoftOcclusionKeyword"/>.
        /// </summary>
        public OcclusionShadersMode OcclusionShadersMode
        {
            get => _occlusionShadersMode;
            set
            {
                if (_occlusionShadersMode == value)
                    return;
                _occlusionShadersMode = value;
                if (IsDepthAvailable)
                    SetOcclusionShaderKeywords(value);
            }
        }

        /// <summary>
        /// If set to true, hands will be removed from the depth texture.
        /// </summary>
        /// <remarks>
        /// For more information, see [Hands Removal](https://developers.meta.com/horizon/documentation/unity/unity-depthapi-hands-removal).
        /// </remarks>
        public bool RemoveHands
        {
            get => _removeHands;
            set
            {
                if (_removeHands == value)
                    return;
                _removeHands = value;
                if (enabled)
                    _provider.RemoveHands = value;
            }
        }

        /// <summary>
        /// When using the Depth Mask feature, the world position of the environment depth and the world position of the meshes used in the masking process are compared, and the closest value will be taken. To avoid
        /// z-fighting, adjust this value to move the meshes closer or further.
        /// </summary>
        public float MaskBias
        {
            get => _maskBias;
            set
            {
                _maskBias = value;
                if (_mask != null)
                {
                    _mask._maskMaterial.SetFloat(MaskBiasID, value);
                }
            }
        }

        private readonly Matrix4x4[] _reprojectionMatrices = new Matrix4x4[2];
        private XRDisplaySubsystem _xrDisplay;

        private void Awake()
        {
            Assert.AreEqual(1, FindObjectsByType<EnvironmentDepthManager>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length,
                $"Environment Depth: more than one {nameof(EnvironmentDepthManager)} component. Only one instance is allowed at a time. Current instance: {name}");
#if !UNITY_2022_3_OR_NEWER
            Debug.LogError("DepthAPI requires at least Unity 2022.3.0f");
#endif
            if (!IsSupported)
            {
#if UNITY_EDITOR_WIN
                Debug.LogError("Environment Depth could not be retrieved! Please ensure the following:" +
                    "\n\n" +
                    "When running over Link, the spatial data feature needs to be enabled in the Meta Quest Link app.\n" +
                    " (Settings > Beta > Spatial Data over Meta Quest Link)." +
                    "\n\n" +
                    "Check the Project Setup Tool for any project related issues.\n" +
                    " (Oculus > Tools > Project Setup Tool" +
                    "\n\n" +
                    "You are using a Quest 3 or newer device.");
#endif
                return;
            }

            var displays = new List<XRDisplaySubsystem>(1);
            SubsystemManager.GetInstances(displays);
            _xrDisplay = displays.Single();
            Assert.IsNotNull(_xrDisplay, nameof(_xrDisplay));

            const string shaderName = "Meta/EnvironmentDepth/Preprocessing";
            var shader = Shader.Find(shaderName);
            Assert.IsNotNull(shader, "Depth preprocessing shader is not present in the Resources folder: " + shaderName);
            _preprocessMaterial = new Material(shader);
        }

        private void OnEnable()
        {
            if (!IsSupported)
            {
                Debug.LogError($"Environment Depth is not supported. Please check {nameof(EnvironmentDepthManager)}.{nameof(IsSupported)} before enabling {nameof(EnvironmentDepthManager)}.\n" +
                                            "Open 'Oculus -> Tools -> Project Setup Tool' to see requirements.\n");
                enabled = false;
                return;
            }

            _hasPermission = Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission);
            if (_hasPermission)
                _provider.SetDepthEnabled(true, _removeHands);
            else Log(LogType.Warning, $"Environment Depth requires {OVRPermissionsRequester.ScenePermission} permission. Waiting for permission...");
        }

        private void ResetDepthTextureIfAvailable()
        {
            if (IsDepthAvailable)
            {
                IsDepthAvailable = false;
                Shader.SetGlobalTexture(DepthTextureID, null);
                if (_occlusionShadersMode != OcclusionShadersMode.None)
                    SetOcclusionShaderKeywords(OcclusionShadersMode.None);
            }
        }

        private void OnDisable()
        {
            ResetDepthTextureIfAvailable();
            if (IsSupported && _hasPermission)
                _provider.SetDepthEnabled(false, false);
        }

        private void OnDestroy()
        {
            if (_preprocessMaterial != null)
                Destroy(_preprocessMaterial);
            if (_preprocessTexture != null)
                Destroy(_preprocessTexture);
            _mask?.Dispose();
        }

        private void Update()
        {
            if (!_hasPermission)
            {
                if (!Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission))
                    return;
                _hasPermission = true;
                _provider.SetDepthEnabled(true, _removeHands);
            }

            var trackingSpaceWorldToLocal = GetTrackingSpaceWorldToLocalMatrix();
            TryFetchDepthTexture(trackingSpaceWorldToLocal);
            if (!IsDepthAvailable)
                return;

            // Calculate Environment Depth Camera parameters
            // Assume NearZ and FarZ are the same for left and right eyes
            var leftEyeData = _provider.GetFrameDesc(0);
            var rightEyeData = _provider.GetFrameDesc(1);
            var depthZBufferParams = EnvironmentDepthUtils.ComputeNdcToLinearDepthParameters(leftEyeData.nearZ, leftEyeData.farZ);
            Shader.SetGlobalVector(ZBufferParamsID, depthZBufferParams);

            _reprojectionMatrices[0] = EnvironmentDepthUtils.CalculateReprojection(leftEyeData) * trackingSpaceWorldToLocal;
            _reprojectionMatrices[1] = EnvironmentDepthUtils.CalculateReprojection(rightEyeData) * trackingSpaceWorldToLocal;
            Shader.SetGlobalMatrixArray(ReprojectionMatricesID, _reprojectionMatrices);
        }

#if UNITY_EDITOR
        private void Reset() => CacheCameraRig();
        private void OnValidate() => CacheCameraRig();
#endif

        private void CacheCameraRig()
        {
            if (_cameraRig == null)
                _cameraRig = FindObjectOfType<OVRCameraRig>();
        }

        private static void SetOcclusionShaderKeywords(OcclusionShadersMode mode)
        {
            switch (mode)
            {
                case OcclusionShadersMode.HardOcclusion:
                    Shader.DisableKeyword(SoftOcclusionKeyword);
                    Shader.EnableKeyword(HardOcclusionKeyword);
                    break;
                case OcclusionShadersMode.SoftOcclusion:
                    Shader.DisableKeyword(HardOcclusionKeyword);
                    Shader.EnableKeyword(SoftOcclusionKeyword);
                    break;
                case OcclusionShadersMode.None:
                    Shader.DisableKeyword(HardOcclusionKeyword);
                    Shader.DisableKeyword(SoftOcclusionKeyword);
                    break;
                default:
                    Debug.LogError($"Environment Depth: unknown {nameof(EnvironmentDepth.OcclusionShadersMode)} {mode}");
                    break;
            }
        }

        private void TryFetchDepthTexture(Matrix4x4 trackingSpaceWorldToLocal)
        {
            uint textureId = 0;
            if (!_xrDisplay.running || !_provider.GetDepthTextureId(ref textureId))
                return;

#if UNITY_2022_3_OR_NEWER
            var depthTexture = _xrDisplay.GetRenderTexture(textureId);
#else
            RenderTexture depthTexture = null;
#endif
            if (depthTexture == null) // can be null when the headset is awaking from sleep
            {
                ResetDepthTextureIfAvailable();
                return;
            }

            if (_prevTextureId == textureId)
                return;
            _prevTextureId = textureId;

            Assert.IsTrue(depthTexture.IsCreated(), "depthTexture.IsCreated()");
            if (MaskMeshFilters != null && MaskMeshFilters.Count > 0)
            {
                _mask ??= new Mask(depthTexture.width, depthTexture.height, _maskBias);
                depthTexture = _mask.ApplyMask(depthTexture, MaskMeshFilters, trackingSpaceWorldToLocal);
            }
            Shader.SetGlobalTexture(DepthTextureID, depthTexture);
            if (!IsDepthAvailable)
            {
                IsDepthAvailable = true;
                if (_occlusionShadersMode != OcclusionShadersMode.None)
                    SetOcclusionShaderKeywords(_occlusionShadersMode);
            }

            if (_occlusionShadersMode == OcclusionShadersMode.SoftOcclusion)
                PreprocessDepthTexture(depthTexture);
        }

        internal Matrix4x4 GetTrackingSpaceWorldToLocalMatrix()
        {
            if (CustomTrackingSpace != null)
            {
                return CustomTrackingSpace.worldToLocalMatrix;
            }
            if (!_isCameraRigCached)
            {
                _isCameraRigCached = true;
                CacheCameraRig();
            }
            return _cameraRig != null ? _cameraRig.trackingSpace.worldToLocalMatrix : Matrix4x4.identity;
        }

        private class Mask
        {
            internal readonly Material _maskMaterial;
            private readonly RenderTexture _maskDepthRt;
            private readonly RenderTexture _maskedDepthTexture;
            private readonly CommandBuffer _maskCommandBuffer;
            private readonly Matrix4x4[] _mvpMatrices = new Matrix4x4[2];

            internal Mask(int width, int height, float bias)
            {
                const string shaderName = "Meta/EnvironmentDepth/DepthMask";
                var shader = Shader.Find(shaderName);
                Assert.IsNotNull(shader, "Shader named '" + shaderName + "' wasn't found in Resources folder.");
                _maskMaterial = new Material(shader)
                {
                    enableInstancing = true
                };
                _maskMaterial.SetFloat(MaskBiasID, bias);
                _maskDepthRt = new RenderTexture(width, height, GraphicsFormat.R16_UNorm, GraphicsFormat.D16_UNorm)
                {
                    dimension = TextureDimension.Tex2DArray,
                    volumeDepth = numViews
                };
                _maskedDepthTexture = new RenderTexture(width, height, GraphicsFormat.R16_UNorm, GraphicsFormat.None)
                {
                    dimension = TextureDimension.Tex2DArray,
                    volumeDepth = numViews,
                    depth = 0
                };
                _maskCommandBuffer = new CommandBuffer();
            }

            internal RenderTexture ApplyMask(RenderTexture depthTexture, List<MeshFilter> meshFilters, Matrix4x4 trackingSpaceWorldToLocal)
            {
                // update depth camera proj and view matrices
                EnvironmentDepthUtils.CalculateDepthCameraMatrices(_provider.GetFrameDesc(0), out var proj0, out var view0);
                EnvironmentDepthUtils.CalculateDepthCameraMatrices(_provider.GetFrameDesc(1), out var proj1, out var view1);

                // render mask's depth into _maskDepthRt
                _maskCommandBuffer.SetRenderTarget(new RenderTargetIdentifier(_maskDepthRt, 0, CubemapFace.Unknown, -1),
                    colorLoadAction: RenderBufferLoadAction.DontCare, colorStoreAction: RenderBufferStoreAction.Store,
                    depthLoadAction: RenderBufferLoadAction.DontCare, depthStoreAction: RenderBufferStoreAction.DontCare
                );
                _maskCommandBuffer.ClearRenderTarget(true, true, Color.white);
                foreach (var meshFilter in meshFilters)
                {
                    if (meshFilter == null || meshFilter.sharedMesh == null)
                    {
                        Debug.LogError($"{nameof(MeshFilter)} or {nameof(MeshFilter.sharedMesh)} is null.");
                        continue;
                    }
                    _mvpMatrices[0] = GL.GetGPUProjectionMatrix(proj0, true) * view0 * trackingSpaceWorldToLocal * meshFilter.transform.localToWorldMatrix;
                    _mvpMatrices[1] = GL.GetGPUProjectionMatrix(proj1, true) * view1 * trackingSpaceWorldToLocal * meshFilter.transform.localToWorldMatrix;
                    _maskCommandBuffer.SetGlobalMatrixArray(MvpMatricesID, _mvpMatrices);
                    _maskCommandBuffer.DrawMeshInstancedProcedural(meshFilter.sharedMesh, 0, _maskMaterial, 0, numViews);
                }

                // combine mask with the depth texture
                _maskMaterial.SetTexture(DepthTextureID, depthTexture);
                _maskMaterial.SetTexture(MaskTextureID, _maskDepthRt);
                _maskCommandBuffer.SetRenderTarget(new RenderTargetIdentifier(_maskedDepthTexture, 0, CubemapFace.Unknown, -1),
                    colorLoadAction: RenderBufferLoadAction.DontCare, colorStoreAction: RenderBufferStoreAction.Store,
                    depthLoadAction: RenderBufferLoadAction.DontCare, depthStoreAction: RenderBufferStoreAction.DontCare);
                _maskCommandBuffer.DrawProcedural(Matrix4x4.identity, _maskMaterial, 1, MeshTopology.Triangles, 3, numViews);
                Graphics.ExecuteCommandBuffer(_maskCommandBuffer);
                _maskCommandBuffer.Clear();
                return _maskedDepthTexture;
            }

            internal void Dispose()
            {
                Destroy(_maskMaterial);
                Destroy(_maskDepthRt);
                Destroy(_maskedDepthTexture);
                _maskCommandBuffer.Dispose();
            }
        }

        private void PreprocessDepthTexture(RenderTexture depthTexture)
        {
            if (_preprocessTexture == null)
            {
                _preprocessTexture = new RenderTexture(depthTexture.width, depthTexture.height, GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormat.None)
                {
                    dimension = TextureDimension.Tex2DArray,
                    volumeDepth = numViews,
                    name = nameof(_preprocessTexture),
                    depth = 0
                };
                _preprocessTexture.Create();
                Shader.SetGlobalTexture(PreprocessedEnvironmentDepthTexture, _preprocessTexture);

                _preprocessRenderTargetSetup = new RenderTargetSetup
                {
                    color = new[] { _preprocessTexture.colorBuffer },
                    depth = _preprocessTexture.depthBuffer,
                    depthSlice = -1,
                    colorLoad = new[] { RenderBufferLoadAction.DontCare },
                    colorStore = new[] { RenderBufferStoreAction.Store },
                    depthLoad = RenderBufferLoadAction.DontCare,
                    depthStore = RenderBufferStoreAction.DontCare,
                    mipLevel = 0,
                    cubemapFace = CubemapFace.Unknown
                };
            }

            Graphics.SetRenderTarget(_preprocessRenderTargetSetup);
            _preprocessMaterial.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Triangles, 3, numViews);
        }

        [Conditional("UNITY_ASSERTIONS")]
        private static void Log(LogType type, string msg) => Debug.unityLogger.Log(type, msg);
    }

    internal interface IDepthProvider
    {
        bool IsSupported { get; }
        bool RemoveHands { set; }
        void SetDepthEnabled(bool isEnabled, bool removeHands);
        DepthFrameDesc GetFrameDesc(int eye);
        bool GetDepthTextureId(ref uint textureId);
    }

    internal class DepthProviderNotSupported : IDepthProvider
    {
        public bool IsSupported => false;
        public bool RemoveHands
        {
            set { }
        }
        public void SetDepthEnabled(bool isEnabled, bool removeHands) { }
        public DepthFrameDesc GetFrameDesc(int eye) => throw new NotSupportedException();
        public bool GetDepthTextureId(ref uint textureId) => throw new NotSupportedException();
    }

    internal struct DepthFrameDesc
    {
        internal Vector3 createPoseLocation;
        internal Vector4 createPoseRotation;
        internal float fovLeftAngle;
        internal float fovRightAngle;
        internal float fovTopAngle;
        internal float fovDownAngle;
        internal float nearZ;
        internal float farZ;
    }
}
