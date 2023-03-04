using System;
using UnityEngine;

namespace PlaneWaver.Parameters
{
    public partial class Parameter
    {
        [Serializable]
        public struct ParameterDefault
        {
            public int Index;
            public string Name;
            public Vector2 ParameterMaxRange;
            public Vector2 InitialRange;
            public bool FixedStart;
            public bool FixedEnd;

            public ParameterDefault(int index, string name, Vector2 paramRange, Vector2 initialRange, bool fixedStart, bool fixedEnd)
            {
                Index = index;
                Name = name;
                ParameterMaxRange = paramRange;
                InitialRange = initialRange;
                FixedStart = fixedStart;
                FixedEnd = fixedEnd;
            }
        }
    }

    public class Volume : Parameter
    {
        public Volume(bool volatileEmitter = false) : base(volatileEmitter)
        {
            Defaults = new ParameterDefault(
                0,
                "Volume",
                new Vector2(0f, 2f),
                VolatileEmitter ? new Vector2(1,0) : new Vector2(0.5f, 0.5f),
                false,
                VolatileEmitter
            );
            Data = new DataObject(Defaults);
        }
    }

    public class Playhead : Parameter
    {
        public Playhead(bool volatileEmitter = false) : base(volatileEmitter)
        {
            Defaults = new ParameterDefault(
                1,
                "Playhead",
                new Vector2(0f, 2f),
                VolatileEmitter ? new Vector2(0.3f,0) : new Vector2(0, 1),
                false,
                VolatileEmitter
            );
            Data = new DataObject(Defaults);
        }
    }

    public class Duration : Parameter
    {
        public Duration(bool volatileEmitter = false) : base(volatileEmitter)
        {
            Defaults = new ParameterDefault(
                2,
                "Duration",
                new Vector2(10f, 250f),
                VolatileEmitter ? new Vector2(40,80) : new Vector2(60,60),
                VolatileEmitter,
                false
            );
            Data = new DataObject(Defaults);
        }
    }

    public class Density : Parameter
    {
        public Density(bool volatileEmitter = false) : base(volatileEmitter)
        {
            Defaults = new ParameterDefault(
                3,
                "Density",
                new Vector2(0.1f, 10),
                VolatileEmitter ? new Vector2(3,2) : new Vector2(3, 3),
                false,
                false
            );
            Data = new DataObject(Defaults);
        }
    }

    public class Transpose : Parameter
    {
        public Transpose(bool volatileEmitter = false) : base(volatileEmitter)
        {
            Defaults = new ParameterDefault(
                4,
                "Transpose",
                new Vector2(-3, 3),
                Vector2.zero,
                VolatileEmitter,
                false
            );
            Data = new DataObject(Defaults);
        }
    }

    public class Length : Parameter
    {
        public Length(bool volatileEmitter = true) : base(volatileEmitter)
        {
            if (!VolatileEmitter) throw new Exception("Length parameter is only valid for volatile emitters");
            Defaults = new ParameterDefault(
                5,
                "Length",
                new Vector2(10, 1000),
                new Vector2(200,200),
                true,
                false
            );
            Data = new DataObject(Defaults);
        }
    }
}