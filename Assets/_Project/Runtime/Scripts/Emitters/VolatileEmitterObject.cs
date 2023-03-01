using System.Collections.Generic;
using PlaneWaver.Interaction;
using PlaneWaver.Modulation;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    /// <summary>
    ///     Scriptable Object for storing deployable Volatile Emitter configurations, which are then assigned to Frames.
    /// </summary>
    [CreateAssetMenu(fileName = "Emitter.Volatile.", menuName = "PlaneWaver/Emitters/Volatile", order = 1)]
    public class VolatileEmitterObject : EmitterObject
    {
        #region CLASS DEFINITIONS

        public Data Volume = new(Defaults.Volume, true);
        public Data Playhead = new(Defaults.Playhead, true);
        public Data Duration = new(Defaults.Duration, true);
        public Data Density = new(Defaults.Density, true);
        public Data Transpose = new(Defaults.Transpose, true);
        public Data Length = new(Defaults.Length, true);

        #endregion

        #region INITIALISATION METHODS

        public override void InitialiseParameters(in Actor actor)
        {
            ParameterCount = 6;
            PreviousSmoothed = new float[ParameterCount];

            for (var i = 0; i < ParameterCount; i++) PreviousSmoothed[i] = 0f;

            Volume.Initialise(actor, true);
            Playhead.Initialise(actor, true);
            Duration.Initialise(actor, true);
            Density.Initialise(actor, true);
            Transpose.Initialise(actor, true);
            Length.Initialise(actor, true);

            IsInitialised = true;
        }

        #endregion

        #region PARAMETER COMPONENT BUILDERS

        public override List<ModulationComponent> GetModulationComponents()
        {
            var components = new List<ModulationComponent>
            {
                Volume.ProcessModulation(),
                Playhead.ProcessModulation(),
                Duration.ProcessModulation(),
                Density.ProcessModulation(),
                Transpose.ProcessModulation(),
                Length.ProcessModulation()
            };

            return components;
        }

        #endregion
    }
}