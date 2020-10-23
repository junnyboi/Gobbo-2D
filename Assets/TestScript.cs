using System;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public const int Height = 30;
    public const int Width = 100;
    public const int SCAN_RADIUS = 2;

    float[] heightmap = new float[Width];
    GameObject[] heightGOs = new GameObject[Width];

    public MeshFilter meshFilter;
    MeshRenderer meshRenderer;
    Mesh mesh;

    void Awake()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        meshRenderer.material.color = Color.white;

        meshFilter.mesh = mesh = new Mesh();
        mesh.name = "Terrain mesh";

        for (int i = 0; i < Width; i++)
        {
            heightmap[i] = UnityEngine.Random.Range(1, Height);
        }

        Camera.main.transform.position = new Vector3(Width / 2, Height / 2, -Width);

        BuildMesh();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Smooth();
            BuildMesh();
        }
    }

    void Smooth()
    {
        for (int i = 0; i < heightmap.Length; i++)
        {
            float height = heightmap[i];

            float heightSum = 0;
            float heightCount = 0;

            for (int n = i - SCAN_RADIUS;
                     n < i + SCAN_RADIUS + 1;
                     n++)
            {
                if (n >= 0 &&
                    n < heightmap.Length)
                {
                    float heightOfNeighbour = heightmap[n];

                    heightSum += heightOfNeighbour;
                    heightCount++;
                }
            }

            float heightAverage = heightSum / heightCount;
            heightmap[i] = heightAverage;
        }
    }

    void BuildMesh()
    {
        mesh.Clear();
        List<Vector3> positions = new List<Vector3>();
        List<int> triangles = new List<int>();

        int offset = 0;
        for (int i = 0; i < Width - 1; i++)
        {
            offset = i * 4;

            float h = heightmap[i];
            float hn = heightmap[i + 1];
            positions.Add(new Vector3(i + 0, 0, 0)); //lower left - at index 0
            positions.Add(new Vector3(i + 1, 0, 0)); //lower right - at index 1
            positions.Add(new Vector3(i + 0, h, 0)); //upper left - at index 2
            positions.Add(new Vector3(i + 1, hn, 0)); //upper right - at index 3

            triangles.Add(offset + 0);
            triangles.Add(offset + 2);
            triangles.Add(offset + 1);

            triangles.Add(offset + 1);
            triangles.Add(offset + 2);
            triangles.Add(offset + 3);
        }

        mesh.vertices = positions.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }
}