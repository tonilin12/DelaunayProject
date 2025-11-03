using ClassLibrary2.MeshFolder.Else;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace TestProject1.TestFolder.DataStructureTestFolder
{
    [TestClass]
    public class VertexEnumerateTest
    {
        private readonly TriangleSplitter splitter = new TriangleSplitter();

        // ---------------- Helper: initialization ----------------
        /// <summary>
        /// Builds a single triangle, inserts vD via SplitTriangle, and returns
        /// vD, the pre-split original edges, all current edges from the
        /// post-split faces adjacent to those originals, and vD's incident edges.
        /// </summary>
        private (Vertex vD,
                 List<HalfEdge> originalEdges,
                 List<HalfEdge> allEdgesFromFaces,
                 List<HalfEdge> vertexEdges)
        InitTriangleWithInsertedVertex()
        {
            // Base triangle
            var vA = new Vertex(0, 0);
            var vB = new Vertex(1, 0);
            var vC = new Vertex(0, 1);
            var face = new Face(vA, vB, vC);

            // Inserted vertex
            var vD = new Vertex(0.3f, 0.3f);

            // Preserve pre-split order of the original face edges
            var originalEdges = new List<HalfEdge>(face.GetEdges());

            // Split
            splitter.SplitTriangle(face, vD);

            // Collect all edges from all faces related to the original edges (post-split)
            var allEdgesFromFaces = originalEdges
                .SelectMany(e => e.Face.GetEdges())
                .Distinct()
                .ToList();

            // vD incident edges from the vertex API
            var vertexEdges = vD.GetEdges().ToList();

            return (vD, originalEdges, allEdgesFromFaces, vertexEdges);
        }

        // ---------------- Test ----------------
        [TestMethod]
        public void TestVertexEnumerable()
        {
            var (vD, originalEdges, allEdgesFromFaces, vertexEdges) = InitTriangleWithInsertedVertex();

            // Filter edges whose Origin is the inserted vertex vD (from the mesh)
            var edgesFromVD = allEdgesFromFaces
                .Where(e => e.Origin == vD)
                .ToList();

            // 1) Same count
            Assert.AreEqual(
                edgesFromVD.Count, vertexEdges.Count,
                $"vD.GetVertexEdges() count mismatch: expected {edgesFromVD.Count}, got {vertexEdges.Count}"
            );

            // 2) Same references (order independent)
            CollectionAssert.AreEquivalent(
                edgesFromVD, vertexEdges,
                $"vD.GetVertexEdges() returned a different edge set.\n" +
                $"Expected: {string.Join(", ", edgesFromVD)}\n" +
                $"Actual:   {string.Join(", ", vertexEdges)}"
            );

            // 3) Polygon orientation around vD should be CW
            // Build the ring from the order returned by GetVertexEdges()
            var ring = vertexEdges.Select(e => e.Dest).ToList();

            Assert.IsTrue(ring.Count >= 3, "vD incident ring must have at least 3 vertices.");
            Assert.AreEqual(ring.Count, ring.Distinct().Count(),
                "Destinations around vD contain duplicates; expected a simple ring.");

            // Signed area (shoelace). Negative -> CW in standard Cartesian (y-up)
            double twiceArea = 0.0;
            for (int i = 0; i < ring.Count; i++)
            {
                var p = ring[i].Position;
                var q = ring[(i + 1) % ring.Count].Position;
                twiceArea += (double)p.X * q.Y - (double)q.X * p.Y;
            }

            Assert.IsTrue(
                twiceArea < 0,
                $"Expected CW orientation around vD, but signed area was {twiceArea / 2.0}. " +
                "If your geometry uses screen coordinates (y-down), flip the sign check."
            );
        }
    }
}
