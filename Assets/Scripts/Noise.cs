using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Unity.Mathematics.math;

public class Noise : MonoBehaviour
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor
    {
        [WriteOnly] public NativeArray<uint> hashes;
        public int resolution;
        public float invResolution;
        public SmallXXHash hash;

        public void Execute(int i)
        {
            int v = (int)floor(invResolution * i + 0.00001f);
            int u = i - resolution * v;

            hashes[i] = hash.Eat(u).Eat(v);
        }
    }

    const int maxResolution = 1000;
    public enum FunctionName { None, Time, Octaves, Frequency, Amplitude, Seeds }

    [SerializeField] ComputeShader computeShader;
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    [SerializeField, Range(10, maxResolution)] int resolution = 500;
    [SerializeField, Range(1, 10)] int octaves = 3;
    [SerializeField, Range(2, 10)] int frequency = 4;
    [SerializeField, Range(2, 10)] int amplitude = 4;
    [SerializeField] int seed;
    [SerializeField] FunctionName cycle;
    [SerializeField, Range(0.5f, 4f)] float instanceScale = 2f;

    ComputeBuffer positionsBuffer;
    ComputeBuffer hashesBuffer;
    [WriteOnly] NativeArray<uint> hashes;

    static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        hashesId = Shader.PropertyToID("_Hashes"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        octavesId = Shader.PropertyToID("_Octaves"),
        stepId = Shader.PropertyToID("_Step"),
        scaleId = Shader.PropertyToID("_InstanceScale"),
        frequencyId = Shader.PropertyToID("_Frequency"),
        amplitudeId = Shader.PropertyToID("_Amplitude"),
        timeId = Shader.PropertyToID("_Time");
    
    private void OnEnable()
    {
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4);
        hashesBuffer = new ComputeBuffer(resolution * resolution, 4);
        hashes = new NativeArray<uint>(resolution * resolution, Allocator.Persistent);
    }
    private void OnDisable()
    {
        positionsBuffer.Release();
        positionsBuffer = null;

        hashes.Dispose();
        hashesBuffer.Release();
        hashesBuffer = null;
    }

    private void Update()
    {
        int kernelIndex = (int)cycle;
        computeShader.SetInt(resolutionId, resolution);
        
        if (kernelIndex == 2)
        {
            octaves = Mathf.RoundToInt(Mathf.Sin(Time.time) * 9f / 2f + 11f / 2f);
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
        else if (kernelIndex == 5)
        {
            seed = Mathf.RoundToInt(Mathf.Sin(Time.time) * 4f + 6f);
        }

        new HashJob
        {
            hashes = hashes,
            resolution = resolution,
            invResolution = 1f / resolution,
            hash = SmallXXHash.Seed(seed)
        }.ScheduleParallel(hashes.Length, resolution, default).Complete();

        hashesBuffer.SetData(hashes);

        computeShader.SetInt(octavesId, octaves);
        computeShader.SetInt(frequencyId, frequency);
        computeShader.SetInt(amplitudeId, amplitude); 
        computeShader.SetFloat(stepId, 2f / resolution);
        computeShader.SetFloat(timeId, Time.time);
        computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer);
        computeShader.SetBuffer(kernelIndex, hashesId, hashesBuffer);

        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);

        material.SetBuffer(positionsId, positionsBuffer);
        material.SetBuffer(hashesId, hashesBuffer);
        material.SetFloat(scaleId, instanceScale / resolution);

        var rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        Graphics.RenderMeshPrimitives(rp, mesh, 0, resolution * resolution);
    }
}