using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxVRAM.Extensions
{
    public static class ListExtensions
    {
        public static Transform[] PositionArray<Transform>(this List<Transform> transforms)
        {
            Vector3[] positions = new Vector3[transforms.Count];



            foreach (var transform in transforms)
            {
                positions[transforms.IndexOf(transform)] = transform.position;
            }

            return list;
        }
    }
}
