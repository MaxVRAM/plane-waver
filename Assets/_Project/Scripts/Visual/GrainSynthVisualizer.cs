﻿//using Ludiq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using PlaneWaver;

public class GrainSynthVisualizer : MonoBehaviour
{
    #region ------------------------------------- VARIABLES 
    float _SampleRate;
    public ContinuousAuthoring _Emitter;
    public SpeakerAuthoring _GrainSpeaker;
    Vector3 _LookAtPos;
    int _ClipSampleCount;

    public bool _DrawWaveform = true;
    public bool _DrawTimeline = true;

    public Transform _WaveformParent;
    public Transform _TimelineParent;

    // ----------------------------------- WAVEFORM/PLAYHEAD
    [Header("Waveform - Playhead")]
    public float _ArcTotalAngle = 90;    
    public float _ArcRadius = 4;
    public int _WaveformBlockCount = 200;
    public float _WaveformHeight = 1;
    public float _WaveformBlockHeight = .5f;
    public GameObject _WaveformBlockPrefab;
    GameObject[] _WaveformBlocks;
    public LineRenderer _WaveformBorderLine;
    public int _BorderVerts = 30;

    float _PlayheadZOffset = .03f;

    public LineRenderer _PlayheadLine;

    public WaveformVizGrain _WaveformVizGrainPrefab;
    WaveformVizGrain[] _WaveformVizGrainPool;
    int _WaveformGrainIndex;


    // ----------------------------------- TIMELINE
    [Header("Timeline")]
    public float _TimelineScale = .1f;
    public float _TimelineDistance = 3;
    public float _TimelineDuration = 3;

   
    public int _TimelineHeightCount = 4;
    int _IncrementY = 0;

    public Transform _XAxisPivot;
    public Transform _XAxisPivot_FrameTime;
    public Transform _YAxisPivot;

   

    public GrainSynthVisualizerBlock _TimelineBlockPrefab;
    GrainSynthVisualizerBlock[] _TimelineBlocks;
    int _TimelinePoolAmount = 200;
    int _BlockCounter = 0;
    #endregion

