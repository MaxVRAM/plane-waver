using System;
using System.Collections.Generic;
using PlaneWaver.Emitters;
using UnityEngine;
using PlaneWaver.Modulation;
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
            {"Default", GeneralBase + "undefined" + IconExtension},
            {"StableEmitter", EmitterBase + "stable" + IconExtension},
            {"VolatileEmitter", EmitterBase + "volatile" + IconExtension},
            {"Volume", ParameterBase + "volume" + IconExtension},
            {"Playhead", ParameterBase + "playhead" + IconExtension},
            {"Duration", ParameterBase + "duration" + IconExtension},
            {"Density", ParameterBase + "density" + IconExtension},
            {"Transpose", ParameterBase + "transpose" + IconExtension},
            {"Length", ParameterBase + "length" + IconExtension},
            {"ModulationOn", ModulationBase + "on" + IconExtension},
            {"ModulationOff", ModulationBase + "off" + IconExtension},
            {"PathForward", ModulationBase + "forward" + IconExtension},
            {"PathReverse", ModulationBase + "reverse" + IconExtension},
        };

        public static Texture GetIcon(string name)
        {
            string iconPath = IconPaths.ContainsKey(name) ? IconPaths[name] : IconPaths["Default"];
            return AssetDatabase.LoadAssetAtPath<Texture>(iconPath) ?? Texture2D.whiteTexture;
        }

        public static Texture GetIcon(IHasGUIContent obj)
        {
            return GetIcon(obj.GetType().Name);
        }
    }
}
