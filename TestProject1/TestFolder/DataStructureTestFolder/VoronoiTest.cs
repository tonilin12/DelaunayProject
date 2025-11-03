using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClassLibrary2;
using ClassLibrary2.HalfEdgeFolder.VoronoiFolder;
using ClassLibrary2.MeshFolder.Else;

namespace TestProject1.TestFolder.DataStructureTestFolder
{
    [TestClass]
    public class VoronoiTest
    {
        private Vertex[] _vertices = Array.Empty<Vertex>();

        [TestInitialize]
        public void Setup()
        {
            // Define a small, well-known configuration of points forming two adjacent Delaunay triangles
            var vA = new Vertex(0.25f, 0f);   // Left
            var vB = new Vertex(0.75f, 0f);   // Right
            var vC = new Vertex(0.5f, 0.5f);  // Top
            var vD = new Vertex(0.5f, -0.5f); // Bottom

            _vertices = new[] { vA, vB, vC, vD };
        }

        [TestMethod]
        [Description("Tests full Voronoi diagram construction from a small convex set of 4 vertices.")]
        public void TestVoronoiCellConstruction()
        {
            Assert.IsNotNull(_vertices, "Setup did not initialize vertices.");
            Assert.IsTrue(_vertices.Length > 0, "No vertices to process.");

            // Arrange
            var superTriangle = TriangulationOperation.GetSuperTriangle(_vertices);
            var triangulator = new DelaunayBuilder(superTriangle);

            // Act
            foreach (var v in _vertices)
            {
                triangulator.AddVertices(v);
                triangulator.ProcessSingleVertex();
            }

            var internalVertices = triangulator.GetInternalVertices();
            var voronoiCells = VoronoiBuilder.BuildDiagram(triangulator);

            // Assert
            Assert.IsTrue(voronoiCells.Any(), "VoronoiBuilder produced no cells.");
            Assert.AreEqual(internalVertices.ToList().Count(), voronoiCells.Count,
                $"Voronoi cell count mismatch. Expected {internalVertices.ToList().Count()}, Actual {voronoiCells.Count}");

            foreach (var cell in voronoiCells)
            {
                var site = cell.Site;
                Assert.IsNotNull(site, "Voronoi cell site is null.");


                var expectedPolygon = site
                    .GetEdges()                  // returns IEnumerable<HalfEdge>
                    .Select(e => e.Face.Circumcenter) // select the circumcenter of each edge’s face
                    .ToList();

                var actualPolygon = cell.Polygon;

                Assert.AreEqual(expectedPolygon.Count, actualPolygon.Count,
                    $"Voronoi polygon vertex count mismatch for site {site.Position}: expected {expectedPolygon.Count}, got {actualPolygon.Count}.");

                for (int i = 0; i < actualPolygon.Count; i++)
                {
                    var a = actualPolygon[i];
                    var b = expectedPolygon[i];

                    if (Vector2.DistanceSquared(a, b) > GeometryUtils.EPSILON)
                    {
                        Assert.Fail(
                            $"Voronoi vertex mismatch for site {site.Position}, polygon index {i}:\n" +
                            $"Expected: ({b.X:F6}, {b.Y:F6})\n" +
                            $"Actual:   ({a.X:F6}, {a.Y:F6})\n" +
                            $"Δ = {Vector2.Distance(a, b):F8}"
                        );
                    }
                }
            }
        }
    }
}
