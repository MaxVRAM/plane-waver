
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace PlaneWaver
{
    public class ControllerToggleXR : MonoBehaviour
    {
        public List<GameObject> ControllerObjects = new();
        public List<XRNodeState> NodeStates = new();

        private void Start()
        {
            InputTracking.GetNodeStates(NodeStates);
            foreach (XRNodeState nodeState in NodeStates)
            {
                Debug.Log(nodeState.nodeType + " " + nodeState.tracked + " " + nodeState.uniqueID);
            }
            foreach (GameObject controller in ControllerObjects)
            {
                controller.SetActive(false);
            }
        }

        private void Update()
        {
            if (XRSettings.isDeviceActive)
            {
                foreach (GameObject controller in ControllerObjects) { controller.gameObject.SetActive(true); }
            }
            else
            {
                foreach (GameObject controller in ControllerObjects) { controller.gameObject.SetActive(false); }
            }
        }
    }
}