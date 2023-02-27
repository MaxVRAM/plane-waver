using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

#region ---------- COMPONENTS

//    _________.__                             .___ ________          __          
//   /   _____/|  |__ _____ _______   ____   __| _/ \______ \ _____ _/  |______   
//   \_____  \ |  |  \\__  \\_  __ \_/ __ \ / __ |   |    |  \\__  \\   __\__  \  
//   /        \|   Y  \/ __ \|  | \/\  ___// /_/ |   |    `   \/ __ \|  |  / __ \_
//  /_______  /|___|  (____  /__|    \___  >____ |  /_______  (____  /__| (____  /
//          \/      \/     \/            \/     \/          \/     \/          \/ 

public struct WindowingDataComponent : IComponentData
{
    public BlobAssetReference<FloatBlobAsset> WindowingArray;
}

public struct AudioClipDataComponent :IComponentData
{
    public int ClipIndex;
    public BlobAssetReference<FloatBlobAsset> ClipDataBlobAsset;
}

public struct AudioTimerComponent : IComponentData
{
    public int NextFrameIndexEstimate;
    public int GrainQueueSampleDuration;
    public int PreviousFrameSampleDuration;
    public int RandomiseBurstStartIndex;
    public int AverageGrainAge;
}

public struct ConnectionConfig : IComponentData
{
    public float DeltaTime;
    public float ArcDegrees;
    public float ListenerRadius;
    public float BusyLoadLimit;
    public float SpeakerLingerTime;
    public float TranslationSmoothing;
    public float3 DisconnectedPosition;
    public float3 ListenerPos;
}

//    ________             .__               
//   /  _____/___________  |__| ____   ______
//  /   \  __\_  __ \__  \ |  |/    \ /  ___/
//  \    \_\  \  | \// __ \|  |   |  \\___ \ 
//   \______  /__|  (____  /__|___|  /____  >
//          \/           \/        \/     \/ 

public struct SamplesProcessedTag : IComponentData { }

public struct GrainComponent : IComponentData
{
    public AudioClipDataComponent AudioClipDataComponent;
    public int StartSampleIndex;
    public int SampleCount;
    public float PlayheadNorm;
    public float Pitch;
    public float Volume;
    public int SpeakerIndex;
    public int EffectTailSampleLength;
}

//    _________                     __                        
//   /   _____/_____   ____ _____  |  | __ ___________  ______
//   \_____  \\____ \_/ __ \\__  \ |  |/ // __ \_  __ \/  ___/
//   /        \  |_> >  ___/ / __ \|    <\  ___/|  | \/\___ \ 
//  /_______  /   __/ \___  >____  /__|_ \\___  >__|  /____  >
//          \/|__|        \/     \/     \/    \/           \/ 

public struct SpeakerAvailableTag : IComponentData { }

public struct SpeakerIndex : IComponentData
{
    public int Value;
}

public enum ConnectionState
{
    Pooled,
    Active,
    Lingering
}

public struct SpeakerComponent : IComponentData
{
    public ConnectionState State;
    public int ConnectedHostCount;
    public float ConnectionRadius;
    public float InactiveDuration;
    public float GrainLoad;
    public float3 WorldPos;
}

//  ___________       .__  __    __                       
//  \_   _____/ _____ |__|/  |__/  |_  ___________  ______
//   |    __)_ /     \|  \   __\   __\/ __ \_  __ \/  ___/
//   |        \  Y Y  \  ||  |  |  | \  ___/|  | \/\___ \ 
//  /_______  /__|_|  /__||__|  |__|  \___  >__|  /____  >
//          \/      \/                    \/           \/ 

public struct ConnectedTag : IComponentData { }
public struct InListenerRadiusTag : IComponentData { }
public struct LoneHostOnSpeakerTag : IComponentData { }

public struct HostComponent : IComponentData
{
    public int HostIndex;
    public int SpeakerIndex;
    public bool Connected;
    public bool InListenerRadius;
    public float3 WorldPos;
}

public struct PlayingTag : IComponentData { }
public struct PingPongTag : IComponentData { }

public struct ContinuousComponent : IComponentData
{
    public int HostIndex;
    public int EmitterIndex;
    public int SpeakerIndex;
    public int AudioClipIndex;
    public bool PingPong;
    public bool IsPlaying;
    public float VolumeAdjust;
    public float DistanceAmplitude;
    public int LastSampleIndex;
    public int PreviousGrainDuration;
    public int SamplesUntilFade;
    public int SamplesUntilDeath;
    public ModulationComponent Playhead;
    public ModulationComponent Density;
    public ModulationComponent Duration;
    public ModulationComponent Transpose;
    public ModulationComponent Volume;
}

public struct BurstComponent : IComponentData
{
    public int HostIndex;
    public int EmitterIndex;
    public int SpeakerIndex;
    public int AudioClipIndex;
    public bool PingPong;
    public bool IsPlaying;
    public float VolumeAdjust;
    public float DistanceAmplitude;
    public ModulationComponent Length;
    public ModulationComponent Density;
    public ModulationComponent Playhead;
    public ModulationComponent Duration;
    public ModulationComponent Transpose;
    public ModulationComponent Volume;
}

public struct ModulationComponent : IComponentData
{
    public float StartValue;
    public float EndValue;
    public float Noise;
    public bool UsePerlin;
    public bool LockNoise;
    public float PerlinValue;
    public float Exponent;
    public float Modulation;
    public float Min;
    public float Max;
    public bool FixedStart;
    public bool FixedEnd;
    public float Input;
}

#endregion

#region ---------- BUFFER ELEMENTS
// Capacity set to a 1 second length by default
//[InternalBufferCapacity(44100)]
public struct GrainSampleBufferElement : IBufferElementData
{
    public float Value;
}

public struct DSPSampleBufferElement : IBufferElementData
{
    public float Value;
}

[System.Serializable]
public struct DSPParametersElement : IBufferElementData
{
    public DSPTypes DSPType;
    public bool DelayBasedEffect;
    public int SampleRate;
    public int SampleTail;
    public int SampleStartTime;
    public float Mix;
    public float Value0;
    public float Value1;
    public float Value2;
    public float Value3;
    public float Value4;
    public float Value5;
    public float Value6;
    public float Value7;
    public float Value8;
    public float Value9;
    public float Value10;
}

public enum DSPTypes
{
    Bitcrush,
    Flange,
    Delay,
    Filter,
    Chopper
}

#endregion

#region ---------- BLOB ASSETS

public struct FloatBlobAsset
{
    public BlobArray<float> Array;
}

#endregion

