 using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(RoadsGenerator))]
public class RoadsGeneratorInspector : Editor {
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        RoadsGenerator script = (RoadsGenerator)target;
        if (GUILayout.Button("Generate"))
        {
            script.Generate();
        }
        if (GUILayout.Button("Preview path"))
        {
            script.ApplySmoothing();
        }
        if (GUILayout.Button("Clear Smooth Points"))
        {
            script.ClearSmoothingPoints();
        }
    }
}

#endif
public class RoadsGenerator : MonoBehaviour
{
    [SerializeField] Material roadsMat;
    [SerializeField] float groundOffset;
    [SerializeField] float roadWidth;
    [SerializeField] float smoothing = 1;
    [SerializeField] float maxSmoothingPointDistance;
    [SerializeField] float roadHeight;
    [SerializeField] float sideExtrusion;
    [SerializeField] float roadPaintDistancing;
    [SerializeField] int groundClippingElimination;
    [SerializeField] bool debugMode;
    [SerializeField] LayerMask groundLayer;
    private void Start()
    {
    }

    Vector3[] verts;
    Vector2[] uvs;
    int[] tris;
    Transform[] points;

    struct pair {
        public Vector3 left;
        public Vector3 right;
    }
    public void Generate()
    {
        GameObject oldRoad = GameObject.Find("Road_Generated");
        DestroyImmediate(oldRoad);

        ClearSmoothingPoints();
        points = new Transform[transform.childCount];

        GameObject newGo = new GameObject("Road_Generated", typeof(MeshFilter), typeof(MeshRenderer));

        ApplySmoothing();

        GetPoints();


        verts = new Vector3[points.Length * 2];
        tris = new int[(points.Length - 1) * 6];


        Vector3 dir = points[1].position - points[0].position;
        dir = Vector3.Cross(dir.normalized, Vector3.down).normalized;

        pair vertPos = GetVertPos(points[0], dir);
        verts[0] = vertPos.left;
        verts[1] = vertPos.right;

        GetPoints();
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 pos = points[i].position;
            pos.y = points[0].position.y;
            points[i].position = pos;
        }

        //compute vertices
        for (int i = 0; i < points.Length; i++)
        {
            if (i < 1)
            {
                dir = points[i + 1].position - points[i].position;
                dir = Vector3.Cross(dir.normalized, Vector3.down).normalized;
                //Debug.DrawRay(points[i].position, dir * 5,Color.blue, 60);
            }
            else if (i >= points.Length - 1)
            {
                dir = points[i].position - points[i - 1].position;
                dir = Vector3.Cross(dir.normalized, Vector3.down).normalized;
            }
            else
            {
                int sign = 1;
                float angle = Vector3.SignedAngle(points[i - 1].position - points[i].position, points[i + 1].position - points[i].position, Vector3.up);
                float unsignedAngle = Mathf.Abs(angle);
                float angleMargin = 2f;

                if (angle > -90)
                {
                    sign = -1;
                }


                if (unsignedAngle < 180 + angleMargin && unsignedAngle > 180 - angleMargin)
                {
                    dir = points[i + 1].position - points[i].position;
                    dir = Vector3.Cross(dir.normalized, Vector3.down).normalized;
                }
                else {
                    dir = (points[i - 1].position - points[i].position).normalized + (points[i + 1].position - points[i].position).normalized;
                    dir *= sign;
                }

                //print(Vector3.SignedAngle(points[i - 1].position - points[i].position, points[i + 1].position - points[i].position, Vector3.up));

                //Debug.DrawRay(points[i].position, dir.normalized * 5,Color.red, 60);
            }
            dir.Normalize();
            //dir = points[2].position - points[1].position;
            //dir = Vector3.Cross(dir.normalized, Vector3.down).normalized;
            vertPos = GetVertPos(points[i], dir);
            verts[2 * i] = vertPos.left;
            verts[2 * i + 1] = vertPos.right;
        }


        //compute tris
        for (int i = 0; i < points.Length - 1; i++)
        {
            tris[0 + i * 6] = 0 + i * 2;
            tris[1 + i * 6] = 2 + i * 2;
            tris[2 + i * 6] = 1 + i * 2;
            tris[3 + i * 6] = 1 + i * 2;
            tris[4 + i * 6] = 2 + i * 2;
            tris[5 + i * 6] = 3 + i * 2;
        }


