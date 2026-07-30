using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClassLibrary2;
using ClassLibrary2.MeshFolder.Else;
using ClassLibrary2.MeshFolder.DataStructures;

namespace TestProject1.TestFolder.Else
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
        [Description("Verifies Voronoi cells are one-to-one with internal vertices and match incident face circumcenters using tuples without custom comparer.")]
        public void TestVoronoiCellsMappingAndPolygons_Tuples_NoComparer()
        {
            Assert.IsNotNull(_vertices, "Setup did not initialize vertices.");
            Assert.IsTrue(_vertices.Length > 0, "No vertices to process.");

            // Arrange: build triangulation
            var superTriangle = TriangulationOperation.GetSuperTriangle(_vertices);
            var triangulator = new DelaunayBuilder(superTriangle);

            triangulator.AddVertices(_vertices);
            triangulator.ProcessAllVertices();

            var internalVertices = triangulator.GetInternalVertices().ToList();
            var voronoiCells =triangulator.GetVoronoi();


            // Pair each vertex with its Voronoi cell using tuples
            var vertexCellPairs = new List<(Vertex vertex, VoronoiCell cell)>();
            var usedSites = new List<Vector2>();

            foreach (var vertex in internalVertices)
            {
                var cell = voronoiCells.FirstOrDefault(c => Vector2.Distance(c.Site, vertex.Position) <GeometryUtils.EPSILON);
                Assert.IsNotNull(cell, $"No Voronoi cell found for vertex at position {vertex.Position}.");

                // Ensure one-to-one mapping by checking previous sites
                Assert.IsFalse(usedSites.Any(s => Vector2.Distance(s, cell.Site) < GeometryUtils.EPSILON),
                    $"Duplicate Voronoi cell found for site {cell.Site}.");

                usedSites.Add(cell.Site);
                vertexCellPairs.Add((vertex, cell));
            }

            // Verify polygons match circumcenters
            foreach (var (vertex, cell) in vertexCellPairs)
            {
                var circumcenters = vertex.GetEdges()
                                          .Select(e => e.Face?.Circumcenter)
                                          .Where(c => c != null)
                                          .Select(c => c.Value)
                                          .ToList();

                var sortedCellPolygon = cell.CellVertices.ToList();
                var sortedCircumcenters = circumcenters.ToList();

                Assert.AreEqual(sortedCircumcenters.Count, sortedCellPolygon.Count,
                    $"Vertex at {vertex.Position} has mismatched number of circumcenters and Voronoi polygon vertices.");

                for (int i = 0; i < sortedCircumcenters.Count; i++)
                {
                    Assert.IsTrue(Vector2.Distance(sortedCircumcenters[i], sortedCellPolygon[i]) <GeometryUtils.EPSILON,
                        $"Vertex at {vertex.Position}: Voronoi polygon vertex {sortedCellPolygon[i]} does not match circumcenter {sortedCircumcenters[i]}.");
                }
            }
        }


    }
}
