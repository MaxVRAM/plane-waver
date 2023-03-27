using UnityEngine;
using UnityEditor;
using UnityEditor.AnimatedValues;

using MaxVRAM;
using MaxVRAM.Extensions;
using PlaneWaver.Modulation;
using UnityEngine.UIElements;

public class TestWindow : EditorWindow
{
    private AnimBool _showExtraFields;
    
    private ModulationInput _modulationInput = new ModulationInput();
    
    private Vector2 _paramMaxRange = new Vector2(-3,3);
    private Vector2 _paramVisible = Vector2.zero;
    private Vector2 _paramData = Vector2.zero;
    
    
    private const float MinFieldWidth = 45f;
    private const float MaxFieldWidth = 80f;
    
    [MenuItem("Plane Waver/Test Window")]
    static void Init()
    {
        TestWindow window = (TestWindow)EditorWindow.GetWindow(typeof(TestWindow));
    }

    void OnEnable()
    {
        _showExtraFields = new AnimBool(true);
        _showExtraFields.valueChanged.AddListener(Repaint);
    }

    void OnGUI()
    {
        float viewWidth = EditorGUIUtility.currentViewWidth;
        float rangeFieldWidth = Mathf.Clamp(viewWidth * 0.3f, MinFieldWidth, MaxFieldWidth);
        _showExtraFields.target = EditorGUILayout.ToggleLeft("Show extra fields", _showExtraFields.target);
        
        //Extra block that can be toggled on and off.
        if (EditorGUILayout.BeginFadeGroup(_showExtraFields.faded))
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            _paramMaxRange = EditorGUILayout.Vector2Field("Max Range", _paramMaxRange);
            _paramVisible = EditorGUILayout.Vector2Field("Visible Range", _paramVisible);
            _paramData = EditorGUILayout.Vector2Field("Data Range", _paramData);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            

            //EditorGUI.BeginChangeCheck();
            EditorGUILayout.FloatField(_paramVisible.x, GUILayout.Width(rangeFieldWidth));
            EditorGUILayout.FloatField(_paramVisible.y, GUILayout.Width(rangeFieldWidth));
            // if (EditorGUI.EndChangeCheck()) 
            //     _paramData = MaxMath.RangedToNorm(_paramMaxRange, _paramVisible).RoundDecimal(4);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.MinMaxSlider(ref _paramData.x, ref _paramData.y, 0, 1);
            if (EditorGUI.EndChangeCheck()) 
                _paramVisible = MaxMath.NormToRanged(_paramMaxRange, _paramData).RoundDecimal(4);
            
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndFadeGroup();
        EditorGUILayout.Space();
        
        
    }
    
    // https://docs.unity3d.com/2021.3/Documentation/ScriptReference/UIElements.MinMaxSlider.html
    
    void SkinSlider(Slider s)
    {
        var tracker = s.Q(className: Slider.trackerUssClassName);
        var dragger = s.Q(className: Slider.draggerUssClassName);
        var highlightTracker = new VisualElement()
        {
            name = "sub-tracker"
        };
        tracker.Add(highlightTracker); //Adding it as a child means it will be drawn on top
        highlightTracker.style.backgroundColor = Color.magenta;
        s.RegisterValueChangedCallback((evt) =>
        {
            highlightTracker.style.width = dragger.transform.position.x;
            highlightTracker.style.height = tracker.layout.height;
        });
    }
}