using UnityEngine;

namespace PlaneWaver.Modulation
{
    public class Parameter : IHasGUIContent
    {
        public ModulationData Data;
        public bool IsVolatileEmitter;

        protected Parameter(bool isVolatileEmitter = false)
        {
            IsVolatileEmitter = isVolatileEmitter;
            Reset();
        }

        public void Reset()
        {
            Data.Input = new ModulationInput();
            Data = new ModulationData(GetDefaults(), IsVolatileEmitter);
        }
        
        public virtual Defaults GetDefaults() { return new Defaults(); }

        public class Defaults
        {
            public readonly int Index;
            public readonly string Name;
            public readonly Vector2 ParameterRange;
            public readonly Vector2 InitialRange;
            public readonly bool ReversePath;
            public readonly bool FixedStart;
            public readonly bool FixedEnd;

            public Defaults() { }

            public Defaults(int index, string name, Vector2 parameterRange, Vector2 initialRange, bool reversePath, bool fixedStart, bool fixedEnd)
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
        public override Defaults GetDefaults()
        {
            return new Defaults(
                0,
                "Volume",
                new Vector2(0f, 1f),
                IsVolatileEmitter ? new Vector2(0,1) : new Vector2(0.5f, 0.5f),
                IsVolatileEmitter,
                false,
                IsVolatileEmitter
            );
        }

        public override GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "Volume of the emitted grains");
        }
    }

    public class Playhead : Parameter
    {
        public override Defaults GetDefaults()
        {
            return new Defaults(
                1,
                "Playhead",
                new Vector2(0f, 1f),
                IsVolatileEmitter ? new Vector2(0f,0.75f) : new Vector2(0, 1),
                false,
                false,
                IsVolatileEmitter
            );
        }
        
        public override GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "Normalised audio clip playback position");
        }
    }

    public class Duration : Parameter
    {
        public override Defaults GetDefaults()
        {
            return new Defaults(
                2,
                "Grain Duration",
                new Vector2(10f, 250f),
                IsVolatileEmitter ? new Vector2(40,80) : new Vector2(60,60),
                false,
                IsVolatileEmitter,
                false
            );
        }

        public override GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "Playback duration (ms) for each emitted grain");
        }
    }

    public class Density : Parameter
    {
        public override Defaults GetDefaults()
        {
            return new Defaults(
                3,
                "Density",
                new Vector2(0.1f, 10),
                IsVolatileEmitter ? new Vector2(2,3) : new Vector2(3, 3),
                IsVolatileEmitter,
                false,
                false
            );
        }
        
        public override GUIContent GetGUIContent()
        {
            return new GUIContent
            (IconManager.GetIcon(this),
                "Number of overlapping grains");
        }
    }

    public class Transpose : Parameter
    {
        public override Defaults GetDefaults()
        {
            return new Defaults(
                4,
                "Transpose",
                new Vector2(-3, 3),
                Vector2.zero,
                false,
                IsVolatileEmitter,
                false
            );
        }

        public override GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "Pitch shift of grains in octaves. 0 = same pitch as source sample");
        }
    }

    public class Length : Parameter
    {
        public override Defaults GetDefaults()
        {
            return new Defaults(
                5,
                "Burst Length",
                new Vector2(10, 1000),
                new Vector2(800,800),
                false,
                true,
                false
            );
        }
        
        public override GUIContent GetGUIContent()
        {
            return new GUIContent(
                IconManager.GetIcon(this), 
                "Length (ms) of the triggered grain burst. This parameter is only valid for volatile emitters.");
        }
    }
}