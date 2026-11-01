using System.Collections.Generic;
using SnowballBattleRoyale.World;
using UnityEngine;

namespace SnowballBattleRoyale.EditorTools
{
    /// <summary>
    /// Low-poly placeholder meshes for trees and rocks, so the world is readable before real art
    /// exists. Every mesh is flat-shaded (no shared vertices) for a consistent faceted look.
    /// </summary>
    internal static class ProceduralMeshes
    {
        /// <summary>A part of a combined mesh: geometry, placement, and which submesh it belongs to.</summary>
        internal readonly struct Part
        {
            public Part(Mesh mesh, Matrix4x4 transform, int submesh)
            {
                Mesh = mesh;
                Transform = transform;
                Submesh = submesh;
            }

            public Mesh Mesh { get; }

            public Matrix4x4 Transform { get; }

            public int Submesh { get; }
        }

        /// <summary>Truncated cone along +Y from y=0 to y=height. A top radius of 0 makes a cone.</summary>
        public static Mesh Frustum(float bottomRadius, float topRadius, float height, int segments)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            for (int i = 0; i < segments; i++)
            {
                float a0 = i * Mathf.PI * 2f / segments;
                float a1 = (i + 1) * Mathf.PI * 2f / segments;
                var d0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0));
                var d1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));

                Vector3 b0 = d0 * bottomRadius;
                Vector3 b1 = d1 * bottomRadius;
                Vector3 t0 = (d0 * topRadius) + (Vector3.up * height);
                Vector3 t1 = (d1 * topRadius) + (Vector3.up * height);

                AddTriangle(vertices, triangles, b0, t1, b1);
                if (topRadius > 0f)
                {
                    AddTriangle(vertices, triangles, b0, t0, t1);
                    AddTriangle(vertices, triangles, Vector3.up * height, t1, t0);
                }

                AddTriangle(vertices, triangles, Vector3.zero, b0, b1);
            }

            return Build("Frustum", vertices, triangles);
        }

        /// <summary>Subdivided icosahedron, optionally lumpy, as a flat-shaded rock.</summary>
        public static Mesh Rock(float radius, int subdivisions, int seed, float lumpiness, float flattenBottom)
        {
            (List<Vector3> positions, List<int> indices) = Icosphere(subdivisions);
            var noise = new SeededNoise(seed);

            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 n = positions[i];
                float lump = 1f + (noise.Fbm((n.x * 1.7f) + 10f, (n.z * 1.7f) + (n.y * 2.3f), 3) * lumpiness);
                Vector3 p = n * (radius * lump);

                // Rocks sit on the ground; squash the underside instead of leaving a ball.
                if (p.y < -radius * flattenBottom)
                {
                    p.y = Mathf.Lerp(p.y, -radius * flattenBottom, 0.8f);
                }

                positions[i] = p;
            }

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (int i = 0; i < indices.Count; i += 3)
            {
                AddOutwardTriangle(vertices, triangles, positions[indices[i]], positions[indices[i + 1]], positions[indices[i + 2]]);
            }

            return Build("Rock", vertices, triangles);
        }

        /// <summary>Squashed flat-shaded sphere, used for birch canopies.</summary>
        public static Mesh Blob(float radius, Vector3 squash)
        {
            (List<Vector3> positions, List<int> indices) = Icosphere(1);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            for (int i = 0; i < indices.Count; i += 3)
            {
                AddOutwardTriangle(
                    vertices,
                    triangles,
                    Vector3.Scale(positions[indices[i]] * radius, squash),
                    Vector3.Scale(positions[indices[i + 1]] * radius, squash),
                    Vector3.Scale(positions[indices[i + 2]] * radius, squash));
            }

            return Build("Blob", vertices, triangles);
        }

        /// <summary>Merges parts into one mesh with one submesh per distinct <see cref="Part.Submesh"/>.</summary>
        public static Mesh Combine(string name, IReadOnlyList<Part> parts, int submeshCount)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var submeshes = new List<int>[submeshCount];
            for (int s = 0; s < submeshCount; s++)
            {
                submeshes[s] = new List<int>();
            }

            foreach (Part part in parts)
            {
                int offset = vertices.Count;
                Matrix4x4 normalMatrix = part.Transform.inverse.transpose;

                foreach (Vector3 v in part.Mesh.vertices)
                {
                    vertices.Add(part.Transform.MultiplyPoint3x4(v));
                }

                foreach (Vector3 n in part.Mesh.normals)
                {
                    normals.Add(normalMatrix.MultiplyVector(n).normalized);
                }

                foreach (int index in part.Mesh.triangles)
                {
                    submeshes[part.Submesh].Add(offset + index);
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.subMeshCount = submeshCount;
            for (int s = 0; s < submeshCount; s++)
            {
                mesh.SetTriangles(submeshes[s], s);
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        /// <summary>
        /// Adds a triangle of a closed shape centred on the origin, flipping it if needed so it faces
        /// outward. Unity treats clockwise triangles as front faces, and Cross(b - a, c - a) of a
        /// front face points toward the viewer.
        /// </summary>
        private static void AddOutwardTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 centroid = (a + b + c) / 3f;
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), centroid) < 0f)
            {
                (b, c) = (c, b);
            }

            AddTriangle(vertices, triangles, a, b, c);
        }

        private static Mesh Build(string name, List<Vector3> vertices, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static (List<Vector3> Positions, List<int> Indices) Icosphere(int subdivisions)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var positions = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
            };

            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] = positions[i].normalized;
            }

            var indices = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11,
                1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9,
                4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };

            var midpoints = new Dictionary<long, int>();
            for (int s = 0; s < subdivisions; s++)
            {
                var next = new List<int>();
                for (int i = 0; i < indices.Count; i += 3)
                {
                    int a = indices[i];
                    int b = indices[i + 1];
                    int c = indices[i + 2];
                    int ab = Midpoint(positions, midpoints, a, b);
                    int bc = Midpoint(positions, midpoints, b, c);
                    int ca = Midpoint(positions, midpoints, c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }

                indices = next;
            }

            return (positions, indices);
        }

        private static int Midpoint(List<Vector3> positions, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int index))
            {
                return index;
            }

            positions.Add(((positions[a] + positions[b]) * 0.5f).normalized);
            index = positions.Count - 1;
            cache[key] = index;
            return index;
        }
    }
}
