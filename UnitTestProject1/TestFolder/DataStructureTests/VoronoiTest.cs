using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WindowsFormsApp1;
using WindowsFormsApp1.myitem.GeometryFolder; // Replace with your actual namespace

namespace UnitTestProject1.TestFolder.DataStructureTests
{
    [TestClass]
    public class VoronoiTest
    {
        private Vertex[] _vertices;

        [TestInitialize]
        public void Setup()
        {
            // Shared middle edge
            var vA = new Vertex(new Vector2(0.25f, 0f));  // left point of middle edge
            var vB = new Vertex(new Vector2(0.75f, 0f));  // right point of middle edge

            // One vertex above and one below, forming a convex quad
            var vC = new Vertex(new Vector2(0.5f, 0.5f));   // top vertex
            var vD = new Vertex(new Vector2(0.5f, -0.5f));  // bottom vertex

            // Store vertices in an array
            _vertices = new Vertex[] { vA, vB, vC, vD };
        }

        [TestMethod]
        public void TestTriangulationSetup()
        {
            // Get the super triangle
            Face superTriangle;
            TriangulationOperation.getSuperTriangle(ref _vertices, out superTriangle);

            // Build triangulator starting from the super triangle
            var triangulator = new TriangulationBuilder(superTriangle);

            Assert.IsNotNull(triangulator, "Triangulator should be created successfully.");

            foreach (var v in _vertices)
            {
                triangulator.AddVertices(v);
                triangulator.ProcessSingleVertex();

                var faces = triangulator.GetInternalTriangles().ToList();
                foreach (var face in faces)
                {
                    foreach (var tri_v in face.GetVertices())
                    {
                        var actualVoronoi = tri_v.GetVoronoiCell()
                        .Take(tri_v.GetVoronoiCell().Count - 1)
                        .ToList();

                        var expectedVoronoi = tri_v.EnumerateEdges(e => e.Face.Circumcenter).ToList();

                        // First, check that the polygon counts match
                        Assert.AreEqual(expectedVoronoi.Count, actualVoronoi.Count,
                            $"Voronoi polygon count mismatch:\n" +
                            $"- Outer vertex (v): {v}\n" +
                            $"- Face: {face}\n" +
                            $"- Inner vertex (tri_v): {tri_v}\n" +
                            $"- Actual count: {actualVoronoi.Count}, Expected count: {expectedVoronoi.Count}");

                        // Compare element by element
                        for (int i = 0; i < actualVoronoi.Count; i++)
                        {
                            var a = actualVoronoi[i];
                            var b = expectedVoronoi[i];

                            if (Vector2.DistanceSquared(a, b) > GeometryUtils.GetEpsilon)
                            {
                                Assert.Fail(
                                    $"Voronoi mismatch detected:\n" +
                                    $"- Outer vertex (v): {v}\n" +
                                    $"- Face: {face}\n" +
                                    $"- Inner vertex (tri_v): {tri_v}\n" +
                                    $"- Polygon index: {i}\n" +
                                    $"- Actual vertex: ({a.X:F6}, {a.Y:F6})\n" +
                                    $"- Expected vertex: ({b.X:F6}, {b.Y:F6})"
                                );
                            }
                        }
                    }
                }
            }
        }

    }
}

