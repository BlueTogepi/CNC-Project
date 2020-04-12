using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class RectangularMeshGenerator : MonoBehaviour
{
    // Axes specified in this script are relative to the piece's local coordination

    public float initialLength = 0.4f;       // X-axis
    public float initialWidth = 0.2f;        // Z-axis
    public float initialHeight = 0.1f;
    public float vertexSpacing = 0.005f;     // This need to be a common factor of initialLength and initialWidth
    public string meshName = "Rectangular Piece Mesh";
    public Material pieceMaterial;
    public GameObject blade;

    public GameObject bladeTip;
    public int bladeLayer;

    // Blade tip boundary marker should be placed at the center of the furthest tip of the blade: (indicated by *)
    //                |   |   |   |
    //                | blade |   |                  +y
    //                |   |---|---|                   ^   +z
    //                |  /    |  /                    |  /
    //                | /   * | /                     | /
    //                ---------                       |--------> +x

    private int vertexSize1;       // Number of columns in the x axis
    private int vertexSize2;       // Number of rows in the z axis
    private float[,] currentHeight;
    private int sideVerticesIndex;       // Index of the first vertex of other 5 sides

    private bool[,] isCutting;           // Boolean of whether each vertex is going to be milled by the blade (cuttingHeight < currentHeight)
    private Collider bladeCollider;
    private Vector3 bladeTipRelPos;

    // The indexing of the vertices is      {(0, 0), (0, 1), ..., (0, vertexSize2),
    //                                       (1, 0), (1, 1), ..., (1, vertexSize2),
    //                                       ...,
    //                                       (vertexSize1, 0), ..., (vertexSize1, vertexSize2),
    //                                       sideVerticesIndex, ...}
    //
    // Which represents                     { Vertices on the top side in (x, z) coordination,
    //                                        (vertexSize1 + vertexSize2) * 2 * 2 vertices on the 4 sides, 4 vertices on the bottom side }
    //
    //                  [The order of the vertices on 4 sides are illustrated, bottom vertices are showed in the both pic]
    // 
    //                    side 1                                                      +y
    //               1_____-->______2---------> +z                                     ^
    //               |              |                                Vertices order    |
    //        side 3 V              V side 4                      ______<-------_______|  1st: Top 4 edges [(vertexSize1 + vertexSize2) * 2 vertices]
    //               |              |                            |                     |
    //               3_____-->______4                            |       side 1        |
    //               |    side 2                                 |                     |
    //               |                             +z <----------2______<-------_______1  Then, 2nd: bottom 4 edges 2nd [another (vertexSize1 + vertexSize2) * 2 vertices]
    //               V                                           
    //               +x
    // 
    //        Order of vertices at the edges from 4 sides will be incremented toward positive axis, all top edges then all bottom edges

    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;

    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private void Awake()
    {
        mesh = new Mesh();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        bladeCollider = blade.GetComponent<Collider>();
    }

    // Start is called before the first frame update
    void Start()
    {
        InitVariables();
        GenerateMesh();
        PostGenerateMesh();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        bladeTipRelPos = transform.InverseTransformPoint(bladeTip.transform.position);
        if(IsBladeWithinPieceBound())
        {
            SetCuttingIndex();
            float height = bladeTipRelPos.y;
            ResizeMesh(height);
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.grey;
            Gizmos.DrawWireMesh(GetComponent<MeshFilter>().mesh, transform.position, transform.rotation, transform.lossyScale);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(meshCollider.bounds.center, meshCollider.bounds.size);
            Gizmos.DrawWireCube(bladeCollider.bounds.center, bladeCollider.bounds.size);

            /*Gizmos.color = Color.blue;
            for (int k = 0; k < sideVerticesIndex; k++)
            {
                if (IsPointRelWithinBladeBound(vertices[k]))
                {
                    Gizmos.DrawSphere(transform.TransformPoint(vertices[k]), 0.002f);
                }
            }
            Gizmos.color = Color.black;
            for (int i = 0; i < vertexSize1; i++)
            {
                for (int j = 0; j < vertexSize2; j++)
                {
                    if (isCutting[i, j])
                    {
                        Gizmos.DrawSphere(transform.TransformPoint(vertices[(i * vertexSize2) + j]), 0.002f);
                    }
                }
            }*/
        }
    }

    private void InitVariables()
    {
        vertexSize1 = (int)(initialLength / vertexSpacing) + 1;
        vertexSize2 = (int)(initialWidth / vertexSpacing) + 1;

        mesh.name = meshName;
        vertices = new Vector3[(vertexSize1 * vertexSize2) + ((vertexSize1 + vertexSize2) * 4 + 4)];
        triangles = new int[((vertexSize1 - 1) * (vertexSize2 - 1) * 6) + ((vertexSize1 + vertexSize2 - 2) * 2 * 6) + 6];
        currentHeight = new float[vertexSize1, vertexSize2];
        isCutting = new bool[vertexSize1, vertexSize2];

        sideVerticesIndex = vertexSize1 * vertexSize2;

        bladeTipRelPos = transform.InverseTransformPoint(bladeTip.transform.position);
    }

    #region MeshCreation/Editing

    private void GenerateMesh()
    {
        ResetHeightArray();
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

    private void ResizeMesh(float height)        // Only resize mesh vertices from isCutting
    {
        EditHeightArray(height);
        EditVerticesArray();
        mesh.vertices = vertices;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    private void ResetHeightArray()
    {
        for (int i = 0; i < vertexSize1; i++)
        {
            for (int j = 0; j < vertexSize2; j++)
            {
                currentHeight[i, j] = initialHeight;
            }
        }
    }

    private void EditHeightArray(float height)
    {
        for (int i = 0; i < vertexSize1; i++)
        {
            for (int j = 0; j < vertexSize2; j++)
            {
                if (isCutting[i, j])
                {
                    currentHeight[i, j] = Mathf.Min(currentHeight[i, j], height);
                }
            }
        }
    }

    private void CreateVerticesArray()
    {
        // Top of the piece
        _editVerticesArrayTop();

        // Side of the piece
        _editVerticesArrayTopEdges();
        _editVerticesArrayBottomEdges();

        // Bottom of the piece
        _editVerticesArrayBottom();
    }

    private void EditVerticesArray()
    {
        // Updating only the vertices that associated with currentHeight
        _editVerticesArrayTop();
        _editVerticesArrayTopEdges();
    }

    private void _editVerticesArrayTop()
    {
        for (int i = 0; i < vertexSize1; i++)
        {
            for (int j = 0; j < vertexSize2; j++)
            {
                __editVerticesArrayTopByIndex(i, j);
            }
        }
    }

    private void __editVerticesArrayTopByIndex(int i, int j)
    {
        vertices[(i * vertexSize2) + j] = new Vector3(vertexSpacing * i, currentHeight[i, j], vertexSpacing * j);
    }

    private void _editVerticesArrayTopEdges()
    {
        int k = sideVerticesIndex;
        for (int j = 0; j < vertexSize2; j++, k++)
        {
            __editVerticesArrayTopEdgeByVerticesIndex(0, j, k);
            __editVerticesArrayTopEdgeByVerticesIndex(vertexSize1 - 1, j, k + vertexSize2);
        }
        k = sideVerticesIndex + (vertexSize2 * 2);
        for (int i = 0; i < vertexSize1; i++, k++)
        {
            __editVerticesArrayTopEdgeByVerticesIndex(i, 0, k);
            __editVerticesArrayTopEdgeByVerticesIndex(i, vertexSize2 - 1, k + vertexSize1);
        }
    }

    private void _editVerticesArrayBottomEdges()
    {
        int k = sideVerticesIndex + (vertexSize2 * 2) + (vertexSize1 * 2);
        for (int j = 0; j < vertexSize2; j++, k++)
        {
            __editVerticesArrayBottomEdgeByVerticesIndex(0, j, k);
            __editVerticesArrayBottomEdgeByVerticesIndex(vertexSize1 - 1, j, k + vertexSize2);
        }
        k = sideVerticesIndex + (vertexSize2 * 4) + (vertexSize1 * 2);
        for (int i = 0; i < vertexSize1; i++, k++)
        {
            __editVerticesArrayBottomEdgeByVerticesIndex(i, 0, k);
            __editVerticesArrayBottomEdgeByVerticesIndex(i, vertexSize2 - 1, k + vertexSize1);
        }
    }

    private void __editVerticesArrayTopEdgeByVerticesIndex(int i, int j, int k)            // ***This does not check for inner vertices
    {
        vertices[k] = vertices[(i * vertexSize2) + j];
    }

    private void __editVerticesArrayBottomEdgeByVerticesIndex(int i, int j, int k)         // ***This does not check for inner vertices
    {
        vertices[k] = new Vector3(vertexSpacing * i, 0, vertexSpacing * j);
    }

    private void _editVerticesArrayBottom()
    {
        int k = sideVerticesIndex + (vertexSize2 * 4) + (vertexSize1 * 4);
        vertices[k] = new Vector3(0, 0, 0);
        vertices[k + 1] = new Vector3(0, 0, initialWidth);
        vertices[k + 2] = new Vector3(initialLength, 0, 0);
        vertices[k + 3] = new Vector3(initialLength, 0, initialWidth);
    }

    private void CreateTrianglesArray()
    {
        int k = 0;
        int bottomLeft;
        int topLeft;
        int bottomRight;
        int topRight;

        // Top of the piece
        for (int i = 0; i < vertexSize1 - 1; i++)
        {
            for (int j = 0; j < vertexSize2 - 1; j++, k += 6)
            {
                bottomLeft = (vertexSize2 * i) + j;
                topLeft = bottomLeft + 1;
                bottomRight = bottomLeft + vertexSize2;
                topRight = bottomRight + 1;
                _createQuad(bottomLeft, topLeft, topRight, bottomRight, k);
            }
        }

        // Sides of the piece
        int top2bottom = (vertexSize1 + vertexSize2) * 2;
        for (int i = 0; i < vertexSize2 - 1; i++, k += 12)
        {
            topRight = sideVerticesIndex + i;
            topLeft = topRight + 1;
            bottomRight = topRight + top2bottom;
            bottomLeft = bottomRight + 1;
            _createQuad(bottomLeft, topLeft, topRight, bottomRight, k);

            topLeft = topRight + vertexSize2;
            topRight = topLeft + 1;
            bottomLeft = topLeft + top2bottom;
            bottomRight = bottomLeft + 1;
            _createQuad(bottomLeft, topLeft, topRight, bottomRight, k + 6);
        }
        for (int i = 0; i < vertexSize1 - 1; i++, k += 12)
        {
            topLeft = sideVerticesIndex + (vertexSize2 * 2) + i;
            topRight = topLeft + 1;
            bottomLeft = topLeft + top2bottom;
            bottomRight = bottomLeft + 1;
            _createQuad(bottomLeft, topLeft, topRight, bottomRight, k);

            topRight = topLeft + vertexSize1;
            topLeft = topRight + 1;
            bottomRight = topRight + top2bottom;
            bottomLeft = bottomRight + 1;
            _createQuad(bottomLeft, topLeft, topRight, bottomRight, k + 6);
        }

        // Bottom of the piece
        _createQuad(vertices.Length - 3, vertices.Length - 4, vertices.Length - 2, vertices.Length - 1, k);
    }

    private void _createQuad(int bottomLeft, int topLeft, int topRight, int bottomRight, int k)
    {
        triangles[k] = bottomLeft;
        triangles[k + 1] = topLeft;
        triangles[k + 2] = bottomRight;
        triangles[k + 3] = topLeft;
        triangles[k + 4] = topRight;
        triangles[k + 5] = bottomRight;
    }

    #endregion

    #region Cutting/CheckingBoundary

    private void SetCuttingIndex()
    {
        int k;
        int layerMask = 1 << bladeLayer;
        Vector3 offset;
        RaycastHit hit;
        for (int i = 0; i < vertexSize1; i++)
        {
            for (int j = 0; j < vertexSize2; j++)
            {
                isCutting[i, j] = false;
                k = (vertexSize2 * i) + j;
                if (IsPointRelWithinBladeBound(vertices[k]))
                {
                    offset = bladeCollider.bounds.center - transform.TransformPoint(vertices[k]);
                    if (!Physics.Raycast(transform.TransformPoint(vertices[k]), offset.normalized, out hit, offset.magnitude * 1.1f, layerMask))
                    {
                        isCutting[i, j] = true;
                    }
                }
            }
        }
    }

    private void ResetCuttingIndex()
    {
        for (int i = 0; i < vertexSize1; i++)
        {
            for (int j = 0; j < vertexSize2; j++)
            {
                isCutting[i, j] = false;
            }
        }
    }

    private bool IsBladeWithinPieceBound()
    {
        bool output = false;
        if (meshCollider.bounds.Contains(bladeTip.transform.position))
        {
            bladeTip.GetComponent<ShowGizmosPoint>().SetActiveColor(true);
            output = true;
        }
        else
        {
            bladeTip.GetComponent<ShowGizmosPoint>().SetActiveColor(false);
        }
        return output;
    }

    private bool IsPointRelWithinBladeBound(Vector3 relPos)
    {
        return bladeCollider.bounds.Contains(transform.TransformPoint(relPos));
    }

    private int NearestVerticesIndexX(float posX)
    {
        return Mathf.RoundToInt(posX / vertexSpacing);
    }

    private int NearestVerticesIndexZ(float posZ)
    {
        return Mathf.RoundToInt(posZ / vertexSpacing);
    }

    #endregion

    public void PrintHeight()
    {
        for (int i = 0; i < vertexSize1; i++)
        {
            for (int j = 0; j < vertexSize2; j++)
            {
                print(i + " " + j + ": " + currentHeight[i, j]);
            }
        }
    }
}
