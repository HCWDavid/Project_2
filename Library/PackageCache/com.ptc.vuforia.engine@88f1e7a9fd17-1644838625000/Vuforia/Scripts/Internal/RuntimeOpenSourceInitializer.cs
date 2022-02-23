/*===============================================================================
Copyright (c) 2019 PTC Inc. All Rights Reserved.

Confidential and Proprietary - Protected under copyright and other laws.
Vuforia is a trademark of PTC Inc., registered in the United States and other 
countries.
===============================================================================*/

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;
#if PLATFORM_ANDROID
using UnityEngine.Android;
#endif

#if UNITY_WSA && OPEN_XR_ENABLED
using Microsoft.MixedReality.OpenXR;
using UnityEngine.XR.Management;
#endif

namespace Vuforia.UnityRuntimeCompiled
{
    public class RuntimeOpenSourceInitializer
    {
        static IUnityRuntimeCompiledFacade sFacade;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            InitializeFacade();
        }

        static void InitializeFacade()
        {
            if (sFacade != null) return;

            sFacade = new OpenSourceUnityRuntimeCompiledFacade();
            UnityRuntimeCompiledFacade.Instance = sFacade;
        }

        class OpenSourceUnityRuntimeCompiledFacade : IUnityRuntimeCompiledFacade
        {
            readonly IUnityRenderPipeline mUnityRenderPipeline = new UnityRenderPipeline();
            readonly IUnityXRBridge mUnityXRBridge = new UnityXRBridge();

            public IUnityRenderPipeline UnityRenderPipeline
            {
                get { return mUnityRenderPipeline; }
            }

            public IUnityXRBridge UnityXRBridge
            {
                get { return mUnityXRBridge; }
            }

            public bool IsUnityUICurrentlySelected()
            {
                return !(EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null);
            }
        }

        class UnityRenderPipeline : IUnityRenderPipeline
        {
            public event Action<Camera[]> BeginFrameRendering;
            public event Action<Camera> BeginCameraRendering;

            public UnityRenderPipeline()
            {
#if UNITY_2018
                UnityEngine.Experimental.Rendering.RenderPipeline.beginFrameRendering += OnBeginFrameRendering;
                UnityEngine.Experimental.Rendering.RenderPipeline.beginCameraRendering += OnBeginCameraRendering;
#else
                UnityEngine.Rendering.RenderPipelineManager.beginFrameRendering += OnBeginFrameRendering;
                UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
#endif
            }

#if UNITY_2018
            void OnBeginCameraRendering(Camera camera)
#else
            void OnBeginCameraRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
#endif
            {
                if (BeginCameraRendering != null)
                    BeginCameraRendering(camera);
            }

#if UNITY_2018
            void OnBeginFrameRendering(Camera[] cameras)
#else
            void OnBeginFrameRendering(UnityEngine.Rendering.ScriptableRenderContext context, Camera[] cameras)
#endif
            {
                if (BeginFrameRendering != null)
                    BeginFrameRendering(cameras);
            }
        }
        
        class UnityXRBridge : IUnityXRBridge
        {
            public UnityXRBridge()
            {
                RegisterCallbacks();
            }

            void RegisterCallbacks()
            {
#if UNITY_WSA && OPEN_XR_ENABLED

                var xrSettings = XRGeneralSettings.Instance;
                var xrManager = xrSettings.Manager;
                var xrLoader = xrManager.activeLoader;
                var xrInput = xrLoader.GetLoadedSubsystem<XRInputSubsystem>();

                xrInput.trackingOriginUpdated += TrackingOriginUpdated;
#endif
            }

            public event Action OnTrackingOriginUpdated;
            
            public bool IsOpenXREnabled()
            {
#if UNITY_WSA && OPEN_XR_ENABLED
                return true;
#else
                return false;
#endif
            }

            public IntPtr GetHoloLensSpatialCoordinateSystemPtr()
            {
#if UNITY_WSA && OPEN_XR_ENABLED
                // This method returns null for a short amount of time during initialization.
                // On HoloLens we attempt the configuration of the SceneCoordinateSystem until 
                // a non-null value is returned.
                // After initialization, we rely on the XRInputSubsystem.trackingOriginUpdated event
                // to retrieve a valid pointer to the SceneCoordinateSystem.
                var sceneCoordinateSystem = PerceptionInterop.GetSceneCoordinateSystem(Pose.identity);
                if (sceneCoordinateSystem == null)
                {
                    return IntPtr.Zero;
                }
                
                return Marshal.GetIUnknownForObject(sceneCoordinateSystem);
#elif UNITY_WSA && WINDOWS_XR_ENABLED
                return UnityEngine.XR.WindowsMR.WindowsMREnvironment.OriginSpatialCoordinateSystem;
#elif UNITY_WSA && !UNITY_2020_1_OR_NEWER
                return UnityEngine.XR.WSA.WorldManager.GetNativeISpatialCoordinateSystemPtr();
#else
                Debug.LogError("Failed to get HoloLens Spatial Coordinate System. " +
                               "Please include the appropriate XR Plugin package into your project.");
                return IntPtr.Zero;
#endif
            }
            
            public bool IsHolographicDevice()
            {
#if UNITY_WSA && WINDOWS_XR_ENABLED
                var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
                SubsystemManager.GetInstances(xrDisplaySubsystems);

                foreach (var xrDisplay in xrDisplaySubsystems)
                {
                    if (xrDisplay.running && !xrDisplay.displayOpaque)
                        return true;
                }

                return false;
#elif UNITY_WSA && !UNITY_2020_1_OR_NEWER
                return XRDevice.isPresent && !UnityEngine.XR.WSA.HolographicSettings.IsDisplayOpaque;
#else
                return false;
#endif
            }

            public void SetFocusPointForFrame(Vector3 position, Vector3 normal)
            {
#if UNITY_WSA && WINDOWS_XR_ENABLED
                var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
                SubsystemManager.GetInstances(xrDisplaySubsystems);

                foreach (var xrDisplay in xrDisplaySubsystems)
                {
                    if (xrDisplay.running && !xrDisplay.displayOpaque)
                    {
                        xrDisplay.SetFocusPlane(position, normal, Vector3.zero);
                        return;
                    }
                }
#endif
            }
            
            void TrackingOriginUpdated(XRInputSubsystem inputSubsystem)
            {
                OnTrackingOriginUpdated?.Invoke();
            }
        }
    }
}