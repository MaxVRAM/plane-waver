using PlaneWaver.Interaction;

namespace PlaneWaver.Modulation
{
    public class ParameterInstance
    {
        public readonly Parameter ParameterRef;
        public ModulationValues Values;
        
        public ParameterInstance(in Parameter parameter)
        {
            ParameterRef = parameter;
            Values = new ModulationValues(in parameter);
        }
        
        public void UpdateInputValue(in ActorObject actor)
        {
            Values.Instant = ParameterRef.Input.Source.IsInstant;
            Values.Input = ParameterRef.Input.Enabled ? ParameterRef.Input.Source.GetValue(actor) : 0;
        }
        
        public ParameterComponent GetModulationComponent()
        {
            Values.Process();
            ParameterComponent component = ParameterRef.BuildComponent(Values.Output);
            return component;
        } 
    }
}
