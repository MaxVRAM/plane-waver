using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver
{
    public class ReturnLostObject : MonoBehaviour
    {
        public Vector3 _InitialPosition;
        public Quaternion _InitialRotation;
        public GameObject _BoundingObject;
        private Collider _BoundingCollider;
        private Rigidbody _RigidBody;
        public float _Radius = 30f;
        public float _HeightLimit = 50f;

        void Start()
        {
            _InitialPosition = transform.position;
            _InitialRotation = transform.rotation;

            if (_BoundingObject != null && _BoundingObject.activeSelf &&
                _BoundingObject && !_BoundingObject.TryGetComponent(out _BoundingCollider))
            {
                _Radius = (_BoundingObject.transform.localScale.x + _BoundingObject.transform.localScale.z) / 2;
            }

            _RigidBody = GetComponent<Rigidbody>();
        }

        void FixedUpdate()
        {
            if (_BoundingCollider != null && !_BoundingCollider.bounds.Contains(transform.position))
                ReturnObject();
            else
            {
                if (new Vector2(transform.position.x, transform.position.z).magnitude > _Radius ||
                    transform.position.y > _HeightLimit || transform.position.y < -_HeightLimit)
                    ReturnObject();
            }
        }

        public void ReturnObject()
        {
            if (_RigidBody != null)
            {
                _RigidBody.MovePosition(_InitialPosition);
                _RigidBody.velocity = Vector3.zero;
                _RigidBody.rotation = _InitialRotation;
            }
        }
    }
}