// using Unity.Entities;
// using Unity.Mathematics;
// using Serializable = System.SerializableAttribute;
//
// #region ---------- COMPONENTS
//
// //    _________.__                             .___ ________          __          
// //   /   _____/|  |__ _____ _______   ____   __| _/ \______ \ _____ _/  |______   
// //   \_____  \ |  |  \\__  \\_  __ \_/ __ \ / __ |   |    |  \\__  \\   __\__  \  
// //   /        \|   Y  \/ __ \|  | \/\  ___// /_/ |   |    `   \/ __ \|  |  / __ \_
// //  /_______  /|___|  (____  /__|    \___  >____ |  /_______  (____  /__| (____  /
// //          \/      \/     \/            \/     \/          \/     \/          \/ 
//
// //    ________             .__               
// //   /  _____/___________  |__| ____   ______
// //  /   \  __\_  __ \__  \ |  |/    \ /  ___/
// //  \    \_\  \  | \// __ \|  |   |  \\___ \ 
// //   \______  /__|  (____  /__|___|  /____  >
// //          \/           \/        \/     \/ 
//
// //    _________                     __                        
// //   /   _____/_____   ____ _____  |  | __ ___________  ______
// //   \_____  \\____ \_/ __ \\__  \ |  |/ // __ \_  __ \/  ___/
// //   /        \  |_> >  ___/ / __ \|    <\  ___/|  | \/\___ \ 
// //  /_______  /   __/ \___  >____  /__|_ \\___  >__|  /____  >
// //          \/|__|        \/     \/     \/    \/           \/ 
//
// public struct SpeakerAvailableTag : IComponentData { }
//
// public struct SpeakerIndex : IComponentData
// {
//     public int Value;
// }
//
// public enum ConnectionState
// {
//     Pooled, Active, Lingering
// }
//
// public struct SpeakerComponent : IComponentData
// {
//     public ConnectionState State;
//     public int ConnectedHostCount;
//     public float Radius;
//     public float InactiveDuration;
//     public float GrainLoad;
//     public float3 Position;
// }
//
// //  ___________       .__  __    __                       
// //  \_   _____/ _____ |__|/  |__/  |_  ___________  ______
// //   |    __)_ /     \|  \   __\   __\/ __ \_  __ \/  ___/
// //   |        \  Y Y  \  ||  |  |  | \  ___/|  | \/\___ \ 
// //  /_______  /__|_|  /__||__|  |__|  \___  >__|  /____  >
// //          \/      \/                    \/           \/ 
//
// public struct ConnectedTag : IComponentData { }
// public struct InListenerRadiusTag : IComponentData { }
// public struct LoneHostOnSpeakerTag : IComponentData { }
//
// public struct HostComponent : IComponentData
// {
//     public int HostIndex;
//     public int SpeakerIndex;
//     public bool Connected;
//     public bool InListenerRadius;
//     public float3 WorldPos;
// }
//
// public struct PlayingTag : IComponentData { }
// public struct PingPongTag : IComponentData { }
//
// public struct ContinuousComponent : IComponentData
// {
//     public int HostIndex;
//     public int EmitterIndex;
//     public int SpeakerIndex;
//     public int AudioClipIndex;
//     public bool PingPong;
//     public bool IsPlaying;
//     public float VolumeAdjust;
//     public float DistanceAmplitude;
//     public int LastSampleIndex;
//     public int PreviousGrainDuration;
//     public int SamplesUntilFade;
//     public int SamplesUntilDeath;
//     public ModulationComp Playhead;
//     public ModulationComp Density;
//     public ModulationComp Duration;
//     public ModulationComp Transpose;
//     public ModulationComp Volume;
// }
//
// public struct BurstComponent : IComponentData
// {
//     public int HostIndex;
//     public int EmitterIndex;
//     public int SpeakerIndex;
//     public int AudioClipIndex;
//     public bool PingPong;
//     public bool IsPlaying;
//     public float VolumeAdjust;
//     public float DistanceAmplitude;
//     public ModulationComp Length;
//     public ModulationComp Density;
//     public ModulationComp Playhead;
//     public ModulationComp Duration;
//     public ModulationComp Transpose;
//     public ModulationComp Volume;
// }
//
// public struct ModulationComp : IComponentData
// {
//     public float StartValue;
//     public float EndValue;
//     public float Noise;
//     public bool UsePerlin;
//     public bool LockNoise;
//     public float PerlinValue;
//     public float Exponent;
//     public float Modulation;
//     public float Min;
//     public float Max;
//     public bool FixedStart;
//     public bool FixedEnd;
//     public float Input;
// }
//
// #endregion
//
// #region ---------- BUFFER ELEMENTS
//
// // Capacity set to a 1 second length by default
// //[InternalBufferCapacity(44100)]
// public struct GrainSampleBufferElement : IBufferElementData
// {
//     public float Value;
// }
//
// public struct DSPSampleBufferElement : IBufferElementData
// {
//     public float Value;
// }
//
// [Serializable]
// public struct AudioEffectParameters : IBufferElementData
// {
//     public AudioEffectTypes AudioEffectType;
//     public bool DelayBasedEffect;
//     public int SampleRate;
//     public int SampleTail;
//     public int SampleStartTime;
//     public float Mix;
//     public float Value0;
//     public float Value1;
//     public float Value2;
//     public float Value3;
//     public float Value4;
//     public float Value5;
//     public float Value6;
//     public float Value7;
//     public float Value8;
//     public float Value9;
//     public float Value10;
// }
//
// public enum AudioEffectTypes
// {
//     Bitcrush, Flange, Delay, Filter, Chopper
// }
//
// #endregion
//
// #region ---------- BLOB ASSETS
//
// public struct FloatBlobAsset
// {
//     public BlobArray<float> Array;
// }
//
// #endregion