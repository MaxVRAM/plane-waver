using System.Collections.Generic;
using PlaneWaver.Interaction;
using PlaneWaver.Modulation;
using UnityEngine;

namespace PlaneWaver.Emitters
{
    /// <summary>
    ///     Scriptable Object for storing deployable Stable Emitter configurations, which are then assigned to Frames.
    /// </summary>
    [CreateAssetMenu(fileName = "Emitter.Stable.", menuName = "PlaneWaver/Emitters/Stable", order = 1)]
    public class StableEmitterObject : EmitterObject
    {
        #region CLASS DEFINITIONS

        public Data Volume = new(Defaults.Volume);
        public Data Playhead = new(Defaults.Playhead);
        public Data Duration = new(Defaults.Duration);
        public Data Density = new(Defaults.Density);
        public Data Transpose = new(Defaults.Transpose);

        #endregion

        #region INITIALISATION METHODS

        public override void InitialiseParameters(in Actor actor)
        {
            ParameterCount = 5;
            PreviousSmoothed = new float[ParameterCount];

            for (var i = 0; i < ParameterCount; i++)
            {
                PreviousSmoothed[i] = 0f;
            }

            Volume.Initialise(actor);
            Playhead.Initialise(actor);
            Duration.Initialise(actor);
            Density.Initialise(actor);
            Transpose.Initialise(actor);

            IsInitialised = true;
        }

        #endregion

        #region PARAMETER COMPONENT BUILDERS

        public override List<ModulationComponent> GetModulationComponents()
        {
            var components = new List<ModulationComponent>
            {
                Volume.BuildComponent(),
                Playhead.BuildComponent(),
                Duration.BuildComponent(),
                Density.BuildComponent(),
                Transpose.BuildComponent()
            };

            return components;
        }

        #endregion
    }
}