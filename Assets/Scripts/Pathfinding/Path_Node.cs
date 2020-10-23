using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Path_Node<T>
{
    // conventional placeholder for node data
    public T data;

    public Path_Edge<T>[] edges;
}
