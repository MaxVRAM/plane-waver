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
    public BlobAssetReference<FloatBlobAsset> _WindowingArray;
}

public struct AudioClipDataComponent :IComponentData
{
    public int _ClipIndex;
    public BlobAssetReference<FloatBlobAsset> _ClipDataBlobAsset;
}

public struct AudioTimerComponent : IComponentData
{
    public int _NextFrameIndexEstimate;
    public int _GrainQueueSampleDuration;
    public int _PreviousFrameSampleDuration;
    public int _RandomiseBurstStartIndex;
    public int _AverageGrainAge;
}

public struct ConnectionConfig : IComponentData
{
    public float _DeltaTime;
    public float _ArcDegrees;
    public float _ListenerRadius;
    public float _BusyLoadLimit;
    public float _SpeakerLingerTime;
    public float _TranslationSmoothing;
    public float3 _DisconnectedPosition;
    public float3 _ListenerPos;
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
    public AudioClipDataComponent _AudioClipDataComponent;
    public int _StartSampleIndex;
    public int _SampleCount;
    public float _PlayheadNorm;
    public float _Pitch;
    public float _Volume;
    public int _SpeakerIndex;
    public int _EffectTailSampleLength;
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
    public ConnectionState _State;
    public int _ConnectedHostCount;
    public float _ConnectionRadius;
    public float _InactiveDuration;
    public float _GrainLoad;
    public float3 _WorldPos;
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
    public int _HostIndex;
    public int _SpeakerIndex;
    public bool _Connected;
    public bool _InListenerRadius;
    public float3 _WorldPos;
}
public struct PlayingTag : IComponentData { }
public struct PingPongTag : IComponentData { }
public struct ModulationComponent : IComponentData
{
    public float _StartValue;
    public float _EndValue;
    public float _Noise;
    public bool _PerlinNoise;
    public bool _LockNoise;
    public float _PerlinValue;
    public float _Exponent;
    public float _Modulation;
    public float _Min;
    public float _Max;
    public bool _FixedStart;
    public bool _FixedEnd;
    public float _Input;
}

public struct ContinuousComponent : IComponentData
{
    public int _HostIndex;
    public int _EmitterIndex;
    public int _SpeakerIndex;
    public int _AudioClipIndex;
    public bool _PingPong;
    public bool _IsPlaying;
    public float _VolumeAdjust;
    public float _DistanceAmplitude;
    public int _LastSampleIndex;
    public int _PreviousGrainDuration;
    public int _SamplesUntilFade;
    public int _SamplesUntilDeath;
    public ModulationComponent _Playhead;
    public ModulationComponent _Density;
    public ModulationComponent _Duration;
    public ModulationComponent _Transpose;
    public ModulationComponent _Volume;
}

public struct BurstComponent : IComponentData
{
    public int _HostIndex;
    public int _EmitterIndex;
    public int _SpeakerIndex;
    public int _AudioClipIndex;
    public bool _PingPong;
    public bool _IsPlaying;
    public float _VolumeAdjust;
    public float _DistanceAmplitude;
    public ModulationComponent _Length;
    public ModulationComponent _Density;
    public ModulationComponent _Playhead;
    public ModulationComponent _Duration;
    public ModulationComponent _Transpose;
    public ModulationComponent _Volume;
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
    public DSPTypes _DSPType;
    public bool _DelayBasedEffect;
    public int _SampleRate;
    public int _SampleTail;
    public int _SampleStartTime;
    public float _Mix;
    public float _Value0;
    public float _Value1;
    public float _Value2;
    public float _Value3;
    public float _Value4;
    public float _Value5;
    public float _Value6;
    public float _Value7;
    public float _Value8;
    public float _Value9;
    public float _Value10;
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
    public BlobArray<float> array;
}

#endregion


