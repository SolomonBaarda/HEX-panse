using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    public Camera MainCamera;
    public CinemachineVirtualCamera Camera;
    CinemachineTrackedDolly Dolly;

    public CinemachineSmoothPath CameraPath;

    public float CameraHeightOffGround = 3.0f;
    public float CameraDistanceFromCity = 1.0f;

    public Transform CameraLookAtPlayer;
    public Transform CameraLookAtCentreMap;

    public Transform CameraFollow;

    [Space]
    public float ManualCameraSpeed = 0.1f;

    private void Awake()
    {
        Dolly = Camera.GetCinemachineComponent<CinemachineTrackedDolly>();
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            SetCameraModeAutomatic(!Dolly.m_AutoDolly.m_Enabled);
        }

        if (!Dolly.m_AutoDolly.m_Enabled)
        {
            Dolly.m_PathPosition += Input.GetAxis("Horizontal") * ManualCameraSpeed * Time.deltaTime;
        }
    }

    public enum CameraMode
    {
        Automatic,
        Manual
    }

    public void SetCameraModeAutomatic(bool automatic)
    {
        Dolly.m_AutoDolly.m_Enabled = automatic;
    }

    public void SetupCameras(List<Vector3> cities)
    {
        Vector3 centre = new Vector3();
        foreach (Vector3 pos in cities)
        {
            centre += pos;
        }
        centre /= cities.Count;

        CinemachineSmoothPath.Waypoint[] waypoints = new CinemachineSmoothPath.Waypoint[cities.Count];

        // Sort points so that thay are in clockwise order
        cities.Sort((x, y) => -Clockwise.Compare(x, y, centre));

        // Add them as waypoints for the camera path
        for (int i = 0; i < cities.Count; i++)
        {
            Vector3 position = cities[i];
            position.y += CameraHeightOffGround;

            Vector3 facing = (new Vector3(centre.x, 0, centre.z) - new Vector3(position.x, 0, position.z)).normalized;

            waypoints[i] = new CinemachineSmoothPath.Waypoint() { position = position - facing * CameraDistanceFromCity };
        }

        CameraPath.m_Waypoints = waypoints;

        CameraLookAtCentreMap.position = centre;
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


    private static class Clockwise
    {
        public static int Compare(Vector3 first, Vector3 second, Vector3 centre)
        {
            Vector3 firstOffset = first - centre;
            Vector3 secondOffset = second - centre;

            // Get the angles in degrees
            float angle1 = Mathf.Atan2(firstOffset.x, firstOffset.z) * Mathf.Rad2Deg % 360;
            float angle2 = Mathf.Atan2(secondOffset.x, secondOffset.z) * Mathf.Rad2Deg % 360;

            // Compare them
            return angle1.CompareTo(angle2);
        }
    }

}
