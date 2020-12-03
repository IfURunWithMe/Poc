﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NRKernal.NRExamples;
using NRKernal;
using UnityEngine.Events;

public class ZMarkerHelper : MonoBehaviour
{
    public static ZMarkerHelper Instance;

    // 是否完成识别过
    public bool OpenScan = false;

    // 记录识别的marker的pose
    public Pose MapPose = Pose.identity;

    // 识别到maker的模型
    public TrackingImageVisualizer TrackingImageVisualizerPrefab;

    // 是否识别到marker
    private bool find = false;

    // 识别的次数 
    private int scanCount = 0;

    private Matrix4x4 WorldInMakerMatrix = Matrix4x4.identity;

    private Dictionary<int, TrackingImageVisualizer> m_Visualizers = new Dictionary<int, TrackingImageVisualizer>();

    private GameObject nrCamera;
    //private GameObject nrInput;
    private GameObject nrCameraParent;

    public UnityAction ScanSuccessEvent;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (OpenScan && (find || CheckingScan())
#if UNITY_EDITOR
            || Input.GetKeyDown(KeyCode.R)
#endif
        )
        {

            OpenScan = false;

            // go to next phase
            // UI - wait for other people

            ScanSuccessEvent?.Invoke();
        }
    }

    public void ResetScanStatus()
    {
        OpenScan = true;
        find = false;
        scanCount = 0;
        MapPose = Pose.identity;
        WorldInMakerMatrix = Matrix4x4.identity;
        m_Visualizers = new Dictionary<int, TrackingImageVisualizer>();
        ScanSuccessEvent = null;
        SwitchImageTrackingMode(true);
    }


    private bool CheckingScan()
    {
        List<NRTrackableImage> m_TempTrackingImages = new List<NRTrackableImage>();
        NRFrame.GetTrackables<NRTrackableImage>(m_TempTrackingImages, NRTrackableQueryFilter.All);
        foreach (var item in m_TempTrackingImages)
        {

            TrackingImageVisualizer visualizer = null;
            m_Visualizers.TryGetValue(item.GetDataBaseIndex(), out visualizer);

            if (item.GetTrackingState() == TrackingState.Tracking)
            {
                

                if (visualizer == null)
                {
                    visualizer = (TrackingImageVisualizer)Instantiate(TrackingImageVisualizerPrefab, item.GetCenterPose().position, item.GetCenterPose().rotation);
                    visualizer.Image = item;
                    visualizer.transform.parent = transform;
                    m_Visualizers.Add(item.GetDataBaseIndex(), visualizer);
                }

                scanCount++;
                if (scanCount > 64)
                {
                    WorldInMakerMatrix = Matrix4x4.Inverse(ZUtils.GetTMatrix(item.GetCenterPose().position, item.GetCenterPose().rotation));

                    nrCamera = GameObject.Find("NRCameraRig");

                    if (nrCameraParent == null)
                        nrCameraParent = new GameObject("NRCameraRigParent");
                    nrCameraParent.transform.position = nrCamera.transform.position;
                    nrCameraParent.transform.rotation = nrCamera.transform.rotation;
                    nrCamera.transform.SetParent(nrCameraParent.transform);

                    ResetPlayerPoseToMakerCoordinateSystem(nrCameraParent);

                    MapPose = new Pose(item.GetCenterPose().position, item.GetCenterPose().rotation);
                    m_Visualizers.Remove(item.GetDataBaseIndex());
                    Destroy(visualizer.gameObject);

                    SwitchImageTrackingMode(false);

                    find = true;
                    return true;
                }

            }
            else if (item.GetTrackingState() == TrackingState.Stopped && visualizer != null)
            {
                //FollowPanel.Instance.PleaseScanMarkerUI.text = "Please scan the marker";
                //FollowPanel.Instance.PleaseScanMarkerUI.gameObject.SetActive(true);
                m_Visualizers.Remove(item.GetDataBaseIndex());
                Destroy(visualizer.gameObject);
            }
        }
        return false;
    }

    private void SwitchImageTrackingMode(bool on)
    {
        var config = NRSessionManager.Instance.NRSessionBehaviour.SessionConfig;
        config.ImageTrackingMode = on ? TrackableImageFindingMode.ENABLE : TrackableImageFindingMode.DISABLE;
        NRSessionManager.Instance.SetConfiguration(config);
    }

    // reset player pose to marker coordinate system
    private void ResetPlayerPoseToMakerCoordinateSystem(GameObject player)
    {
        player.transform.position = ZUtils.GetPositionFromTMatrix(WorldInMakerMatrix);
        player.transform.rotation = ZUtils.GetRotationFromTMatrix(WorldInMakerMatrix);
    }
}