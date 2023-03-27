using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PlaneWaver
{
    public interface IHasGUIContent
    {
        public GUIContent GetGUIContent();
    }

    public static class IconManager
    {
        private const string BaseIconFolder = "Assets/_Project/Resources/Icons/";
        private const string IconExtension = ".png";
        private const string IconPrefix = "icon.";
        private const string GeneralBase = BaseIconFolder + "General/" + IconPrefix + "general-";
        private const string ModulationBase = BaseIconFolder + "Modulation/" + IconPrefix + "modulation-";
        private const string EmitterBase = BaseIconFolder + "Emitters/" + IconPrefix + "emitter-";
        private const string ParameterBase = BaseIconFolder + "Parameters/" + IconPrefix + "parameter-";

        private static readonly Dictionary<string, string> IconPaths = new() {
            { "Default", GeneralBase + "undefined" + IconExtension },
            { "StableEmitter", EmitterBase + "stable" + IconExtension },
            { "VolatileEmitter", EmitterBase + "volatile" + IconExtension },
            { "ConstantEmitter", EmitterBase + "constant" + IconExtension },
            { "ContactEmitter", EmitterBase + "contact" + IconExtension },
            { "AirborneEmitter", EmitterBase + "airborne" + IconExtension },
            { "CollisionEmitter", EmitterBase + "collision" + IconExtension },
            { "Volume", ParameterBase + "volume" + IconExtension },
            { "Playhead", ParameterBase + "playhead" + IconExtension },
            { "Grain Duration", ParameterBase + "duration" + IconExtension },
            { "Density", ParameterBase + "density" + IconExtension },
            { "Transpose", ParameterBase + "transpose" + IconExtension },
            { "Burst Length", ParameterBase + "length" + IconExtension },
            { "ModulationOn", ModulationBase + "on" + IconExtension },
            { "ModulationOff", ModulationBase + "off" + IconExtension },
            { "PathForward", ModulationBase + "forward" + IconExtension },
            { "PathReverse", ModulationBase + "reverse" + IconExtension },
        };

        public static Texture GetIcon(string name)
        {
            string iconPath = IconPaths.ContainsKey(name) ? IconPaths[name] : IconPaths["Default"];
            return AssetDatabase.LoadAssetAtPath<Texture>(iconPath) ?? Texture2D.whiteTexture;
        }

        public static Texture GetIcon(IHasGUIContent obj) { return GetIcon(obj.GetType().Name); }

        public static readonly Dictionary<string, GUIContent> ToggleIcons = new() {
            { "ModulationOn", new GUIContent(GetIcon("ModulationOn"), "Modulation On") },
            { "ModulationOff", new GUIContent(GetIcon("ModulationOff"), "Modulation Off") }, {
                "PathForward",
                new GUIContent
                (GetIcon("PathForward"),
                    "Path Forward. " +
                    "Parameter value will traverse the range in a FORWARD direction over the duration of the grain burst.")
            }, {
                "PathReverse",
                new GUIContent
                (GetIcon("PathReverse"),
                    "Path Reverse. " +
                    "Parameter value will traverse the range in a REVERSE direction over the duration of the grain burst.")
            }
        };
    }
}