        //Generate sides

        Mesh newMesh = new Mesh();
        newMesh.vertices = verts;
        newMesh.triangles = tris;
        newMesh.name = "GeneratedRoadMesh";
        newGo.tag = "Stone";
        GenerateRoadSides(newMesh);

        CalculateUVs(newMesh);
        newMesh.RecalculateNormals();
        newGo.AddComponent<MeshCollider>();
        newGo.GetComponent<MeshCollider>().sharedMesh = newMesh;

        //SmoothMeshSharpAngles(newMesh);

        newGo.transform.position = transform.position;
        newGo.GetComponent<MeshFilter>().mesh = newMesh;
        newGo.GetComponent<MeshRenderer>().sharedMaterial = roadsMat;
    }
    void SmoothMeshSharpAngles(Mesh mesh) {
        /*
         
        2____3
        |\  |
        | \ |
        |  \|
       0_____1
         
         */

        int[] tris = mesh.triangles;
        Vector3[] verts = mesh.vertices;
        int vertCount = verts.Length;

        //find the new length of the verts array
        for (int i = 1; i < points.Length - 1; i++)
        {
            Vector3 A = points[i - 1].position;
            Vector3 B = points[i].position;
            Vector3 C = points[i + 1].position;
            if (Vector3.Angle((A - B).normalized, (C - B).normalized) >= 90)
            {
                vertCount += 2;
            }
        }

        //resize the array
        Vector3[] newVerts = new Vector3[vertCount];
        for (int i = 0; i < verts.Length; i++)
        {
            newVerts[i] = verts[i];
        }
        verts = newVerts;


        for (int i = 1; i < points.Length - 1; i++)
        {

            Vector3 A = points[i - 1].position;
            Vector3 B = points[i].position;
            Vector3 C = points[i + 1].position;
            if (Vector3.Angle((A - B).normalized, (C - B).normalized) >= 90) {
                continue;
            }


            Vector3 bis = (A - B).normalized + (C - B).normalized;
            Vector3 perp = Vector3.Cross(bis, Vector3.down).normalized;
            float angleBack = Vector3.Angle(perp, (A - B).normalized);
            float angleFwd = Vector3.Angle(perp, (C - B).normalized);
            if (angleFwd < angleBack)
            {//reverse the perpendicular
             //perpendicular should be facing the backwards direction
                perp *= -1;
            }

            Vector3 leftVert = verts[i * 2];
            Vector3 rightVert = verts[i * 2 + 1];
            Vector3 correctPoint = B + perp * roadWidth / 2;

            Vector3 smoothingPoint;

            if (Vector3.Distance(leftVert, correctPoint) < Vector3.Distance(rightVert, correctPoint))
            {
                //use the left vert as smoothing point
                smoothingPoint = leftVert;
            }
            else {
                //use right vert
                smoothingPoint = rightVert;
            }

            Vector3 newVertPos = smoothingPoint + (perp * 1.5f + (B - A).normalized).normalized * 3f;



        }
    }
    void GetPoints() {
        points = new Transform[transform.childCount];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = transform.GetChild(i);
        }
    }
    public void ClearSmoothingPoints() {
        GameObject[] smoothPts = GameObject.FindGameObjectsWithTag("EditorOnly");
        for (int i = 0; i < smoothPts.Length; i++)
        {
            DestroyImmediate(smoothPts[i]);
        }
    }
    public void ApplySmoothing() {
        ClearSmoothingPoints();

        for (int i = 0; i < smoothing; i++)
        {
            GetPoints();

            SmoothPath();

            points = new Transform[transform.childCount];
        }
        for (int i = 0; i < groundClippingElimination; i++)
        {
            EliminateClipping();
        } 
    }
    void SmoothPath()
    {
        int offset = 0;
        int length = points.Length;

        //smooth first point
        Vector3 B = transform.GetChild(0).position;
        Vector3 C = transform.GetChild(1).position;
        Vector3 A;

        //Debug.DrawRay(B, (C-B).normalized * 10, Color.yellow, 100);

        Transform trans1 = new GameObject("SmoothingPoint").transform;
        trans1.gameObject.tag = "EditorOnly";

        trans1.SetParent(transform);

        float l1 = Vector3.Distance(C, B) / 4;

        trans1.position = (B + (C-B).normalized * (l1 > maxSmoothingPointDistance ? maxSmoothingPointDistance : l1));

        trans1.SetSiblingIndex(1);
        offset += 1;
        

        //smooth path points
        for (int i = 1; i < length - 1; i++)
         {
            A = transform.GetChild(i - 1 + offset).position;
            B = transform.GetChild(i + offset).position;
            C = transform.GetChild(i + 1 + offset).position;
            Vector3 bis = (A - B).normalized + (C - B).normalized;

            //Debug.DrawRay(B,bis.normalized * 10,Color.blue,100);
            //Debug.DrawRay(B,(A-B).normalized * 5,Color.red,100);
            //Debug.DrawRay(B,(C-B).normalized * 5,Color.green,100);

            Vector3 perp = Vector3.Cross(bis, Vector3.down).normalized;
            
            //Debug.DrawRay(B - perp * 5,perp * 10 ,Color.yellow,100);
            //The perpendicular should be closer to the backwards direction
            float angleBack = Vector3.Angle(perp,(A-B).normalized);
            float angleFwd = Vector3.Angle(perp,(C-B).normalized);
            if (angleFwd < angleBack)
            {//reverse the perpendicular
                perp *= -1;
            }
            
            trans1 = new GameObject("SmoothingPoint").transform;
            Transform trans2 = new GameObject("SmoothingPoint").transform;
            trans1.gameObject.tag = "EditorOnly";
            trans2.gameObject.tag = "EditorOnly";

            trans1.SetParent(transform);
            trans2.SetParent(transform);

            l1 = Vector3.Distance(A,B)/4;
            float l2 = Vector3.Distance(C,B)/4;

            trans1.position = (B + ((A - B).normalized + perp.normalized) * (l1 > maxSmoothingPointDistance ? maxSmoothingPointDistance : l1));
            trans2.position = (B + ((C - B).normalized - perp.normalized) * (l2 > maxSmoothingPointDistance ? maxSmoothingPointDistance : l2));
            //trans2.position = dir;

            trans1.SetSiblingIndex(i + offset);
            trans2.SetSiblingIndex(i + 2 + offset);
            offset += 2;
        }


        //smooth last point
        A = transform.GetChild(transform.childCount - 2).position;
        B = transform.GetChild(transform.childCount - 1).position;

        //Debug.DrawRay(B, (C - B).normalized * 10, Color.yellow, 100);

        trans1 = new GameObject("SmoothingPoint").transform;
        trans1.gameObject.tag = "EditorOnly";

        trans1.SetParent(transform);

        l1 = Vector3.Distance(A, B) / 4;

        trans1.position = (B + (A - B).normalized * (l1 > maxSmoothingPointDistance ? maxSmoothingPointDistance : l1));

        trans1.SetSiblingIndex(transform.childCount - 2);
    }
    pair GetVertPos(Transform point, Vector3 dir) {

        pair newPair = new pair();
        newPair.left = transform.InverseTransformPoint(CastDown(point.position + -dir * roadWidth * 0.5f)) + Vector3.up * groundOffset;
        newPair.right = transform.InverseTransformPoint(CastDown(point.position + dir * roadWidth * 0.5f)) + Vector3.up * groundOffset;

        
        float averageHeight = transform.TransformPoint(newPair.left).y + transform.TransformPoint(newPair.right).y;
        averageHeight /= 2;
        float offset = 0;

        Vector3 terrainHeight = CastDown(point.position);
        if (averageHeight < terrainHeight.y) {
            offset = terrainHeight.y - averageHeight;
        }

        newPair.left += Vector3.up * (offset + groundOffset);
        newPair.right += Vector3.up * (offset + groundOffset);
        
        return newPair;
    }
    Vector3 CastDown(Vector3 pos) {
        if(debugMode)Debug.DrawRay(pos + Vector3.up * 100, Vector3.down * 500, Color.green, 5);
        
        if (Physics.Raycast(pos + Vector3.up * 100, Vector3.down * 500, out RaycastHit hit,500,groundLayer))
        {
            return hit.point;
        }
        return pos;
    }
    void ComputeTris() {
        int squareCount = (points.Length - 1);
        for (int i = 0; i < squareCount; i++)
        {
            tris[0 + i * 6] = 0 + 2 * i;
            tris[1 + i * 6] = 1 + 2 * i;
            tris[2 + i * 6] = 2 + 2 * i;
            tris[3 + i * 6] = 2 + 2 * i;
            tris[4 + i * 6] = 1 + 2 * i;
            tris[5 + i * 6] = 3 + 2 * i;
        }
    }
    void CalculateUVs(Mesh mesh) {
        GetPoints();
        Vector2[] uvs = new Vector2[mesh.vertexCount];
        Vector3[] verts = mesh.vertices;
        float yOffset = 0;

        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        float leftDist = 0;
        float rightDist = 0;
        for (int i = 2; i < uvs.Length/2; i++)
        {
            leftDist = UVDistance( Vector3.Distance(verts[i], verts[i - 2]));
            rightDist = UVDistance(Vector3.Distance(verts[i + 1], verts[i - 1]));
            uvs[i] = new Vector2(0.04f, leftDist + yOffset);
            uvs[i + 1] = new Vector2(0.96f, rightDist + yOffset);
            i++;
            yOffset += (leftDist + rightDist) / 2;
        }
        for (int i = uvs.Length / 2; i < uvs.Length; i++)
        {
            leftDist = UVDistance(Vector3.Distance(verts[i], verts[i - 2]));
            rightDist = UVDistance(Vector3.Distance(verts[i + 1], verts[i - 1]));
            uvs[i] = new Vector2(0, leftDist + yOffset);
            uvs[i + 1] = new Vector2(1, rightDist + yOffset);
            i++;
            yOffset += (leftDist + rightDist) / 2;
        }
        uvs[verts.Length / 2].y = uvs[0].y + 0.5f;
        uvs[verts.Length / 2 + 1].y = uvs[1].y + 0.5f;

        uvs[verts.Length / 2].y = 0;
        uvs[verts.Length / 2 + 1].y = 1;


        mesh.uv = uvs;
    }
    float UVDistance(float worldDistance) {
        return worldDistance / roadPaintDistancing;
    }

    void GenerateRoadSides(Mesh mesh) {

        GetPoints();

        Vector3[] verts = mesh.vertices;
        int v = verts.Length;
        int[] tris = mesh.triangles;

        int orig_vertsCount = verts.Length;
        Vector3[] newVerts = new Vector3[verts.Length * 2];

        for (int i = 0; i < verts.Length; i++)
        {
            newVerts[i] = verts[i];
        }
        verts = newVerts;
        
        for (int i = 0; i < points.Length; i++)
        {
            Vector3 left = verts[i * 2];
            Vector3 right = verts[i * 2 + 1];
            verts[v + i * 2] = left + Vector3.down * roadHeight + (left - right).normalized * sideExtrusion;
            verts[v + i * 2 + 1] = right + Vector3.down * roadHeight + (right - left).normalized * sideExtrusion;
        }
        verts[v] += (verts[v] - verts[v + 2]).normalized * sideExtrusion;
        verts[v+1] += (verts[v + 1] - verts[v + 3]).normalized * sideExtrusion;
        
        verts[v*2 - 1] += (verts[v * 2 - 1] - verts[v * 2 - 3]).normalized * sideExtrusion;
        verts[v*2 - 2] += (verts[v * 2 - 2] - verts[v * 2 - 4]).normalized * sideExtrusion;
        
         int t = tris.Length;

        int[] newTris = new int[t * 3 + 2 * 6];// + 2 square for start and end
        for (int i = 0; i < tris.Length; i++)
        {
            newTris[i] = tris[i];
        }
        tris = newTris;

        /*
        tris[t + 0] = v + 0;//28
        tris[t + 1] = v + 2;
        tris[t + 2] = 0;
        tris[t + 3] = 0;
        tris[t + 4] = v + 2;
        tris[t + 5] = 2;
                */
        //compute tris

        for (int i = 0; i < points.Length - 1; i++)
        {
            //stanga
            tris[t + 0 + i * 12] = v + i*2;
            tris[t + 1 + i * 12] = v + (i+1) * 2;
            tris[t + 2 + i * 12] = i * 2;
            tris[t + 3 + i * 12] = i*2;
            tris[t + 4 + i * 12] = v + (i + 1) * 2;
            tris[t + 5 + i * 12] = (i+1)* 2;
            //dreapta
            tris[t + 6 + i * 12] = i*2 + 1;
            tris[t + 7 + i * 12] = (i+1) * 2 + 1;
            tris[t + 8 + i * 12] = v + i*2 + 1;
            tris[t + 9 + i * 12] = v + i * 2 + 1;
            tris[t + 10 + i * 12] = (i + 1) *2 + 1;
            tris[t + 11 + i * 12] = v + (i+1)*2 + 1;     
        }

        tris[newTris.Length - 12] = verts.Length - 1;
        tris[newTris.Length - 11] = verts.Length/2 - 1;
        tris[newTris.Length - 10] = verts.Length - 2;
        tris[newTris.Length - 9] = verts.Length - 2;
        tris[newTris.Length - 8] = verts.Length / 2 - 1;
        tris[newTris.Length - 7] = verts.Length / 2 - 2;

        tris[newTris.Length - 6] = verts.Length/2;
        tris[newTris.Length - 5] = 0;
        tris[newTris.Length - 4] = verts.Length/2 + 1;
        tris[newTris.Length - 3] = verts.Length / 2 + 1;
        tris[newTris.Length - 2] = 0;
        tris[newTris.Length - 1] = 1;

        mesh.vertices = verts;
        mesh.triangles = tris;

    }

    void EliminateClipping() {

        GetPoints();
        int childOffset = 0;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 startPoint = CastDown(points[i].position) + Vector3.up*(groundOffset);
            Vector3 endPoint = CastDown(points[i + 1].position) + Vector3.up*(groundOffset);
            if (Physics.Raycast(startPoint, endPoint - startPoint, out RaycastHit hitFwd, (endPoint - startPoint).magnitude, groundLayer)) {
                if (Physics.Raycast(endPoint, startPoint- endPoint, out RaycastHit hitBack, (endPoint - startPoint).magnitude, groundLayer))
                {
                    //Debug.DrawLine(hitFwd.point, hitBack.point, Color.green, 20);
                    Vector3 maxHeight = Vector3.down * 1000;
                    Vector3 dir = hitBack.point - hitFwd.point;
                    dir.Normalize();
                    for (float j = 0; j < Vector3.Distance(hitFwd.point,hitBack.point); j+= 0.2f)
                    {
                        //Debug.DrawRay(hitFwd.point + dir * j,Vector3.down,Color.yellow,20);
                        Vector3 hit = CastDown(hitFwd.point + dir * j);
                        if (hit.y >= maxHeight.y) {
                            maxHeight = hit;
                        }
                    }
                    if (maxHeight != Vector3.down * 1000) {
                        Debug.DrawLine(maxHeight + Vector3.up * 6, maxHeight - Vector3.up * 6,Color.red,20);
                        Transform newPoint = new GameObject("HeightSmoothingPoint").transform;
                        newPoint.gameObject.tag = "EditorOnly";
                        newPoint.parent = transform;
                        newPoint.SetSiblingIndex(i + childOffset + 1);
                        childOffset++;
                        newPoint.position = maxHeight;
                    }
                }
            }

        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform go = GameObject.Find("Road_Generated")?.transform;
        Mesh mesh = go?.GetComponent<MeshFilter>().sharedMesh;

        if (debugMode)
        {
            Handles.color = Color.red;
            Gizmos.color = Color.red;
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                Handles.Label(go.TransformPoint(mesh.vertices[i]), i.ToString());
            }
        }
        for (int i = 0; i < transform.childCount - 1; i++)
        {
            Debug.DrawLine(transform.GetChild(i).position, transform.GetChild(i + 1).position);
        }
      
    }
#endif
}
