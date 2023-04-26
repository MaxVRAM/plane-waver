using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MaxVRAM;

using PlaneWaver.Emitters;

namespace PlaneWaver
{
    public class SurfaceProperties : MonoBehaviour
    {
        public bool IsEmitter = false;
        [Range(0, 1)]
        public float Rigidity = 1;
        public bool ApplyToChildren;
        public bool IsSurfaceChild;

        private void Start()
        {
            IsEmitter = GetComponentInChildren<EmitterFrame>();
            if (IsSurfaceChild || !ApplyToChildren) return;

            foreach (Collider colliderComponent in GetComponentsInChildren<Collider>())
            {
                if (colliderComponent.gameObject.TryGetComponent(out SurfaceProperties _)) continue;
                var surface = colliderComponent.gameObject.AddComponent<SurfaceProperties>();
                surface.IsSurfaceChild = true;
                surface.Rigidity = Rigidity;
            }
        }
    }
}
