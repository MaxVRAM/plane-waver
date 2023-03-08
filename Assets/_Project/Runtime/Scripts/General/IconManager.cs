using System;
using PlaneWaver.Emitters;
using UnityEngine;
using PlaneWaver.Modulation;

namespace PlaneWaver
{
    public interface IHasIcon
    {
        public GUIContent GetIcon();
    }

    [Serializable]
    public class IconManager : MonoBehaviour
    {
        public static IconManager Instance;

        public GUIContent DefaultIcon;
        public EmitterIconClass EmitterIcons;
        public ParameterIconClass ParameterIcons;
        public ModulationIconClass ModulationIcons;

        public GUIContent GetIcon(IHasIcon obj)
        {
            return obj switch {
                BaseEmitterObject emitterObject => Instance.EmitterIcons.GetIcon(emitterObject),
                Parameter parameter             => Instance.ParameterIcons.GetIcon(parameter),
                _                               => Instance.DefaultIcon,
            };
        }

        [Serializable]
        public class EmitterIconClass
        {
            public GUIContent VolatileIcon;
            public GUIContent StableIcon;

            public GUIContent GetIcon(BaseEmitterObject emitter)
            {
                return emitter switch {
                    VolatileEmitterObject => Instance.EmitterIcons.VolatileIcon,
                    StableEmitterObject   => Instance.EmitterIcons.StableIcon,
                    _                     => Instance.DefaultIcon
                };
            }
        }

        [Serializable]
        public class ParameterIconClass
        {
            public GUIContent VolumeIcon;
            public GUIContent PlayheadIcon;
            public GUIContent DurationIcon;
            public GUIContent DensityIcon;
            public GUIContent TransposeIcon;
            public GUIContent LengthIcon;

            public GUIContent GetIcon(Parameter parameter)
            {
                return parameter switch {
                    Volume    => Instance.ParameterIcons.VolumeIcon,
                    Playhead  => Instance.ParameterIcons.PlayheadIcon,
                    Duration  => Instance.ParameterIcons.DurationIcon,
                    Density   => Instance.ParameterIcons.DensityIcon,
                    Transpose => Instance.ParameterIcons.TransposeIcon,
                    Length    => Instance.ParameterIcons.LengthIcon,
                    _         => Instance.DefaultIcon
                };
            }
        }

        [Serializable]
        public class ModulationIconClass
        {
            public GUIContent On;
            public GUIContent Off;
            public GUIContent Forward;
            public GUIContent Reverse;
        }
    }
}