using UnityEngine;

//using MaxVRAM;

public class GUI_Tester : MonoBehaviour
{
    [Range(0, 1)] public float _TestSlider = 0;
    //[RangeSlider(0, 10, 0f, 1f)] public Vector2 _DifferentNumbers = new(1, 7);
    public float _DifferentX = 0;
    public float _DifferentY = 0;
    //[ValueSlider (0, 10, 0f, 1f)] public float _DifferentNumber = 0;
    public float _JustDifferent = 0;    

    public void Start()
    {
    }

    public void Update()
    {
        //_DifferentX = _DifferentNumbers.x;
        //_DifferentY = _DifferentNumbers.y;
        //_JustDifferent = _DifferentNumber;
    }
}
