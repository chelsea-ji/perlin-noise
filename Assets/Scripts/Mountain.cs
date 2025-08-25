using UnityEngine;
public class Mountain : MonoBehaviour
{
    const int maxResolution = 1000;

    [SerializeField] ComputeShader computeShader;
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;
    [SerializeField, Range(10, maxResolution)] int resolution = 500;
    [SerializeField, Range(1, 12)] int octaves = 3;
    [SerializeField, Range(2, 10)] int frequency = 4;
    [SerializeField, Range(2, 10)] int amplitude = 4;

    ComputeBuffer positionsBuffer;
    static readonly int
        positionsId = Shader.PropertyToID("_Positions"),
        resolutionId = Shader.PropertyToID("_Resolution"),
        octavesId = Shader.PropertyToID("_Octaves"),
        stepId = Shader.PropertyToID("_Step"),
        frequencyId = Shader.PropertyToID("_Frequency"),
        amplitudeId = Shader.PropertyToID("_Amplitude");

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
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetInt(octavesId, octaves);
        computeShader.SetInt(frequencyId, frequency);
        computeShader.SetInt(amplitudeId, amplitude); 
        computeShader.SetFloat(stepId, step);

        computeShader.SetBuffer(0, positionsId, positionsBuffer);

        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(0, groups, groups, 1);

        material.SetBuffer(positionsId, positionsBuffer);
        material.SetFloat(stepId, step);

        var rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        Graphics.RenderMeshPrimitives(rp, mesh, 0, resolution*resolution);
    }
}