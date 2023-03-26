using PlaneWaver.Interaction;

namespace PlaneWaver.Modulation
{
    public class ParameterInstance
    {
        public readonly ModulationData DataRef;
        public ModulationValues Values;
        
        public ParameterInstance(in ModulationData data)
        {
            DataRef = data;
            Values = new ModulationValues(in data, data.IsVolatileEmitter) {
                Instant = DataRef.Input.IsInstant
            };
        }
        
        public void UpdateInputValue(in ActorObject actor)
        {
            Values.Instant = DataRef.Input.IsInstant;
            Values.Input = DataRef.Enabled ? DataRef.Input.GetValue(actor) : 0;
        }
        
        public ModulationComponent GetModulationComponent()
        {
            Values.Process();
            return DataRef.BuildComponent(Values.Output);
        } 
    }
}
