﻿using UnityEngine;
/*
 * This script generates a mesh of billboards (quads or triangles) in an array with a 
 * rectangular (box) or circular/spherical (ball) layout.
 * 
 * It is used to create a mesh of quads or triangles that can be used to procedurally 
 * render a large number of billboards in a single draw call.
 * 
 * Each billboard is a quad or triangle that can be used to render a particle texture or 
 * sprite.
 * 
 * In the fragment shader, each vertex is linked to a particular particle in the 
 * simulation by dividing the vertex index by 4 (for quads) or 3 (for Triangles). 
 * 
 * The vertex position is then updated to reflect it's position and orientation relative 
 * to the centre postion of the particular particle in object space.
 * 
 * The concept and shader code was derived from examples provided by VRChat world creator 
 * SBoys3 who developed the technique to render flocks of particles representng flying 
 * debris in Tornado simulations that run on the mobile platforms.
 * 
 * The advantage of this method is that a flock of particles can be procedurally rendered 
 * and animated in a single draw call, allowng 20,000 particles to be simulated in a mobile 
 * VR device such as the quest.
 * 
 * This script (originally calle QuadMesh) is currently used in the WaveParticleDemo project 
 * to generate a rectangular mesh in the quantum particles simulation.
 * 
 * It is also used in a separate Crystal scattering project to generate a ball of particle 
 * billboards used to procedurally render a crystal and reciprocal structures in space. 
 * 
 * Note: The ball option was added to minimise the total number of redundant particles and 
 * simplify the math in the shader code when developing crystal lattice applications that 
 * operate within a spherical volume.
 * 
 */
namespace WaveParticleSim
{
    [RequireComponent(typeof(MeshFilter))]
    public class BillboardMesh : MonoBehaviour
    {
        [Tooltip("Width/Height/Depth in model space")] public Vector3 meshDimensions = Vector3.one;
        [SerializeField, Tooltip("Uncheck for circular/sphere point distribution")] public bool isRectangular = false;
        [SerializeField, Tooltip("Center array around mesh origin")] public bool centerArrayOrigin = true;
        [Tooltip("# quads across")] public Vector3Int pointsAcross = new Vector3Int(16, 16, 16);
        [Tooltip("Snap points to origin (adds 1 point when origin at model center & even #points across)")]
        public bool snapOrigin = true;
        [SerializeField]
        public Material material;

        Mesh mesh;
        MeshFilter mf;

        float radiusSq;
        [SerializeField]
        Vector3 arraySpacing;
        [SerializeField]
        float arrayRadius;
        [SerializeField]
        Vector3 arrayOrigin;
        [SerializeField]
        bool useTriangles = false;

