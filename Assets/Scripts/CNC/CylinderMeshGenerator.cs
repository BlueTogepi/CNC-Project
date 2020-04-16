using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class CylinderMeshGenerator : MonoBehaviour
{
    // Axes specified in this script are relative to the piece's local coordination

    public float initialRadius = 0.05f;
    public float initialLength = 0.4f;
    public float vertexLinearSpacing = 0.005f;      // This need to be a factor of initialLength
    public float vertexAngularSpacing = 10f;        // This need to be a factor of 360, unit is in degree
    public string meshName = "Cylindrical Piece Mesh";
    public Material pieceMaterial;
    public GameObject bladeLeft;
    public GameObject bladeRight;

    [HideInInspector]
    public bool isActive;

    // Blade boundary markers should be placed on xy plane as seen from -z toward +z direction:
    //                | blade |                      +y
    //              Left-----Right                    ^   +z
    //                                                |  /
    //          ---------------------                 | /
    //          |       Piece       |                 |--------> +x

    private int linearVertexSize;       // Number of columns in the x axis (perpendicular axis to the cylinder's cross-sectional surface)
    private int angularVertexSize;      // Number of rows around the cylinder (360 degrees)
    private float[] currentRadius;
    private int baseVerticesIndex1;     // Index of the first vertex of the base
    private int baseVerticesIndex2;     // Index of the first vertex of the other base

    private bool[] isCutting;           // Boolean of whether each linearVertex is going to be cut by the blade (cuttingRadius < currentRadius)
    private Vector3 bladeLeftRelPos;
    private Vector3 bladeRightRelPos;

    // The indexing of vertices is      {0, 1, ..., angularVertexSize, (1 * angularVertexSize), (1 * angularVertexSize) + 1, ..., (1 * angularVertexSize) + (angularVertexSize - 1),
    //                                   (2 * angularVertexSize), (2 * angularVertexSize) + 1, ..., (2 * angularVertexSize) + (angularVertexSize - 1),
    //                                   ...,
    //                                   ((linearVertexSize - 1) * angularVertexSize), ((linearVertexSize - 1) * angularVertexSize) + 1, ..., linearVertexSize* angularVertexSize,
    //                                   baseVerticesIndex1, baseVerticesIndex1 + 1, ..., baseVerticesIndex1 + (angularVertexSize - 1),
    //                                   baseVerticesIndex1 + angularVertexSize,
    //                                   baseVerticesIndex2, baseVerticesIndex2 + 1, ..., baseVerticesIndex2 + (angularVertexSize - 1),
    //                                   baseVerticesIndex2 + angularVertexSize}
    //
    // Which represents                 {side vertices of the first row,
    //                                   side vertices of the second row,
    //                                   ...,
    //                                   side vertices of the last row,
    //                                   circular vertices of the first base,
    //                                   the center vertex of the first base,
    //                                   circular vertices of the other base,
    //                                   the center vertex of the other base}

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private void Awake()
    {
        isActive = true;
        mesh = new Mesh();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
    }

    // Start is called before the first frame update
    void Start()
    {
        InitVariables();
        GenerateMesh();
        PostGenerateMesh();

        /*int[] testArray1 = { 30, 31, 32, 33, 70, 71 };
        int[] testArray2 = { 0, 10, 34, 35, 36, 37, 38, 50, 51, 52, 75, 76, 77, 78, 79, 80 };
        ResizeMesh(testArray1, 0.04f);
        ResizeMesh(testArray2, 0.03f);*/
    }

    void FixedUpdate()
    {
        if (isActive)
        {
            bladeLeftRelPos = transform.InverseTransformPoint(bladeLeft.transform.position);
            bladeRightRelPos = transform.InverseTransformPoint(bladeRight.transform.position);
            if (IsBladeWithinBound())
            {
                int startInd = Mathf.Max(0, NearestLinearVerticesIndex(bladeLeftRelPos.x));
                int stopInd = Mathf.Min(linearVertexSize, NearestLinearVerticesIndex(bladeRightRelPos.x)) + 1;
                float radius = RadiusFromCenter(bladeLeftRelPos);
                SetCuttingIndex(startInd, stopInd, radius);
                ResizeMesh(radius);
            }
        }
    }

    public void OnDrawGizmos()
    {
        if (Application.isPlaying && isActive)
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawWireMesh(GetComponent<MeshFilter>().mesh, transform.position, transform.rotation, transform.lossyScale);
        }
    }

    /*public void FinishCutting()
    {
        isActive = false;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        GetComponent<MeshFilter>().sharedMesh = mesh;

        PostGenerateMesh();

        GetComponent<Rigidbody>().isKinematic = false;
        GetComponent<Rigidbody>().useGravity = true;
    }*/

    public void RePieceMesh()
    {
        GenerateMesh();
        PostGenerateMesh();
    }

    private void InitVariables()
    {
        linearVertexSize = (int)(initialLength / vertexLinearSpacing) + 1;
        angularVertexSize = (int)(360 / vertexAngularSpacing);

        mesh.name = meshName;
        vertices = new Vector3[(linearVertexSize * angularVertexSize) + (angularVertexSize * 2 + 2)];
        triangles = new int[((linearVertexSize - 1) * angularVertexSize * 6) + (angularVertexSize * 3 * 2)];
        currentRadius = new float[linearVertexSize];
        isCutting = new bool[linearVertexSize];

        baseVerticesIndex1 = linearVertexSize * angularVertexSize;
        baseVerticesIndex2 = baseVerticesIndex1 + angularVertexSize + 1;

        bladeLeftRelPos = transform.InverseTransformPoint(bladeLeft.transform.position);
        bladeRightRelPos = transform.InverseTransformPoint(bladeRight.transform.position);
    }

    #region MeshCreation/Editing

    private void GenerateMesh()
    {
        ResetRadiusArray();
        CreateVerticesArray();
        CreateTrianglesArray();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private void PostGenerateMesh()
    {
        if (pieceMaterial != null)
            meshRenderer.material = pieceMaterial;
        meshCollider.sharedMesh = mesh;
    }

    private void ResizeMesh(int[] editInd, float radius)
    {
        EditRadius(editInd, radius);
        EditVerticesArray(editInd);
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private void ResizeMesh(float radius)        // Only resize mesh vertices from isCutting
    {
        EditRadius(radius);
        EditVerticesArray();
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private void ResetRadiusArray()
    {
        for (int j = 0; j < linearVertexSize; j++)
        {
            currentRadius[j] = initialRadius;
        }
    }

    private void EditRadius(int[] editInd, float radius)
    {
        for (int i = 0; i < editInd.Length; i++)
        {
            currentRadius[editInd[i]] = radius;
        }
    }

    private void EditRadius(float radius)                       // Only edit radius from isCutting
    {
        for (int i = 0; i < linearVertexSize; i++)
        {
            if (isCutting[i])
            {
                currentRadius[i] = radius;
            }
        }
    }

    private void CreateVerticesArray()
    {
        // Side of cylinder
        for (int j = 0; j < linearVertexSize; j++)
        {
            _editVerticesArraySideByLinearIndex(j);
        }

        // Bases of cylinder
        _editVerticesArrayBases();
    }

    private void EditVerticesArray(int[] editInd)
    {
        for (int editIt = 0; editIt < editInd.Length; editIt++)
        {
            int j = editInd[editIt];
            _editVerticesArraySideByLinearIndex(j);
            if (j == 0 || j == linearVertexSize - 1)
            {
                _editVerticesArrayBases();
            }
        }
    }

    private void EditVerticesArray()                            // Only edit vertices from isCutting
    {
        for (int j = 0; j < linearVertexSize; j++)
        {
            if (isCutting[j])
            {
                _editVerticesArraySideByLinearIndex(j);
                if (j == 0 || j == linearVertexSize - 1)
                {
                    _editVerticesArrayBases();
                }
            }
        }
    }

    private void _editVerticesArraySideByLinearIndex(int j)
    {
        float r = currentRadius[j];
        int k = j * angularVertexSize;
        for (int i = 0; i < angularVertexSize; i++, k++)
        {
            float theta = Mathf.Deg2Rad * (vertexAngularSpacing * i);
            vertices[k] = new Vector3(vertexLinearSpacing * j, r * Mathf.Sin(theta), -r * Mathf.Cos(theta));
        }
    }

    private void _editVerticesArrayBases()
    {
        int k = baseVerticesIndex1;
        for (int i = 0; i < angularVertexSize; i++, k++)
        {
            vertices[k] = vertices[i];
        }
        vertices[k++] = new Vector3(0, 0, 0);

        k = baseVerticesIndex2;
        for (int i = (linearVertexSize - 1) * angularVertexSize; i < linearVertexSize * angularVertexSize; i++, k++)
        {
            vertices[k] = vertices[i];
        }
        vertices[k++] = new Vector3(initialLength, 0, 0);
    }

    private void CreateTrianglesArray()
    {
        int k = 0;
        // Quad Creation (Side of cylinder)
        for (int j = 0; j < linearVertexSize - 1; j++)
        {
            for (int i = 0; i < angularVertexSize; i++, k += 6)
            {
                // Local Indexing
                int bottomLeft = i;
                int topLeft = (bottomLeft + 1) % angularVertexSize;
                int bottomRight = bottomLeft + angularVertexSize;
                int topRight = topLeft + angularVertexSize;

                // Relative Indexing (relative to the piece coordinate system)
                bottomLeft += angularVertexSize * j;
                topLeft += angularVertexSize * j;
                bottomRight += angularVertexSize * j;
                topRight += angularVertexSize * j;

                triangles[k] = bottomLeft;
                triangles[k + 1] = topLeft;
                triangles[k + 2] = bottomRight;
                triangles[k + 3] = topLeft;
                triangles[k + 4] = topRight;
                triangles[k + 5] = bottomRight;
            }
        }

        // Triangle Creation (Base of cylinder)
        for (int i = 0; i < angularVertexSize; i++, k += 3)
        {
            triangles[k] = baseVerticesIndex1 + ((i + 1) % angularVertexSize);
            triangles[k + 1] = baseVerticesIndex1 + i;
            triangles[k + 2] = baseVerticesIndex1 + angularVertexSize;
        }
        
        for (int i = 0; i < angularVertexSize; i++, k += 3)
        {
            triangles[k] = baseVerticesIndex2 + i;
            triangles[k + 1] = baseVerticesIndex2 + ((i + 1) % angularVertexSize); ;
            triangles[k + 2] = baseVerticesIndex2 + angularVertexSize;
        }
    }

    #endregion

    #region Cutting/CheckingBoundary

    private void SetCuttingIndex(int startInd, int stopInd, float cutRadius)                  // stopInd not included
    {
        for (int i = 0; i < linearVertexSize; i++)
        {
            if (startInd <= i && i < stopInd && cutRadius < currentRadius[i])
            {
                isCutting[i] = true;
            }
            else
            {
                isCutting[i] = false;
            }
        }
    }

    private bool IsBladeWithinBound()
    {
        bool output = false;
        if (IsPointWithinBound(bladeLeftRelPos))
        {
            bladeLeft.GetComponent<ShowGizmosPoint>().SetActiveColor(true);
            output = true;
        }
        else
        {
            bladeLeft.GetComponent<ShowGizmosPoint>().SetActiveColor(false);
        }
        if (IsPointWithinBound(bladeRightRelPos))
        {
            bladeRight.GetComponent<ShowGizmosPoint>().SetActiveColor(true);
            output = true;
        }
        else
        {
            bladeRight.GetComponent<ShowGizmosPoint>().SetActiveColor(false);
        }
        return output;
    }

    private bool IsPointWithinBound(Vector3 pos)
    {
        Vector3 center = Vector3.zero;
        return (center.x <= pos.x && pos.x <= center.x + initialLength)
            && (RadiusFromCenter(pos) <= initialRadius);
    }

    private float RadiusFromCenter(Vector3 pos)
    {
        return Vector3.Distance(pos, new Vector3(pos.x, 0, 0));
    }

    private int NearestLinearVerticesIndex(float posX)
    {
        return Mathf.RoundToInt(posX / vertexLinearSpacing);
    }

    #endregion

    public void PrintRadius()
    {
        string s = "";
        for (int i = 0; i < currentRadius.Length; i++)
        {
            s += i + " " + currentRadius[i] + "\n";
        }
        print(s);
    }

    public void PrintVertices()
    {
        Mesh meshTest = GetComponent<MeshFilter>().mesh;
        string s = "";
        for (int i = 0; i < meshTest.vertices.Length; i++)
        {
            s += i + " (" + meshTest.vertices[i].x + ", " + meshTest.vertices[i].y + ", " + meshTest.vertices[i].z + ")" + "\n";
        }
        print(s);
    }

    public void PrintIsCutting()
    {
        string s = "";
        for (int i = 0; i < isCutting.Length; i++)
        {
            s += i + " " + isCutting[i] + "\n";
        }
        print(s);
    }
}
