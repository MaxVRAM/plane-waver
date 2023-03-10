using System;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    public partial class Parameter : IHasGUIContent
    {
        [Serializable]
        public struct PropertiesObject
        {
            public int Index;
            public string Name;
            public Vector2 ParameterRange;
            public Vector2 InitialRange;
            public bool ReversePath;
            public bool FixedStart;
            public bool FixedEnd;

            public PropertiesObject(
                int index, string name, Vector2 parameterRange, Vector2 initialRange, bool reversePath,
                bool fixedStart, bool fixedEnd)
            {
                Index = index;
                Name = name;
                ParameterRange = parameterRange;
                InitialRange = initialRange;
                ReversePath = reversePath;
                FixedStart = fixedStart;
                FixedEnd = fixedEnd;
            }
        }

        public virtual GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "This is an undefined parameter.");
        }
    }

    public class Volume : Parameter
    {
        public Volume(bool isVolatileEmitter = false) : base(isVolatileEmitter)
        {
            ParameterProperties = new PropertiesObject(
                0,
                "Volume",
                new Vector2(0f, 2f),
                IsVolatileEmitter ? new Vector2(0,1) : new Vector2(0.5f, 0.5f),
                IsVolatileEmitter,
                false,
                IsVolatileEmitter
            );
            ModulationData = new ModulationDataObject(ParameterProperties);
        }
        
        public override GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "Volume of the emitted grains.");
        }
    }

    public class Playhead : Parameter
    {
        public Playhead(bool isVolatileEmitter = false) : base(isVolatileEmitter)
        {
            ParameterProperties = new PropertiesObject(
                1,
                "Playhead",
                new Vector2(0f, 1f),
                IsVolatileEmitter ? new Vector2(0.3f,0) : new Vector2(0, 1),
                false,
                false,
                IsVolatileEmitter
            );
            ModulationData = new ModulationDataObject(ParameterProperties);
        }
        
        public override GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "Normalised audio sample playback position a grain starts at when spawned.");
        }
    }

    public class Duration : Parameter
    {
        public Duration(bool isVolatileEmitter = false) : base(isVolatileEmitter)
        {
            ParameterProperties = new PropertiesObject(
                2,
                "Duration",
                new Vector2(10f, 250f),
                IsVolatileEmitter ? new Vector2(40,80) : new Vector2(60,60),
                false,
                IsVolatileEmitter,
                false
            );
            ModulationData = new ModulationDataObject(ParameterProperties);
        }
        
        public override GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "Playback duration (ms) for each emitted grain.");
        }
    }

    public class Density : Parameter
    {
        public Density(bool isVolatileEmitter = false) : base(isVolatileEmitter)
        {
            ParameterProperties = new PropertiesObject(
                3,
                "Density",
                new Vector2(0.1f, 10),
                IsVolatileEmitter ? new Vector2(3,2) : new Vector2(3, 3),
                false,
                false,
                false
            );
            ModulationData = new ModulationDataObject(ParameterProperties);
        }
        
        public override GUIContent GetGUIContent()
        {
            return new GUIContent
            (IconManager.GetIcon(this),
                "Amount of overlap for consecutive grains. < 1 produces a silent period between each grain. = 1 " +
                "creates grains one after another. 1-2 creates uneven overlap due to amplitude windowing. While values " +
                "above 2 will produce a dense and constant sound.");
        }
    }

    public class Transpose : Parameter
    {
        public Transpose(bool isVolatileEmitter = false) : base(isVolatileEmitter)
        {
            ParameterProperties = new PropertiesObject(
                4,
                "Transpose",
                new Vector2(-3, 3),
                Vector2.zero,
                false,
                IsVolatileEmitter,
                false
            );
            ModulationData = new ModulationDataObject(ParameterProperties);
        }
        
        public override GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "Pitch shift applied to each emitted grain. 0 = no shift. -1 = one octave down. 1 = one octave up.");
        }
    }

    public class Length : Parameter
    {
        public Length(bool isVolatileEmitter = true) : base(isVolatileEmitter)
        {
            if (!IsVolatileEmitter) throw new Exception("Length parameter is only valid for volatile emitters");
            ParameterProperties = new PropertiesObject(
                5,
                "Length",
                new Vector2(10, 1000),
                new Vector2(200,200),
                false,
                true,
                false
            );
            ModulationData = new ModulationDataObject(ParameterProperties);
        }
        
        public override GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "Length (ms) of the triggered grain burst. This parameter is only valid for volatile emitters.");
        }
    }
}