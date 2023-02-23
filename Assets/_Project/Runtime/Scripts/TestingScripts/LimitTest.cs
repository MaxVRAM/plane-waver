using System.Collections;
using System.Collections.Generic;
using MaxVRAM.Extensions;

using UnityEngine;

public class LimitTest : MonoBehaviour
{
    [Range(0f, 1f)] public float offset = 0;
    [Range(-1f, 1f)] public float amount = 0;
    [Range(0f, 1f)] public float result = 0;
    public float timeScaler = 0.2f;
    public float rawResult = 0;
    public float time = 0;


    void Start()
    {
    }

    private void Update()
    {
        time += Time.deltaTime * timeScaler;
        rawResult = RepeatNorm(time, amount, offset);
        result = rawResult;
    }

    public float PingPongLimiterA(float value, float amount, float offset)
    {
        float limitedValue = Mathf.PingPong(value + offset, 1f);
        float result = (limitedValue - offset) / amount;
        return Mathf.Clamp01(result);
    }

    public float PingPongLimiterB(float value, float amount, float offset)
    {
        // Doesn't account for negative amount values
        float limitedValue = Mathf.PingPong(offset + value, 1f);
        float clampedValue = Mathf.Clamp(limitedValue, 0f, 1f);
        float result = offset + (clampedValue - offset) * amount;
        return Mathf.Clamp01(result);
    }

    public float PingPongLimiterC(float value, float amount, float offset)
    {
        value = value.Abs();
        float dinged = amount > 0 ? offset + value : offset - value;
        float pinged = Mathf.PingPong(dinged, 1f);
        float result = offset + (pinged - offset) * amount.Abs();
        return result;
    }

    public float PingPongLimiterD(float value, float amount, float offset)
    {
        value = value.Abs();
        float pinged = amount > 0 ? offset + value : offset - value;
        float ponged = Mathf.PingPong(pinged, 1f);
        return offset + (ponged - offset) * amount.Abs();
    }

    public float RepeatNorm(float value, float amount, float offset)
    {
        float reeh = amount > 0 ? offset + value : offset - value;
        float pete = reeh.RepeatNorm();
        return offset + (pete - offset) * amount.Abs();
    }
}
