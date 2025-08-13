using UnityEngine;
public class Mountain : MonoBehaviour
{
    [SerializeField] Transform pointPrefab;
    [SerializeField] int gridSize = 360;
    [SerializeField, Range(1,12)] int octaves = 10;
    [SerializeField, Range(1, 10)] int scale = 1;
    float[,] map;
    Vector2[,] vectors;

    private void Awake()
    {
        map = new float[gridSize, gridSize];
        vectors = new Vector2 [gridSize*octaves, gridSize*octaves];

        for (int i = 0; i < vectors.GetLength(0); i++)
        {
            for (int j = 0; j < vectors.GetLength(1); j++)
            {
                Vector2 vector;
                float randomAngle = Random.Range(0, 360);
                randomAngle = randomAngle * (Mathf.PI / 180f);
                vector.x = Mathf.Cos(randomAngle);
                vector.y = Mathf.Sin(randomAngle);
                vectors[i, j] = vector;
            }
        }

        // input all coordinate points into noise function
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                float value = 0;
                float frequency = 1;
                float amplitude = 1;

                for (int k = 0; k < octaves; k++)
                {
                    value += perlin(x * frequency / gridSize, y * frequency / gridSize, vectors) * amplitude;

                    frequency *= 2;
                    amplitude /= 2;
                }

                if (value < -1.0f)
                {
                    value = -1.0f;
                }
                value = (value + 1.0f) / 2.0f * 100 * scale;

                map[x,y] = value;
            }
        }

        Vector3 position;
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                Transform point = Instantiate(pointPrefab);
                position.x = x;
                position.z = y;
                position.y = (int)(map[x, y]);
                point.localPosition = position;
                point.SetParent(transform);
            }
        }
    }

    private float dotGradient(Vector2 influence, int x0, int y0, float x, float y)
    {
        Vector2 distance = new Vector2(x - (float)x0, y - (float)y0);
        return Vector2.Dot(distance, influence);
    }

    // linear interpolation between two values with a given weight
    private float lerp(float val1, float val2, float w)
    {
        return val1 + w * (val2 - val1);
    }

    //fade function applied to lerp weights to smooth grid lines
    private float fade(float t)
    {
        return t * t * t * (t * (t * 6 - 15) + 10);
    }

    //calculates value from 0-1 given a coordinate point and list of influence vectors for a grid
    private float perlin(float x, float y, Vector2[,] vectors)
    {
        // calculate corners of the grid cell the point is located in
        int x0 = (int)x;
        int y0 = (int)y;
        int x1 = x0 + 1;
        int y1 = y0 + 1;

        // horizontal and vertical weights (distance between point and upper left corner)
        float hw = x - (float)x0;
        float vw = y - (float)y0;

        hw = fade(hw);
        vw = fade(vw);

        // dot products for upper two corners
        float dot0 = dotGradient(vectors[y0, x0], x0, y0, x, y);
        float dot1 = dotGradient(vectors[y0, x1], x1, y0, x, y);
        float i0 = lerp(dot0, dot1, hw);

        // dot products for bottom two corners
        dot0 = dotGradient(vectors[y1, x0], x0, y1, x, y);
        dot1 = dotGradient(vectors[y1, x1], x1, y1, x, y);
        float i1 = lerp(dot0, dot1, hw);

        float value = lerp(i0, i1, vw);

        return value;
    }
}
