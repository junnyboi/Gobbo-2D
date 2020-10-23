using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeshController : MonoBehaviour
{
    public static MeshController Instance;
    World w { get { return WorldController.Instance.w; } }

    Mesh mesh;
    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    private void Awake()
    {
        // Initialize mesh
        Instance = this;

        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh = new Mesh();
        mesh.name = "Water mesh";

        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (!meshRenderer)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        //mesh = BuildRiverMesh();
    }

    public Mesh BuildWaterMesh()
    {
        Debug.Log("MeshController :: BuildWaterMesh");
        mesh.Clear(); // Removes any old data
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int offsetX = 0;
        // Create vertices and triangles geometry
        for (int x = 0; x < w.Width; x++)
        {
            offsetX = x * w.Height * 4;
            for (int y = 0; y < w.Height; y++)
            {
                int offsetXY = y * 4 + offsetX;
                // Create the 4 vertices we will use to create a square,
                // by combining the two triangles below
                vertices.Add(new Vector3(x + 0, y + 0, 0)); //lower left - at index 0
                vertices.Add(new Vector3(x + 1, y + 0, 0)); //lower right - at index 1
                vertices.Add(new Vector3(x + 0, y + 1, 0)); //upper left - at index 2
                vertices.Add(new Vector3(x + 1, y + 1, 0)); //upper right - at index 3

                // Bot-left triangle (wind clockwise)
                triangles.Add(offsetXY + 0);
                triangles.Add(offsetXY + 2);
                triangles.Add(offsetXY + 1);

                // Top-right triangle (wind clockwise)
                triangles.Add(offsetXY + 1);
                triangles.Add(offsetXY + 2);
                triangles.Add(offsetXY + 3);
            }
        }

        // Add UVs
        Vector2[] uvs = new Vector2[vertices.Count];

        for (int i = 0; i < uvs.Length; i++)
        {
            float uv_X = vertices[i].x / vertices.Last().x;
            float uv_Y = vertices[i].y / vertices.Last().y;
            uvs[i] = new Vector2(uv_X, uv_Y);
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }

    public void CreateTerrainMesh()
    {

    }

    public void BlendMesh()
    {
        // TODO: Blend neighbouring tiles of different terrains
    }
}
