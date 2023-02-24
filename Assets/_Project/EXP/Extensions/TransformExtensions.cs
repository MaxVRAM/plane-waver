using UnityEngine;
using System.Collections;

namespace EXP
{
    public static class TransformExtensions
    {
        // Find furthest in array
        // Make heirarchy world 0
        // Follow XY, XZ, YZ

        [ContextMenu("Reset pivot")]
        public static void SetPosToFirstChild(this Transform t)
        {
            Vector3 child0Pos = t.GetChild(0).transform.position;
            Vector3 distance = child0Pos - t.position;

            t.position = child0Pos;

            for (int i = 0; i < t.childCount; i++)
            {
                t.GetChild(i).transform.position -= distance;
            }
        }

        public static void SetWorldX(this Transform t, float x)
        {
            Vector3 pos = t.position;
            pos.x = x;
            t.position = pos;
        }

        public static void SetWorldY(this Transform t, float y)
        {
            Vector3 pos = t.position;
            pos.y = y;
            t.position = pos;
        }

        public static void SetWorldZ(this Transform t, float z)
        {
            Vector3 pos = t.position;
            pos.z = z;
            t.position = pos;
        }

        public static void SetLocalX(this Transform t, float x)
        {
            Vector3 pos = t.localPosition;
            pos.x = x;
            t.localPosition = pos;
        }

        public static void SetLocalY(this Transform t, float y)
        {
            Vector3 pos = t.localPosition;
            pos.y = y;
            t.localPosition = pos;
        }

        public static void SetLocalZ(this Transform t, float z)
        {
            Vector3 pos = t.localPosition;
            pos.z = z;

            t.localPosition = pos;
        }

        public static void SetLocalRotX(this Transform t, float x)
        {
            Vector3 rot = t.localRotation.eulerAngles;
            rot.x = x;
            Quaternion rotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(rot), 1);
            t.localRotation = rotation;
        }

        public static void SetLocalRotY(this Transform t, float y)
        {
            Vector3 rot = t.localRotation.eulerAngles;
            rot.y = y;
            Quaternion rotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(rot), 1);
            t.localRotation = rotation;
        }

        public static void SetLocalRotZ(this Transform t, float z)
        {
            Vector3 rot = t.localRotation.eulerAngles;
            rot.z = z;
            Quaternion rotation = Quaternion.Slerp(t.localRotation, Quaternion.Euler(rot), 1);
            t.localRotation = rotation;
        }

        public static void SetRotX(this Transform t, float x)
        {
            Vector3 rot = t.rotation.eulerAngles;
            rot.x = x;
            Quaternion rotation = Quaternion.Slerp(t.rotation, Quaternion.Euler(rot), 1);
            t.rotation = rotation;
        }

        public static void SetRotY(this Transform t, float y)
        {
            Vector3 rot = t.rotation.eulerAngles;
            rot.y = y;
            Quaternion rotation = Quaternion.Slerp(t.rotation, Quaternion.Euler(rot), 1);
            t.rotation = rotation;
        }

        public static void SetRotZ(this Transform t, float z)
        {
            Vector3 rot = t.rotation.eulerAngles;
            rot.z = z;
            Quaternion rotation = Quaternion.Slerp(t.rotation, Quaternion.Euler(rot), 1);
            t.rotation = rotation;
        }

        public static void SetScaleX(this Transform t, float x)
        {
            Vector3 scale = t.localScale;
            scale.x = x;
            t.localScale = scale;
        }

        public static void SetScaleY(this Transform t, float y)
        {
            Vector3 scale = t.localScale;
            scale.y = y;
            t.localScale = scale;
        }

        public static void SetScaleZ(this Transform t, float z)
        {
            Vector3 scale = t.localScale;
            scale.z = z;
            t.localScale = scale;
        }

        public static void CopyTransform(this Transform t, Transform transformToCopy)
        {
            t.SetPositionAndRotation(transformToCopy.position, transformToCopy.rotation);
            t.localScale = transformToCopy.localScale;
        }

        public static void DestroyAllChildren(this Transform t)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                GameObject.Destroy(t.GetChild(i).gameObject);
            }
        }

        public static Vector3 PositionBetween(this Transform t, Transform t2, float normalizedPosBetween)
        {
            Vector3 placementVector = t2.position - t.position;
            Vector3 pos = t.position + placementVector * normalizedPosBetween;
            return pos;
        }

        public static void ParentAndZero(this Transform t, Transform parent)
        {
            t.parent = parent;
            t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            t.localScale = Vector3.one;
        }

        public static void ParentAndZero(this Transform t, Transform parent, bool zeroPos, bool zeroRot, bool zeroScale)
        {
            t.parent = parent;

            if (zeroPos)
                t.localPosition = Vector3.zero;

            if (zeroRot)
                t.localRotation = Quaternion.identity;

            if (zeroScale)
                t.localScale = Vector3.one;
        }

        public static Matrix4x4 ToMatrix4x4(this Transform t, bool localSpace = true)
        {
            if (localSpace)
                return Matrix4x4.TRS(t.localPosition, t.localRotation, t.localScale);
            else
                return Matrix4x4.TRS(t.position, t.rotation, t.localScale);
        }

        // uses the tangent, normal and binormal of previous transform to make sure this rotation doesn't twist
        // http://www.cs.cmu.edu/afs/andrew/scs/cs/15-462/web/old/asst2camera.html?fbclid=IwAR00IiKeiDgu-VP7luD9xssu4Ojsw2ruB_xtlZpWR42XIYFZwxikOZ0RYUw
        public static Quaternion SmoothRotation(Vector3 currentPos, Vector3 prevPos, Quaternion prevRotation)
        {
            Quaternion smoothedRotation;

            Vector3 prevTangent = prevRotation * Vector3.forward;
            Vector3 prevNormal = prevRotation * Vector3.up;
            Vector3 prevBinormal = prevRotation * Vector3.left;

            // Debug.Log(prevTangent + "   " + prevNormal + "   " + prevBinormal);

            Vector3 tangent = (currentPos - prevPos).normalized;
            Vector3 normal = Vector3.Cross(prevBinormal, tangent).normalized;
            Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

            smoothedRotation = Quaternion.LookRotation(tangent, normal);
            return smoothedRotation;
        }
    }
}