        // Only Uncomment serialization for verification in editor 
        //[SerializeField]
        private Vector3[] vertices;
        //[SerializeField]
        private Vector2[] uvs;
        //[SerializeField]
        int[] triangles;
        //[SerializeField]
        int numDecals;
        //[SerializeField]
        int numVertices;
        //[SerializeField]
        int numTriangles;
        private bool generateMesh()
        {
            if (mf == null)
                return false;

            Vector3 arrayRadius = centerArrayOrigin ? meshDimensions * 0.5f : meshDimensions;

            Vector3Int numGridPoints = pointsAcross;
            if (numGridPoints.x < 1) numGridPoints.x = 1;
            if (numGridPoints.y < 1) numGridPoints.y = 1;
            if (numGridPoints.z < 1) numGridPoints.z = 1;
            if (centerArrayOrigin && snapOrigin)
            {
                numGridPoints.x = numGridPoints.x % 2 == 1 ? numGridPoints.x : numGridPoints.x + 1;
                numGridPoints.y = numGridPoints.y % 2 == 1 ? numGridPoints.y : numGridPoints.y + 1;
                numGridPoints.z = numGridPoints.z % 2 == 1 ? numGridPoints.z : numGridPoints.z + 1;
            }

            arraySpacing = new Vector3((numGridPoints.x <= 1) ? meshDimensions.x : (meshDimensions.x / (numGridPoints.x - 1)),
                                    (numGridPoints.y <= 1) ? meshDimensions.y : (meshDimensions.y / (numGridPoints.y - 1)),
                                    (numGridPoints.z <= 1) ? meshDimensions.z : (meshDimensions.z / (numGridPoints.z - 1)));

            radiusSq = arrayRadius.x + (arraySpacing.x * 0.1f);
            radiusSq *= radiusSq;

            numDecals = numGridPoints.x * numGridPoints.y * numGridPoints.z;
            numVertices = numDecals * (useTriangles ? 3 : 4);
            vertices = new Vector3[numVertices];
            uvs = new Vector2[numVertices];
            triangles = new int[numDecals * (useTriangles ? 3 : 6)];

            arrayOrigin = Vector3.zero;
            if (centerArrayOrigin)
            {
                arrayOrigin = Vector3.Scale(-arrayRadius, new Vector3(numGridPoints.x <= 1 ? 0 : 1, numGridPoints.y <= 1 ? 0 : 1, numGridPoints.z <= 1 ? 0 : 1));
            }
            Vector3 quadPos = arrayOrigin;

            numVertices = 0;
            numTriangles = 0;
            float quadDim = Mathf.Min(arraySpacing.x, arraySpacing.y);

            Vector3 vxOffset0 = (Vector3.down + Vector3.right) * 0.5f;
            Vector3 vxOffset1 = (Vector3.down + Vector3.left) * 0.5f;
            Vector3 vxOffset2 = (Vector3.up + Vector3.right) * 0.5f;
            Vector3 vxOffset3 = (Vector3.up + Vector3.left) * 0.5f; // Only for quad
            if (useTriangles)
            {
                vxOffset0 = new Vector2(0.5f, -0.288675135f);
                vxOffset1 = new Vector2(-0.5f, -0.288675135f);
                vxOffset2 = new Vector2(0, 0.57735027f);
            }

            vxOffset0 *= quadDim;
            vxOffset1 *= quadDim;
            vxOffset2 *= quadDim;
            vxOffset3 *= quadDim;

            Vector2 uv0 = useTriangles ? new Vector2(-0.366025403f, 0) : Vector2.zero;
            Vector2 uv1 = useTriangles ? new Vector2(1.366025403f, 0) : Vector2.right;
            Vector2 uv2 = useTriangles ? new Vector2(0.5f, 1.5f) : Vector2.up;
            Vector2 uv3 = Vector2.one;

            bool positionInRange;
            for (int nPlane = 0; nPlane < numGridPoints.z; nPlane++)
            {
                quadPos.y = arrayOrigin.y;
                for (int nRow = 0; nRow < numGridPoints.y; nRow++)
                {
                    quadPos.x = arrayOrigin.x;
                    for (int nCol = 0; nCol < numGridPoints.x; nCol++)
                    {
                        if (isRectangular)
                        {
                            positionInRange = true;
                        }
                        else
                        {
                            float rsq = Vector3.Dot(quadPos, quadPos);
                            positionInRange = rsq <= radiusSq;
                        }

                        if (positionInRange)
                        {

                            triangles[numTriangles++] = numVertices;
                            triangles[numTriangles++] = numVertices + 2;
                            triangles[numTriangles++] = numVertices + 1;
                            if (!useTriangles)
                            {
                                triangles[numTriangles++] = numVertices + 2;
                                triangles[numTriangles++] = numVertices + 3;
                                triangles[numTriangles++] = numVertices + 1;
                            }
                            uvs[numVertices] = uv0;
                            vertices[numVertices++] = quadPos + vxOffset0;

                            uvs[numVertices] = uv1;
                            vertices[numVertices++] = quadPos + vxOffset1;

                            uvs[numVertices] = uv2;
                            vertices[numVertices++] = quadPos + vxOffset2;

                            if (!useTriangles)
                            {
                                uvs[numVertices] = uv3;
                                vertices[numVertices++] = quadPos + vxOffset3;
                            }
                        }
                        quadPos.x += arraySpacing.x;
                    }
                    quadPos.y += arraySpacing.y;
                }
                quadPos.z += arraySpacing.z;
            }
            mesh = mf.mesh;
            mesh.Clear();
            numDecals = numVertices / 4;
            int[] meshTris = new int[numTriangles];
            for (int i = 0; i < numTriangles; i++)
                meshTris[i] = triangles[i];
            if (meshTris.Length >= 32767)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            Vector3[] meshVerts = new Vector3[numVertices];
            Vector2[] meshUVs = new Vector2[numVertices];
            for (int i = 0; i < numVertices; i++)
            {
                meshUVs[i] = uvs[i];
                meshVerts[i] = vertices[i];
            }
            mesh.vertices = meshVerts;
            mesh.triangles = meshTris;
            mesh.uv = meshUVs;
            triangles = null;
            vertices = null;
            uvs = null;
            if (material != null)
            {
                material.SetVector("_ArraySpacing", arraySpacing);
                if (material.HasProperty("_ArrayDimension"))
                {
                    Vector4 pointVec = new Vector4(numGridPoints.x, numGridPoints.y, numGridPoints.z, numGridPoints.x * numGridPoints.y * numGridPoints.z);
                    material.SetVector("_ArrayDimension", pointVec);
                }
            }
            return true;
        }
        void Start()
        {
            mf = GetComponent<MeshFilter>();
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null)
                material = mr.material;
            generateMesh();
        }
    }
}
