using System.Collections;
using System.Collections.Generic;
using UnityEditor.Hardware;

using UnityEngine;

public class KeepOnlyDecimals : MonoBehaviour
{

    void Start()
    {
        float[] testInputs = { 0.913245f, 53454.2915f, -0.913245f, -53454.2915f };
        int iterations = 1000000;

        for (int i = 0; i < testInputs.Length; i++)
        {
            RepeatTests(iterations, testInputs[i]);
        }

        Debug.Log("");
        Debug.Log("");

        //for (int i = 0; i < testInputs.Length; i++)
        //{
        //    PingPongTests(iterations, testInputs[i]);
        //}
    }

    public void RepeatTests(int iterations, float inputNumber)
    {
        Debug.Log($"                       REPEAT!     Iterations: {iterations}     Input: {inputNumber}      Actual Mathf.Repeat: {MathfRepeat(inputNumber)}     Manual FloorToInt: {ManualRepeat(inputNumber)}");

        float startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < iterations; i++)
        {
            MathfRepeat(inputNumber);
        }
        float endTime = Time.realtimeSinceStartup;
        Debug.Log($"                             - TIME TAKEN (seconds): {endTime - startTime}        (MATHF VERSION)");

        startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < iterations; i++)
        {
            ManualRepeat(inputNumber);
        }
        endTime = Time.realtimeSinceStartup;
        Debug.Log($"                             - TIME TAKEN (seconds): {endTime - startTime}        (MANUAL VERSION)");

        Debug.Log("");
    }

    public void PingPongTests(int iterations, float inputNumber)
    {
        Debug.Log($"                       PINGPONG!     Iterations:: {iterations}     Input: {inputNumber}      Actual Mathf.PingPong: {MathfPingPong(inputNumber)}     AbsModulo: {ModuloAbsPingPong(inputNumber)}      MultiplyNegModulo: {ModuloMultiplyNegativePingPong(inputNumber)}");

        float startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < iterations; i++)
        {
            MathfPingPong(inputNumber);
        }
        float endTime = Time.realtimeSinceStartup;
        Debug.Log($"                             - TIME TAKEN (seconds): {endTime - startTime}        (MATHF VERSION)");

        //startTime = Time.realtimeSinceStartup;
        //for (int i = 0; i < iterations; i++)
        //{
        //    LoopPingPong(inputNumber);
        //}
        //endTime = Time.realtimeSinceStartup;
        //Debug.Log($"                             - TIME TAKEN (seconds): {endTime - startTime}        (LOOP VERSION)");

        startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < iterations; i++)
        {
            ModuloAbsPingPong(inputNumber);
        }
        endTime = Time.realtimeSinceStartup;
        Debug.Log($"                             - TIME TAKEN (seconds): {endTime - startTime}        (ABS MODULO VERSION)");

        startTime = Time.realtimeSinceStartup;
        for (int i = 0; i < iterations; i++)
        {
            ModuloMultiplyNegativePingPong(inputNumber);
        }
        endTime = Time.realtimeSinceStartup;
        Debug.Log($"                             - TIME TAKEN (seconds): {endTime - startTime}        (MULTIPLY MODULO VERSION)");

        Debug.Log("");
    }

    public float MathfRepeat(float testNumber)
    {
        return Mathf.Repeat(testNumber, 1f);
    }

    public float ManualRepeat(float testNumber)
    {
        return testNumber - Mathf.FloorToInt(testNumber);
    }

    public float MathfPingPong(float testNumber)
    {
        return Mathf.PingPong(testNumber, 1f);
    }

    public float LoopPingPong(float testNumber)
    {
        while (testNumber is < 0 or > 1)
        {
            if (testNumber < 0)
                testNumber = -testNumber;
            if (testNumber > 1)
                testNumber = 1 - (testNumber - 1);
        }
        return testNumber;
    }

    public float ModuloAbsPingPong(float testNumber)
    {
        testNumber = Mathf.Abs(testNumber);
        testNumber %= 2;
        if (testNumber < 1)
            return testNumber;
        else
            return 2 * 1 - testNumber;
    }

    public float ModuloMultiplyNegativePingPong(float testNumber)
    {
        testNumber = testNumber > 0 ? testNumber : -testNumber;
        testNumber %= 2;
        if (testNumber < 1)
            return testNumber;
        else
            return 2 * 1 - testNumber;
    }
}
