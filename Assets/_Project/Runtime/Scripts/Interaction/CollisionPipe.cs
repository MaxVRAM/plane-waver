using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver
{
    [RequireComponent(typeof(Collider))]
    public class CollisionPipe : MonoBehaviour
    {
        [SerializeField]
        protected List<HostAuthoring> _HostComponentPipes;

        // !TODO: Consider moving to an event system to decouple from other spaces

        public CollisionPipe AddHost(HostAuthoring host)
        {
            if (_HostComponentPipes == null) _HostComponentPipes = new List<HostAuthoring>();
            if (_HostComponentPipes.Count == 0 || !_HostComponentPipes.Contains(host))
                _HostComponentPipes.Add(host);
            return this;
        }

        public void RemoveHost(HostAuthoring host)
        {
            if (host == null || _HostComponentPipes == null) return;
            if (_HostComponentPipes.Contains(host))
                _HostComponentPipes.Remove(host);
        }

        private void OnCollisionEnter(Collision collision)
        {
            foreach (HostAuthoring host in _HostComponentPipes)
                host.OnCollisionEnter(collision);
        }

        private void OnCollisionStay(Collision collision)
        {
            foreach (HostAuthoring host in _HostComponentPipes)
                host.OnCollisionStay(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            foreach (HostAuthoring host in _HostComponentPipes)
                host.OnCollisionExit(collision);
        }
    }
}
