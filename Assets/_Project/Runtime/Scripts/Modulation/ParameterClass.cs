using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public class Parameter : IHasGUIContent
    {
        public ParameterType ParamType;
        public ModulationData Data;
        public bool IsVolatileEmitter;

        protected Parameter(ParameterType paramType, bool isVolatileEmitter)
        {
            ParamType = paramType;
            IsVolatileEmitter = isVolatileEmitter;
            Reset(ParamType);
        }

        public void Reset(ParameterType paramType)
        {
            ParamType = paramType;

            Data = ParamType switch {
                ParameterType.Volume => new ModulationData(IsVolatileEmitter) {
                    Input = new ModulationInput(),
                    Name = "Volume",
                    ParameterRange = new Vector2(0f, 1f),
                    InitialRange = IsVolatileEmitter ? new Vector2(0, 1) : new Vector2(0.5f, 0.5f),
                    ReversePath = IsVolatileEmitter,
                    FixedStart = false,
                    FixedEnd = IsVolatileEmitter
                },
                ParameterType.Playhead => new ModulationData(IsVolatileEmitter) {
                    Input = new ModulationInput(),
                    Name = "Playhead",
                    ParameterRange = new Vector2(0f, 1f),
                    InitialRange = IsVolatileEmitter ? new Vector2(0.25f, 0.75f) : new Vector2(0f, 1f),
                    ReversePath = false,
                    FixedStart = false,
                    FixedEnd = IsVolatileEmitter
                },
                ParameterType.Duration => new ModulationData(IsVolatileEmitter) {
                    Input = new ModulationInput(),
                    Name = "Duration",
                    ParameterRange = new Vector2(10f, 400f),
                    InitialRange = IsVolatileEmitter ? new Vector2(40, 80) : new Vector2(60, 60),
                    ReversePath = false,
                    FixedStart = IsVolatileEmitter,
                    FixedEnd = false
                },
                ParameterType.Density => new ModulationData(IsVolatileEmitter) {
                    Input = new ModulationInput(),
                    Name = "Density",
                    ParameterRange = new Vector2(0.1f, 10f),
                    InitialRange = new Vector2(3f, 3f),
                    ReversePath = false,
                    FixedStart = IsVolatileEmitter,
                    FixedEnd = false
                },
                ParameterType.Transpose => new ModulationData(IsVolatileEmitter) {
                    Input = new ModulationInput(),
                    Name = "Transpose",
                    ParameterRange = new Vector2(-3f, 3f),
                    InitialRange = Vector2.zero,
                    ReversePath = false,
                    FixedStart = IsVolatileEmitter,
                    FixedEnd = true,
                    
                },
                ParameterType.Length => new ModulationData(IsVolatileEmitter) {
                    Input = new ModulationInput(),
                    Name = "Burst Length",
                    ParameterRange = new Vector2(10f, 1000f),
                    InitialRange = new Vector2(500, 500),
                    ReversePath = false,
                    FixedStart = true,
                    FixedEnd = false
                },
                _ => Data
            };
        }

        public virtual GUIContent GetGUIContent()
        {
            return new GUIContent(IconManager.GetIcon(this), "This is an undefined parameter.");
        }

        public struct Defaults
        {
            public int Index;
            public string Name;
            public Vector2 ParameterRange;
            public Vector2 InitialRange;
            public bool ReversePath;
            public bool FixedStart;
            public bool FixedEnd;
            public bool IsLengthParameter;

            public Defaults(
                int index, string name, Vector2 parameterRange, Vector2 initialRange, bool reversePath,
                bool fixedStart, bool fixedEnd, bool isLengthParameter = false)
            {
                Index = index;
                Name = name;
                ParameterRange = parameterRange;
                InitialRange = initialRange;
                ReversePath = reversePath;
                FixedStart = fixedStart;
                FixedEnd = fixedEnd;
                IsLengthParameter = isLengthParameter;
            }
        }

        //     public static Defaults GetDefaults(ParameterType paramType)
        //     {
        //         return paramType switch {
        //             ParameterType.Volume _ => new Defaults
        //             (0, "Volume", new Vector2(0f, 1f),
        //                 parameter.IsVolatileEmitter ? new Vector2(0, 1) : new Vector2(0.5f, 0.5f),
        //                 parameter.IsVolatileEmitter, false, parameter.IsVolatileEmitter, false),
        //             Playhead _ => new Defaults
        //             (1, "Playhead", new Vector2(0f, 1f),
        //                 parameter.IsVolatileEmitter ? new Vector2(0.25f, 0.75f) : new Vector2(0, 1), false, false,
        //                 parameter.IsVolatileEmitter, false),
        //             Duration _ => new Defaults
        //             (2, "Grain Duration", new Vector2(10f, 250f),
        //                 parameter.IsVolatileEmitter ? new Vector2(40, 80) : new Vector2(60, 60), false,
        //                 parameter.IsVolatileEmitter, false, false),
        //             Density _ => new Defaults
        //             (3, "Grain Density", new Vector2(0.1f, 10f), new Vector2(3f, 3f), false,
        //                 parameter.IsVolatileEmitter, false),
        //             Transpose _ => new Defaults
        //             (4, "Transpose", new Vector2(-3, 3f), Vector2.zero, false,
        //                 parameter.IsVolatileEmitter, false, false),
        //             Length _ => new Defaults
        //             (5, "Burst Length", new Vector2(10f, 1000f), new Vector2(800, 800), false, true,
        //                 false, true),
        //             _ => new Defaults()
        //         };
        //     }
        // }
        //
        // public class Volume : Parameter
        // {
        //     public Volume(bool isVolatileEmitter) : base(isVolatileEmitter) { }
        //     
        //     public override GUIContent GetGUIContent()
        //     {
        //         return new GUIContent(IconManager.GetIcon(this), "Volume of the emitted grains");
        //     }
        // }
        //
        // public class Playhead : Parameter
        // {
        //     public Playhead(bool isVolatileEmitter) : base(isVolatileEmitter) { }
        //     
        //     public override GUIContent GetGUIContent()
        //     {
        //         return new GUIContent(IconManager.GetIcon(this), "Normalised audio clip playback position");
        //     }
        // }
        //
        // public class Duration : Parameter
        // {
        //     public Duration(bool isVolatileEmitter) : base(isVolatileEmitter) { }
        //
        //     public override GUIContent GetGUIContent()
        //     {
        //         return new GUIContent(IconManager.GetIcon(this), "Playback duration (ms) for each emitted grain");
        //     }
        // }
        //
        // public class Density : Parameter
        // {
        //     public Density(bool isVolatileEmitter) : base(isVolatileEmitter) { }
        //
        //     public override GUIContent GetGUIContent()
        //     {
        //         return new GUIContent(IconManager.GetIcon(this), "Number of overlapping grains");
        //     }
        // }
        //
        // public class Transpose : Parameter
        // {
        //     public Transpose(bool isVolatileEmitter) : base(isVolatileEmitter) { }
        //
        //     public override GUIContent GetGUIContent()
        //     {
        //         return new GUIContent
        //         (IconManager.GetIcon(this),
        //             "Pitch shift of grains in octaves. 0 = same pitch as source sample");
        //     }
        // }
        //
        // public class Length : Parameter
        // {
        //     public Length(bool isVolatileEmitter) : base(isVolatileEmitter) { }
        //
        //     public override GUIContent GetGUIContent()
        //     {
        //         return new GUIContent
        //         (IconManager.GetIcon(this),
        //             "Length (ms) of the triggered grain burst. This parameter is only valid for volatile emitters.");
        //     }
        // }
    }

    public enum ParameterType
    {
        Volume = 0,
        Playhead = 1,
        Duration = 2,
        Density = 3,
        Transpose = 4,
        Length = 5
    }
    
    public static class ParameterEnumExtensions
    {
        // https://www.meziantou.net/caching-enum-tostring-to-improve-performance.htm
        
        private static readonly ConcurrentDictionary<ParameterType, string> Cache = new();

        public static string ToStringCached(this ParameterType value)
        {
            return Cache.GetOrAdd(value, v => v.ToString());
        }
    }
}