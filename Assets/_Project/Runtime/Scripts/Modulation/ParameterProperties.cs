using System;
using UnityEngine;

namespace PlaneWaver.Modulation
{
    public partial class Parameter
    {
        [Serializable]
        public struct PropertiesObject
        {
            public int Index;
            public string Name;
            public Vector2 ParameterRange;
            public Vector2 InitialRange;
            public bool InvertVolatileRange;
            public bool FixedStart;
            public bool FixedEnd;
            public string Icon;

            public PropertiesObject(
                int index, string name, Vector2 parameterRange, Vector2 initialRange, bool invertVolatileRange,
                bool fixedStart, bool fixedEnd, string icon = "icon.timeline-question.png")
            {
                Index = index;
                Name = name;
                ParameterRange = parameterRange;
                InitialRange = initialRange;
                InvertVolatileRange = invertVolatileRange;
                FixedStart = fixedStart;
                FixedEnd = fixedEnd;
                Icon = icon;
            }
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
                IsVolatileEmitter ? true : false,
                false,
                IsVolatileEmitter,
                "icon.volume-source.png"
            );
            ModulationData = new ModulationDataObject(ParameterProperties);
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
                IsVolatileEmitter,
                "icon.vector-point-select.png"
            );
            ModulationData = new ModulationDataObject(ParameterProperties);
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
                false,
                "icon.chart-timeline.png"
            );
            ModulationData = new ModulationDataObject(ParameterProperties);
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
                false,
                "icon.format-list-group.png"
            );
            ModulationData = new ModulationDataObject(ParameterProperties);
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
                false,
                "icon.altimeter.png"
            );
            ModulationData = new ModulationDataObject(ParameterProperties);
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
                false,
                "icon.clock-end.png"
            );
            ModulationData = new ModulationDataObject(ParameterProperties);
        }
    }
}