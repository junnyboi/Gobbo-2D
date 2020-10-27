using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialShaderController : MonoBehaviour
{
    public static MaterialShaderController Instance;
    public static Dictionary<string, Material> materials = new Dictionary<string, Material>();
    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
        Material[] materialsLoaded = Resources.LoadAll<Material>("MaterialShaders/");
        foreach (Material mat in materialsLoaded)
            materials.Add(mat.name, mat);
    }
}
