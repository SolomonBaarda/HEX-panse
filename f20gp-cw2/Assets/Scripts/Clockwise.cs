using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Clockwise
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
