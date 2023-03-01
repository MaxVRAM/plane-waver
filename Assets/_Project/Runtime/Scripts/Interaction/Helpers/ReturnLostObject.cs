using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlaneWaver.Interaction
{
    public class ReturnLostObject : MonoBehaviour
    {
        public Vector3 InitialPosition;
        public Quaternion InitialRotation;
        public GameObject BoundingObject;
        private Collider _boundingCollider;
        private Rigidbody _rigidBody;
        public float Radius = 30f;
        public float HeightLimit = 50f;

        private void Start()
        {
            InitialPosition = transform.position;
            InitialRotation = transform.rotation;

            if (BoundingObject != null && BoundingObject.activeSelf &&
                BoundingObject && !BoundingObject.TryGetComponent(out _boundingCollider))
            {
                Vector3 localScale = BoundingObject.transform.localScale;
                Radius = (localScale.x + localScale.z) / 2;
            }

            _rigidBody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (_boundingCollider != null && !_boundingCollider.bounds.Contains(transform.position))
                ReturnObject();
            else
            {
                if (new Vector2(transform.position.x, transform.position.z).magnitude > Radius ||
                    transform.position.y > HeightLimit || transform.position.y < -HeightLimit)
                    ReturnObject();
            }
        }

        private void ReturnObject()
        {
            if (_rigidBody == null) return;
            _rigidBody.MovePosition(InitialPosition);
            _rigidBody.velocity = Vector3.zero;
            _rigidBody.rotation = InitialRotation;
        }
    }
}