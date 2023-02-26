using UnityEngine;

namespace MaxVRAM.Extensions
{
    public static class GameObjectExtensions
    {
        public static GameObject SetParentAndZero(this GameObject gO, GameObject parent)
        {
            gO.transform.parent = parent.transform;
            gO.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            gO.transform.localScale = Vector3.one;
            return gO;
        }

        public static GameObject SetParentAndZero(this GameObject gO, Transform parent)
        {
            return gO.SetParentAndZero(parent.gameObject);
        }

        public static void DestroyAllChildren(this GameObject obj)
        {
            for (int i = 0; i < obj.transform.childCount; i++)
            {
                Object.DestroyImmediate(obj.transform.GetChild(i).gameObject);
            }
        }
    }
}