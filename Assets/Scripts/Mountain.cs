using System;
using UnityEngine;
public class Mountain : MonoBehaviour
{
    const int maxResolution = 1000;
    public enum FunctionName { None, Time, Octaves, Frequency, Amplitude }

    [SerializeField] ComputeShader computeShader;
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    [SerializeField, Range(10, maxResolution)] int resolution = 500;
    [SerializeField, Range(1, 10)] int octaves = 3;
    [SerializeField, Range(2, 10)] int frequency = 4;
    [SerializeField, Range(2, 10)] int amplitude = 4;
    [SerializeField] FunctionName cycle;

    ComputeBuffer positionsBuffer;
    static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        octavesId = Shader.PropertyToID("_Octaves"),
        stepId = Shader.PropertyToID("_Step"),
        frequencyId = Shader.PropertyToID("_Frequency"),
        amplitudeId = Shader.PropertyToID("_Amplitude"),
        timeId = Shader.PropertyToID("_Time");

    private void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4);
    }
    private void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;
    }

    private void Update()
    {
        float step = 2f / resolution;
        int kernelIndex = (int)cycle;
        computeShader.SetInt(resolutionId, resolution);
        
        if (kernelIndex == 2)
        {
            octaves = Mathf.RoundToInt((Mathf.Sin(Time.time) * 9f / 2f + 11f / 2f));
        }
        else if (kernelIndex == 3)
        {
            frequency = Mathf.RoundToInt(Mathf.Sin(Time.time) * 4f + 6f);
            octaves = 2;
        }
        else if (kernelIndex == 4)
        {
            amplitude = Mathf.RoundToInt(Mathf.Sin(Time.time) * 4f + 6f);
        }

        computeShader.SetInt(octavesId, octaves);
        computeShader.SetInt(frequencyId, frequency);
        computeShader.SetInt(amplitudeId, amplitude); 
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);
        computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer);

        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);

        material.SetBuffer(positionsId, positionsBuffer);
        material.SetFloat(stepId, step);

        var rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        Graphics.RenderMeshPrimitives(rp, mesh, 0, resolution*resolution);
    }
}