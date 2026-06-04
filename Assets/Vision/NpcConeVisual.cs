using Garrison.Shared.Config;
using UnityEngine;

namespace Garrison.Vision
{
    // Persistent ground fan for the C4 tell. It is presentation only: C5 reuses the
    // same config keys for perception, but this component just renders what the body is facing.
    [ExecuteAlways]
    public sealed class NpcConeVisual : MonoBehaviour, IConfigConsumer
    {
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField, Min(0f)] private float groundOffset = 0.02f;
        [SerializeField, Range(3, 64)] private int segmentCount = 24;

        [Header("Outline")]
        [SerializeField] private LineRenderer outline;
        [SerializeField, Min(0f)] private float outlineWidth = 0.08f;
        [SerializeField, Min(0f)] private float outlineLift = 0.015f;

        private const float DefaultConeArc = 70f;
        private const float DefaultConeRange = 8f;

        private IConfig config;
        private Mesh coneMesh;

        public void Configure(IConfig source)
        {
            config = source;
            RebuildMesh();
        }

        private void OnEnable()
        {
            EnsureMesh();
            RebuildMesh();
        }

        private void OnValidate()
        {
            EnsureMesh();
            RebuildMesh();
        }

        private void OnDestroy()
        {
            if (coneMesh == null)
                return;

            if (Application.isPlaying)
                Destroy(coneMesh);
            else
                DestroyImmediate(coneMesh);
        }

        private void EnsureMesh()
        {
            // Outline can live on the same GameObject as the cone renderer; resolve it lazily
            // so the prefab does not need an explicit serialized reference.
            if (outline == null)
                outline = GetComponentInChildren<LineRenderer>(true);

            if (meshFilter == null)
                return;

            if (coneMesh == null)
            {
                coneMesh = new Mesh
                {
                    name = "NpcConeMesh"
                };
                coneMesh.MarkDynamic();
            }

            if (meshFilter.sharedMesh != coneMesh)
                meshFilter.sharedMesh = coneMesh;
        }

        private void RebuildMesh()
        {
            if (meshFilter == null)
                return;

            EnsureMesh();
            if (coneMesh == null)
                return;

            float arc = Mathf.Clamp(config?.GetFloat(ConfigKey.NpcConeArc, DefaultConeArc) ?? DefaultConeArc, 1f, 179f);
            float range = Mathf.Max(0.01f, config?.GetFloat(ConfigKey.NpcConeRange, DefaultConeRange) ?? DefaultConeRange);
            int segments = Mathf.Max(3, segmentCount);

            Vector3[] vertices = new Vector3[segments + 2];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] triangles = new int[segments * 3];

            vertices[0] = new Vector3(0f, groundOffset, 0f);
            normals[0] = Vector3.up;
            uvs[0] = new Vector2(0.5f, 0f);

            float halfArc = arc * 0.5f;
            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float degrees = Mathf.Lerp(-halfArc, halfArc, t);
                float radians = degrees * Mathf.Deg2Rad;
                float x = Mathf.Sin(radians) * range;
                float z = Mathf.Cos(radians) * range;
                int vertexIndex = i + 1;

                vertices[vertexIndex] = new Vector3(x, groundOffset, z);
                normals[vertexIndex] = Vector3.up;
                uvs[vertexIndex] = new Vector2(t, 1f);

                if (i == segments)
                    continue;

                int triangleIndex = i * 3;
                triangles[triangleIndex] = 0;
                triangles[triangleIndex + 1] = vertexIndex;
                triangles[triangleIndex + 2] = vertexIndex + 1;
            }

            coneMesh.Clear();
            coneMesh.vertices = vertices;
            coneMesh.normals = normals;
            coneMesh.uv = uvs;
            coneMesh.triangles = triangles;
            coneMesh.RecalculateBounds();

            UpdateOutline(vertices);
        }

        // The fan's vertices already describe the cone boundary as a closed loop:
        // [apex, arc0 .. arcN]. Looping the line connects arcN back to the apex, so the
        // outline traces both straight edges and the curved front in one pass.
        private void UpdateOutline(Vector3[] vertices)
        {
            if (outline == null)
                return;

            outline.useWorldSpace = false;
            outline.loop = true;
            outline.widthMultiplier = outlineWidth;
            outline.numCornerVertices = 0;
            outline.numCapVertices = 0;
            outline.alignment = LineAlignment.View;
            outline.textureMode = LineTextureMode.Stretch;

            outline.positionCount = vertices.Length;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 p = vertices[i];
                p.y += outlineLift;
                outline.SetPosition(i, p);
            }
        }
    }
}