    #region ------------------------------------- UNITY METHODS
    void Start()
    {     
        _SampleRate = AudioSettings.outputSampleRate;
        _GrainSpeaker.OnGrainEmitted += EmitGrain;

        if (_DrawWaveform)
        {
            // Waveform
            float[] clipData = new float[20]; // [GrainBrain.Instance._AudioClips[0].samples];
            // GrainBrain.Instance._AudioClips[0].GetData(clipData, 0);
            _ClipSampleCount = clipData.Length;
            int samplesPerBlock = clipData.Length / _WaveformBlockCount;

            _PlayheadLine.positionCount = 10;

            float maxSampleValue = 0;
            float minSampleValue = float.MaxValue;
            _WaveformBlocks = new GameObject[_WaveformBlockCount];
            float waveformBlockWidth = Vector3.Distance(GetPositionOnArc(0), GetPositionOnArc(1f / (_WaveformBlockCount - 1)));
            float[] averagedSamples = new float[_WaveformBlockCount];

            _LookAtPos = Vector3.up * _WaveformHeight;

            int index = 0;
            for (int i = 0; i < _WaveformBlockCount; i++)
            {
                float summedSquaredSamples = 0;
                for (int s = 0; s < samplesPerBlock; s++)
                {
                    summedSquaredSamples += clipData[index] * clipData[index]; // sum squared samples
                    index++;
                }

                summedSquaredSamples /= samplesPerBlock;
                float rmsValue = Mathf.Sqrt(summedSquaredSamples / samplesPerBlock); // rms = square root of average

                maxSampleValue = Mathf.Max(rmsValue, maxSampleValue);
                minSampleValue = Mathf.Min(rmsValue, minSampleValue);
                averagedSamples[i] = rmsValue;

                float norm = i / (_WaveformBlockCount - 1f);
                _WaveformBlocks[i] = Instantiate(_WaveformBlockPrefab, transform);
                _WaveformBlocks[i].transform.position = GetPositionOnArc(norm);
                _WaveformBlocks[i].transform.LookAt(_LookAtPos);
                _WaveformBlocks[i].transform.rotation *= Quaternion.Euler(0, 180, 0);
            }

            for (int i = 0; i < _WaveformBlockCount; i++)
            {
                float height = Mathf.InverseLerp(minSampleValue, maxSampleValue, averagedSamples[i]);
                _WaveformBlocks[i].transform.localScale = new Vector3(waveformBlockWidth * .7f, height * _WaveformBlockHeight * .85f, .01f);
            }

            // Setup waveform border
            _WaveformBorderLine.positionCount = _BorderVerts * 2;

            // Create waveform grains
            _WaveformVizGrainPool = new WaveformVizGrain[30];
            for (int i = 0; i < _WaveformVizGrainPool.Length; i++)
            {
                WaveformVizGrain newGrain = Instantiate(_WaveformVizGrainPrefab, transform);
                newGrain.gameObject.SetActive(false);
                _WaveformVizGrainPool[i] = newGrain;
            }

            int borderIndex = 0;
            for (int i = 0; i < _BorderVerts; i++)
            {
                float norm = i / (_BorderVerts - 1f);
                norm = Mathf.Lerp(-.01f, 1.01f, norm);
                _WaveformBorderLine.SetPosition(borderIndex, GetPositionOnArc(norm, _WaveformBlockHeight * .5f));
                borderIndex++;
            }

            for (int i = 0; i < _BorderVerts; i++)
            {
                float norm = 1 - (i / (_BorderVerts - 1f));
                norm = Mathf.Lerp(-.01f, 1.01f, norm);
                _WaveformBorderLine.SetPosition(borderIndex, GetPositionOnArc(norm, _WaveformBlockHeight * -.5f));
                borderIndex++;
            }
        }
        else
            _WaveformParent.gameObject.SetActive(false);

        if (_DrawTimeline)
        {
            // Create grain blocks
            _TimelineBlocks = new GrainSynthVisualizerBlock[_TimelinePoolAmount];
            for (int i = 0; i < _TimelineBlocks.Length; i++)
            {
                GrainSynthVisualizerBlock newBlock = Instantiate(_TimelineBlockPrefab, transform);
                newBlock.gameObject.SetActive(false);
                _TimelineBlocks[i] = newBlock;
            }
        }
        else
            _TimelineParent.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        //if (_DrawWavefrom)
        //{
        //    // Waveform       
        //    float playheadWidth = Mathf.Max(.005f, _Emitter._EmissionProps._PlayheadRand);


        //    float startPos = _Emitter._EmissionProps._Playhead - .005f;
        //    for (int i = 0; i < _PlayheadLine.positionCount; i++)
        //    {
        //        float norm = i / (float)(_PlayheadLine.positionCount - 1);
        //        float playheadPos = startPos + (playheadWidth * norm);
        //        playheadPos = Mathf.Clamp01(playheadPos);
        //        _PlayheadLine.SetPosition(i, GetPositionOnArc(playheadPos, 0, _PlayheadZOffset));
        //    }

        //    for (int i = 0; i < _WaveformVizGrainPool.Length; i++)
        //    {
        //        if (_WaveformVizGrainPool[i].gameObject.activeSelf)
        //        {
        //            _WaveformVizGrainPool[i]._Lifetime -= Time.deltaTime;
        //            if (_WaveformVizGrainPool[i]._Lifetime <= 0)
        //                _WaveformVizGrainPool[i].gameObject.SetActive(false);
        //        }
        //    }
        //}


        if (_DrawTimeline)
        {
            // Grain timeline
            if (_YAxisPivot != null)
                _YAxisPivot.SetScaleY(_TimelineScale * _TimelineHeightCount);

            if (_XAxisPivot != null)
                _XAxisPivot.SetScaleX(_TimelineDistance);

            if (Application.isPlaying && _XAxisPivot_FrameTime != null)
                _XAxisPivot_FrameTime.SetScaleX(-GrainBrain.Instance._QueueDurationMS * .001f * (_TimelineDistance / _TimelineDuration));

            for (int i = 0; i < _TimelineBlocks.Length; i++)
            {
                if (_TimelineBlocks[i].gameObject.activeSelf)
                {
                    int sampleDiff = _TimelineBlocks[i]._StartIndex - GrainBrain.Instance.CurrentSampleIndex;
                    Vector3 pos = transform.position + transform.right * (sampleDiff / _SampleRate) * (_TimelineDistance / _TimelineDuration);
                    pos.y = _TimelineBlocks[i].transform.position.y;

                    _TimelineBlocks[i].transform.position = pos;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, transform.position + -transform.right * _TimelineDistance);
        Gizmos.DrawLine(transform.position, transform.position + transform.up * _TimelineScale * _TimelineHeightCount);

        for (int i = 1; i < 10; i++)
        {
            float norm0 = (i-1) / 9f;
            float norm1 = i / 9f;

            Gizmos.DrawLine(GetPositionOnArc(norm0, _WaveformBlockHeight *.5f), GetPositionOnArc(norm1, _WaveformBlockHeight * .5f));
        }

        for (int i = 1; i < 10; i++)
        {
            float norm0 = (i - 1) / 9f;
            float norm1 = i / 9f;

            Gizmos.DrawLine(GetPositionOnArc(norm0, _WaveformBlockHeight * -.5f), GetPositionOnArc(norm1, _WaveformBlockHeight * -.5f));
        }
    }
    #endregion

    Vector3 PosFromStartSampleIndex(int startSampleIndex)
    {
        int sampleDiff = startSampleIndex - GrainBrain.Instance.CurrentSampleIndex;
        Vector3 pos = transform.position + transform.right * (sampleDiff / _SampleRate) * (_TimelineDistance / _TimelineDuration);

        pos.y += (_IncrementY % _TimelineHeightCount) * _TimelineScale;
        pos.y += _TimelineScale * .5f;
        _IncrementY++;

        return pos;
    }

    public Vector3 GetPositionOnArc(float norm, float yOffset = 0, float radiusOffset = 0)
    {
        float radians = (-_ArcTotalAngle * .5f * Mathf.Deg2Rad) + (norm * _ArcTotalAngle * Mathf.Deg2Rad);

        Vector3 pos = Vector3.zero;

        pos.x = Mathf.Sin(radians) * (_ArcRadius + radiusOffset);
        pos.z = Mathf.Cos(radians) * (_ArcRadius + radiusOffset);
        pos.y = _WaveformHeight + yOffset;

        return pos;
    }
   
    public void EmitGrain(Grain grainData, int currentDSPSample)
    {
        if (_DrawTimeline)
        {
            // Scale based on duration
            float durationInSeconds = grainData._SizeInSamples / _SampleRate;
            durationInSeconds *= _TimelineDistance / _TimelineDuration;
            Vector3 size = new Vector3(durationInSeconds, _TimelineScale, .001f);

            GrainSynthVisualizerBlock block = _TimelineBlocks[_BlockCounter];
            block.transform.position = PosFromStartSampleIndex(grainData._DSPStartTime);
            block.transform.localScale = size;
            block._StartIndex = grainData._DSPStartTime;
            block.gameObject.SetActive(true);
            _BlockCounter++;
            _BlockCounter %= _TimelineBlocks.Length;
        }


        if (_DrawWaveform)
        {
            // Waveform grain
            WaveformVizGrain grain = _WaveformVizGrainPool[_WaveformGrainIndex];
            grain.transform.position = GetPositionOnArc(grainData._PlayheadNormalised, 0, -_PlayheadZOffset);

            // Width from duration
            float width = grainData._SizeInSamples / (float)_ClipSampleCount;
            grain.transform.localScale = new Vector3(width, _WaveformBlockHeight * .9f, 1);
            grain.transform.LookAt(_LookAtPos);
            grain._Lifetime = grainData._SampleData.Length / (float)_SampleRate;
            grain.gameObject.SetActive(true);

            _WaveformGrainIndex++;
            _WaveformGrainIndex %= _WaveformVizGrainPool.Length;
        }
    }
}
