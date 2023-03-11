using UnityEditor;
using UnityEngine;

using PlaneWaver.Emitters;
using PlaneWaver.Interaction;

namespace PlaneWaver.Emitters
{
    // [CustomEditor(typeof(EmitterFrame))]
    // public class EmitterFrameCustomEditor : Editor
    // {
    //     // If I get this working I should post it on the Unity forums
    //     // https://answers.unity.com/questions/1857866/calling-a-method-on-a-serialized-property-custom-e.html
    //     // https://answers.unity.com/questions/1782387/call-a-function-of-a-custom-class-through-a-serial.html
    //     
    //     // And here is an explanation about having to use a Custom Editor to access methods of a SerializedProperty:
    //     // https: //answers.unity.com/questions/1253497/call-a-function-from-custompropertydrawer-of-arbit.html
    //
    //     private EmitterFrame _emitterFrame;
    //     private EmitterAuth _emitterAuth;
    //     private SerializedProperty _actor;
    //     private SerializedProperty _speakerTarget;
    //     private SerializedProperty _speakerTransform;
    //     private SerializedProperty _stableList;
    //     private SerializedProperty _volatileList;
    //     
    //     private void OnEnable()
    //     {
    //         _emitterFrame = (EmitterFrame)target;
    //         _actor = serializedObject.FindProperty("Actor");
    //         _speakerTarget = serializedObject.FindProperty("SpeakerTarget");
    //         _speakerTransform = serializedObject.FindProperty("SpeakerTransform");
    //         _stableList = serializedObject.FindProperty("StableEmitters");
    //         _volatileList = serializedObject.FindProperty("VolatileEmitters");
    //     }
    //
    //     public override void OnInspectorGUI()
    //     {
    //         serializedObject.Update();
    //         
    //         int stableCount = _stableList.arraySize;
    //         int volatileCount = _volatileList.arraySize;
    //         
    //         EditorGUI.BeginChangeCheck();
    //         EditorGUILayout.PropertyField(_stableList);
    //         EditorGUILayout.PropertyField(_volatileList);
    //         if (EditorGUI.EndChangeCheck())
    //         {
    //             for (int i = stableCount; i < _emitterFrame.StableEmitters.Count; i++)
    //             {
    //                 _emitterFrame.StableEmitters[i].Reset();
    //                 
    //             }
    //             for (int i = volatileCount; i < _emitterFrame.VolatileEmitters.Count; i++)
    //                 _emitterFrame.VolatileEmitters[i] = new EmitterAuth();
    //         }
    //         serializedObject.ApplyModifiedProperties();
    //     }
    // }
}
