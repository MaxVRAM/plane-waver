using System;
using System.Collections.Concurrent;
using Unity.Entities;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    [Serializable]
    public class Parameter : IHasGUIContent
    {
        public ParameterType ParamType;
        public string ParameterName;
        public int ParameterIndex;
        public bool IsVolatileEmitter;
        public Vector2 Range;
        public Vector2 BaseRange;
        public float TimeExponent;
        public bool ReversePath;

        public ModulationInput Input;
        public ModulationOutput Output;
        public ModulationNoise Noise;

        public Parameter(ParameterType paramType, bool isVolatileEmitter)
        {
            ParamType = paramType;
            ParameterName = paramType.ToStringCached();
            ParameterIndex = (int)paramType;
            IsVolatileEmitter = isVolatileEmitter;
            Reset();
        }

        #region COMPONENT BUILDERS
        
        public ParameterComponent BuildComponent(float modulationValue, float perlinValue = 0)
        {
            return IsVolatileEmitter ? VolatileComponent(modulationValue) : StableComponent(modulationValue, perlinValue);
        }

        public ParameterComponent VolatileComponent(float modulationValue)
        {
            return new ParameterComponent {
                StartValue = ReversePath ? BaseRange.y : BaseRange.x,
                EndValue = ReversePath ? BaseRange.x : BaseRange.y,
                ModValue = modulationValue,
                Min = Range.x,
                Max = Range.y,
                Noise = Noise.Enabled ? Noise.Amount * Noise.Factor : 0,
                LockNoise = Noise.VolatileLock,
                TimeExponent = TimeExponent,
                ModulateStart = Output.Start,
                ModulateEnd = Output.End
            };
        }

        public ParameterComponent StableComponent(float modulationValue, float perlinValue = 0)
        {
            return new ParameterComponent {
                StartValue = modulationValue,
                Min = Range.x,
                Max = Range.y,
                Noise = Noise.Enabled ? Noise.Amount * Noise.Factor : 0,
                PerlinValue = perlinValue,
                UsePerlin = Noise.UsePerlin
            };
        }

        #endregion
        
        public void Reset()
        {
            TimeExponent = 1;
            Input = new ModulationInput();
            Output = new ModulationOutput();
            Noise = new ModulationNoise();

            switch (ParamType)
            {
                case ParameterType.Volume:
                    Range = new Vector2(0f, 1f);
                    BaseRange = IsVolatileEmitter ? new Vector2(0, 1) : new Vector2(0.5f, 0.5f);
                    ReversePath = true;
                    Output = new ModulationOutput {
                        Start = true,
                        End = false
                    };
                    break;
                case ParameterType.Playhead:
                    Range = new Vector2(0f, 1f);
                    BaseRange = IsVolatileEmitter ? new Vector2(0.25f, 0.75f) : new Vector2(0f, 1f);
                    ReversePath = false;
                    Output = new ModulationOutput {
                        Start = true,
                        End = false
                    };
                    break;
                case ParameterType.Duration:
                    Range = new Vector2(10f, 500f);
                    BaseRange = IsVolatileEmitter ? new Vector2(40, 80) : new Vector2(60, 60);
                    ReversePath = false;
                    Output = new ModulationOutput {
                        Start = false,
                        End = true
                    };
                    break;
                case ParameterType.Density:
                    Range = new Vector2(0.1f, 10f);
                    BaseRange = new Vector2(3f, 3f);
                    ReversePath = false;
                    Output = new ModulationOutput {
                        Start = true,
                        End = true
                    };
                    break;
                case ParameterType.Transpose:
                    Range = new Vector2(-3, 3);
                    BaseRange = Vector2.zero;
                    ReversePath = false;
                    Output = new ModulationOutput {
                        Start = false,
                        End = true
                    };
                    break;
                case ParameterType.Length:
                    Range = new Vector2(10f, 1000f);
                    BaseRange = new Vector2(500, 500);
                    ReversePath = false;
                    Output = new ModulationOutput {
                        Start = true,
                        End = false
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public GUIContent GetGUIContent()
        {
            string tooltip = ParamType switch {
                ParameterType.Volume   => "Volume of the emitted grains",
                ParameterType.Playhead => "Normalised audio clip playback position",
                ParameterType.Duration => "Playback duration (ms) for each emitted grain",
                ParameterType.Density  => "Number of overlapping grains",
                ParameterType.Transpose =>
                        "Pitch shift of grains in octaves. 0 = same pitch as source sample",
                ParameterType.Length =>
                        "Length (ms) of the triggered grain burst. Only valid for volatile emitters",
                _ => "Undefined parameter type"
            };

            var paramGUIContent = new GUIContent(IconManager.GetIcon(ParamType.ToStringCached()), tooltip);
            return paramGUIContent;
        }
    }
    
    #region COMPONENT DATA MODEL

    public struct ParameterComponent : IComponentData
    {
        public float StartValue;
        public float EndValue;
        public float TimeExponent;
        public bool ModulateStart;
        public bool ModulateEnd;
        public float ModValue;
        public float Min;
        public float Max;
        public float Noise;
        public float PerlinValue;
        public bool UsePerlin;
        public bool LockNoise;
    }

    #endregion

    public enum ParameterType
    {
        Volume = 0, Playhead = 1, Duration = 2, Density = 3, Transpose = 4, Length = 5
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
