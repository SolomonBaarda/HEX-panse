using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    public Camera MainCamera;

    [Header("Dolly")]
    public CinemachineVirtualCamera OuterDollyCamera;
    CinemachineTrackedDolly OuterDolly;
    public CinemachineVirtualCamera InnerDollyCamera;
    CinemachineTrackedDolly InnerDolly;

    public CinemachineSmoothPath DollyPathOuter;
    public CinemachineSmoothPath DollyPathInner;

    public float CameraDollyHeightOffGround = 3.0f;
    public float CameraOuterDollyDistanceFromCity = 1.75f;
    public float CameraInnerDollyDistanceFromCity = 1.75f;

    public float InnerDollyDistanceFromCentreThreshold = 2.0f;

    [Space]
    public float ManualCameraSpeed = 0.1f;

    [Header("Top Down")]
    public CinemachineVirtualCamera TopDownCamera;
    public float CameraTopDownHeightOffGround = 5.0f;
    bool useDolly = true;
    bool autoDolly = true;
    public bool IsManualDollyCamera => !autoDolly && useDolly;

    [Header("Shared")]
    public Transform CameraLookAtPlayer;
    public Transform CameraLookAtCentreMap;
    public Transform CameraFollow;


    private void Awake()
    {
        InnerDolly = InnerDollyCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        OuterDolly = OuterDollyCamera.GetCinemachineComponent<CinemachineTrackedDolly>();

        InnerDollyCamera.Priority = 1;
        OuterDollyCamera.Priority = 1;
        TopDownCamera.Priority = 0;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetCameraModeAutomatic(!autoDolly);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetCurrentCamera(!useDolly);
        }

        // Manual dolly
        if (IsManualDollyCamera)
        {
            float distance = Input.GetAxis("Horizontal") * ManualCameraSpeed * Time.deltaTime;
            InnerDolly.m_PathPosition += distance;
            OuterDolly.m_PathPosition += distance;
        }
        // Automatic dolly
        else
        {
            float distance = Vector3.Distance(CameraFollow.transform.position, CameraLookAtCentreMap.transform.position);

            if (distance < InnerDollyDistanceFromCentreThreshold)
            {
                InnerDollyCamera.Priority = 1;
                OuterDollyCamera.Priority = 0;
            }
            else
            {
                InnerDollyCamera.Priority = 0;
                OuterDollyCamera.Priority = 1;
            }
        }
    }


    public void SetCurrentCamera(bool dolly)
    {
        if (dolly)
        {
            InnerDollyCamera.Priority = 0;
            OuterDollyCamera.Priority = 1;
            TopDownCamera.Priority = 0;
        }
        else
        {
            InnerDollyCamera.Priority = 0;
            OuterDollyCamera.Priority = 0;
            TopDownCamera.Priority = 1;
        }

        useDolly = dolly;
    }

    public void SetCameraModeAutomatic(bool automatic)
    {
        InnerDolly.m_AutoDolly.m_Enabled = automatic;
        OuterDolly.m_AutoDolly.m_Enabled = automatic;

        autoDolly = automatic;
    }

    public void SetupCameras(List<Vector3> cities, Vector3 centre)
    {
        CinemachineSmoothPath.Waypoint[] outerWaypoints = new CinemachineSmoothPath.Waypoint[cities.Count];
        CinemachineSmoothPath.Waypoint[] innerWaypoints = new CinemachineSmoothPath.Waypoint[cities.Count];


        // Add them as waypoints for the camera path
        for (int i = 0; i < cities.Count; i++)
        {
            Vector3 position = cities[i];
            position.y += CameraDollyHeightOffGround;

            Vector3 facing = (new Vector3(centre.x, 0, centre.z) - new Vector3(position.x, 0, position.z)).normalized;

            outerWaypoints[i] = new CinemachineSmoothPath.Waypoint() { position = position - facing * CameraOuterDollyDistanceFromCity };
            innerWaypoints[i] = new CinemachineSmoothPath.Waypoint() { position = position + facing * CameraInnerDollyDistanceFromCity };
        }

        DollyPathOuter.m_Waypoints = outerWaypoints;
        DollyPathInner.m_Waypoints = innerWaypoints;

        CameraLookAtCentreMap.position = centre;
        TopDownCamera.transform.position = centre + Vector3.up * CameraTopDownHeightOffGround;
    }

    public bool IsHoveringMouseOverTerrain(float raycastDistance, LayerMask layermask, out Vector3 position)
    {
        Vector3 viewport = MainCamera.ScreenToViewportPoint(Input.mousePosition);

        // Mouse within window
        if (viewport.x >= 0 && viewport.x <= 1 && viewport.y >= 0 && viewport.y <= 1)
        {
            Ray ray = MainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, layermask))
            {
                position = hit.point;
                return true;
            }
        }

        position = Vector3.zero;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(CameraLookAtCentreMap.transform.position, InnerDollyDistanceFromCentreThreshold);
    }
}
