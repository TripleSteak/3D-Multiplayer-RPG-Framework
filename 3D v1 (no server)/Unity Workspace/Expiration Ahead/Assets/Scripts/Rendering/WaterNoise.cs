using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterNoise : MonoBehaviour
{
    public float Power = 3;
    public float Scale = 1;
    public float TimeScale = 1;

    // keeps track of how much time passed for Perlin function
    private float xOffset;
    private float yOffset;

    private MeshFilter meshFilter;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    void Update()
    {
        MakeNoise();

        xOffset += Time.deltaTime * TimeScale;
        yOffset += Time.deltaTime * TimeScale;
    }

    void MakeNoise()
    {
        var vertices = meshFilter.mesh.vertices;

        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].z = CalculateHeight(vertices[i].x, vertices[i].y) * Power;
        }

        meshFilter.mesh.vertices = vertices;
        meshFilter.mesh.RecalculateBounds();
        meshFilter.mesh.RecalculateNormals();
    }

    float minX = 0, minY = 0, maxX = 0, maxY = 0;
    float CalculateHeight(float x, float y)
    {
        if (x < minX) minX = x;
        if (x > maxX) maxX = x;
        if (y < minY) minY = y;
        if (y > maxY) maxY = y;
        float xCoord = x * Scale + xOffset;
        float yCoord = y * Scale + yOffset;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }
}
