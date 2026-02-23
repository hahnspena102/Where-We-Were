using System.Collections.Generic;
using System.Linq;
using mattatz.Triangulation2DSystem;
using NaughtyAttributes;
using UnityEngine;
using UnityEditor;
 
public class MeshStencilCreator : MonoBehaviour {
    public Sprite[] targets;
    // public MeshFilter meshFilter;
    public Transform outputContainer;
    public int imageSize = 1024;
    public int offsetX = 512;
    public int offsetY = 0;
    public float threshold = 0.5f;
    public float depth = 0.05f;
    public string meshPrefix = "NPCStencil_";
    public string meshPath = "Assets/Models/NPC/";
 
    public Material faceMaterialToCopy;
    public Material sideMaterial;
    public float edgeUVWidth = 208f / 2048f;
    public string materialPrefix = "CardboardFace_";
    public string materialPath = "Assets/Materials/Cardboard/NPC/";
 
    [Button]
    public void CreateMesh() {
        float posIncrement = 1f;
        float sqrt = Mathf.Sqrt(targets.Length);
        int xLen = Mathf.CeilToInt(sqrt);
        int yLen = Mathf.FloorToInt(sqrt);
        int xCount = 0;
        int yCount = 0;
        foreach (Sprite target in targets) {
            Debug.Log("Creating Mesh for " + target.name);
            //get edge verts
            Color[] pxs = target.texture.GetPixels(0, 0, imageSize, imageSize, 0);
            SimpleSurfaceEdge sse = new SimpleSurfaceEdge(pxs, imageSize, imageSize, threshold);
            List<Vector2> verts = sse.GetOutsideEdgeVertices().ToList();
            Vector2 offset = new Vector2(offsetX, offsetY);
            // Use a smaller constrain threshold to preserve more edge detail
            float constrainThreshold = 0.1f;
            verts = Utils2D.Constrain(verts.ToList(), constrainThreshold); //reduce to a reasonable size
            
            // Normalize vertices
            List<Vector2> normalizedVerts = new List<Vector2>();
            foreach (var v in verts) {
                normalizedVerts.Add((v - offset) / (float)imageSize);
            }
            verts = normalizedVerts;
            Debug.Log($"Mesh '{target.name}': {verts.Count} vertices after constraint, threshold={threshold}, offset=({offsetX}, {offsetY})");
 
            //triangulate the mesh
            Polygon2D polygon = Polygon2D.Contour(verts.ToArray());
            Triangulation2D tri = new Triangulation2D(polygon, 16, 0.1f);
            Mesh m = tri.Build();
            m.name = meshPrefix;
            Vector3[] meshVerts = m.vertices;
            
            // Create back verts with depth
            Vector3[] backVerts = new Vector3[meshVerts.Length];
            for (int i = 0; i < meshVerts.Length; i++) {
                backVerts[i] = new Vector3(meshVerts[i].x, meshVerts[i].y, depth);
            }
            
            // Combine front and back verts
            List<Vector3> allVerts = new List<Vector3>(meshVerts);
            allVerts.AddRange(backVerts);
            m.SetVertices(allVerts);
 
            //backface tris
            int vertsCount = meshVerts.Length;
            int[] originalTris = m.triangles;
            int[] newTris = new int[originalTris.Length];
            
            // Reverse triangles for back face
            for (int i = 0; i < originalTris.Length; i++) {
                newTris[originalTris.Length - 1 - i] = originalTris[i] + vertsCount;
            }
            
            // Combine triangles
            List<int> allTris = new List<int>(originalTris);
            allTris.AddRange(newTris);
            m.SetTriangles(allTris.ToArray(), 0);
 
            //create edge tris
            List<Vector3> sideVerts = new List<Vector3>();
            List<int> sideTris = new List<int>();
            vertsCount *= 2;
            for (int i = 0; i < verts.Count; i++) {
                sideVerts.Add(new Vector3(verts[i].x, verts[i].y, 0f));
                sideVerts.Add(new Vector3(verts[i].x, verts[i].y, depth));
                int i2 = i * 2;
                if (i < verts.Count - 1) {
                    sideTris.Add(i2 + 1);
                    sideTris.Add(i2 + 2);
                    sideTris.Add(i2);
                    sideTris.Add(i2 + 2);
                    sideTris.Add(i2 + 1);
                    sideTris.Add(i2 + 3);
                } else {
                    sideTris.Add(i2 + 1);
                    sideTris.Add(0);
                    sideTris.Add(i2);
                    sideTris.Add(0);
                    sideTris.Add(i2 + 1);
                    sideTris.Add(1);
                }
            }
            
            // Map side tris to correct vertex indices
            List<int> mappedSideTris = new List<int>();
            foreach (int st in sideTris) {
                mappedSideTris.Add(st + vertsCount);
            }
            
            m.subMeshCount = 2;
            List<Vector3> combinedVerts = new List<Vector3>(m.vertices);
            combinedVerts.AddRange(sideVerts);
            m.SetVertices(combinedVerts);
            m.SetTriangles(mappedSideTris.ToArray(), 1);
 
            m.RecalculateNormals();
            m.RecalculateBounds();
 
            //calc UVs
            Vector2[] frontUVs = new Vector2[meshVerts.Length];
            Vector2[] backUVs = new Vector2[meshVerts.Length];
            Vector2 uvOffset = new Vector2(offsetX / (float)imageSize, offsetY / (float)imageSize);
            for (int i = 0; i < meshVerts.Length; i++) {
                frontUVs[i] = (Vector2)meshVerts[i] + uvOffset;
                backUVs[i] = frontUVs[i];
            }
 
            float[] distances = new float[sideVerts.Count];
            float totalDist = 0f;
            for (int i = 0; i < sideVerts.Count; i += 2) {
                float dist;
                if (i < sideVerts.Count - 2) dist = Vector3.Distance(sideVerts[i], sideVerts[i + 2]);
                else dist = Vector3.Distance(sideVerts[i], sideVerts[0]);
                distances[i] = distances[i + 1] = dist;
                totalDist += dist;
            }
 
            Vector2[] sideUVs = new Vector2[sideVerts.Count];
            float uvYPos = 0f;
            for (int i = 0; i < sideVerts.Count; i += 2) {
                sideUVs[i] = new Vector2(0f, uvYPos / totalDist);
                sideUVs[i + 1] = new Vector2(edgeUVWidth, uvYPos / totalDist);
                uvYPos += distances[i];
            }
 
            List<Vector2> allUVs = new List<Vector2>(frontUVs);
            allUVs.AddRange(backUVs);
            allUVs.AddRange(sideUVs);
            m.SetUVs(0, allUVs);
 
            //Create a GameObject
            string baseName = target.name.Substring(0, target.name.IndexOf("_"));
            string meshName = meshPrefix + baseName;
            Debug.Log("baseName: " + baseName + " | meshName: " + meshName);
            GameObject child = new GameObject(meshName);
            child.transform.parent = outputContainer.transform;
            child.transform.localPosition = new Vector3(xCount * posIncrement, 0, yCount * posIncrement);
            if (++xCount >= xLen) {
                xCount = 0;
                yCount++;
            }
 
            MeshFilter mf = child.AddComponent<MeshFilter>();
            MeshRenderer mr = child.AddComponent<MeshRenderer>();
            mf.sharedMesh = m;
 
            string[] ass = AssetDatabase.FindAssets(baseName + "_spec" + " t:texture");
            if (ass.Length != 1) {
                Debug.LogError("Spec map asset error: " + baseName);
                continue;
            }
            string guid = AssetDatabase.GUIDToAssetPath(ass.First());
            Texture2D specMap = AssetDatabase.LoadAssetAtPath<Texture2D>(guid);
            if (specMap == null) {
                Debug.LogError("Spec map asset error #2: " + baseName);
                continue;
            }
 
            Material newFrontMat = new Material(faceMaterialToCopy);
            newFrontMat.name = materialPrefix + baseName;
            newFrontMat.SetTexture("_BaseMap", target.texture);
            newFrontMat.SetTexture("_SpecGlossMap", specMap);
            newFrontMat.SetTexture("_OcclusionMap", specMap);
            mr.materials = new Material[] {
                newFrontMat,
                sideMaterial
            };
 
            AssetDatabase.CreateAsset(m, meshPath + meshName + ".asset");
            AssetDatabase.CreateAsset(newFrontMat, materialPath + newFrontMat.name + ".mat");
            AssetDatabase.SaveAssets();
 
            Debug.Log("Finished creating Mesh for " + target.name);
        }
 
        Debug.Log("All meshes created");
    }
}
