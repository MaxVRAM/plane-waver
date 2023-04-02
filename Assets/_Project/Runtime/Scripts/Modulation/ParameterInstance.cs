using UnityEngine;
using PlaneWaver.Interaction;

namespace PlaneWaver.Modulation
{
    public class ParameterInstance
    {
        public readonly Parameter ParameterRef;
        public ModulationValues Values;
        
        private Vector2 _previousBaseRange;
        
        public ParameterInstance(in Parameter parameter)
        {
            ParameterRef = parameter;
            Values = new ModulationValues(in parameter);
        }
        
        public void UpdateInputValue(in ActorObject actor)
        {
            if (ParameterRef.BaseRange != _previousBaseRange)
            {
                Values.ResetInitialValue();
                _previousBaseRange = ParameterRef.Input.Range;
            }
            
            Values.Instant = ParameterRef.Input.Source.IsInstant;
            Values.Input = ParameterRef.Input.Enabled ? ParameterRef.Input.Source.GetValue(actor) : 0;
        }
        
        public ParameterComponent GetModulationComponent()
        {
            Values.Process();
            float perlin = ParameterRef.IsVolatileEmitter ? 0 : Values.GetPerlinValue();
            ParameterComponent component = ParameterRef.BuildComponent(Values.Output, perlin);
            return component;
        } 
    }
}
