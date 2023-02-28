using UnityEngine;

namespace PlaneWaver.Interaction
{
    public class BehaviourClass : MonoBehaviour
    {
        public GameObject _SpawnedObject;
        public GameObject _ControllerObject;
        public ObjectSpawner _ObjectSpawner;
        public virtual void UpdateBehaviour(BehaviourClass behaviour) { }
    }
}
