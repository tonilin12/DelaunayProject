using ClassLibrary2.MeshFolder.Else;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Numerics;

namespace TestProject1.TestFolder.TriangulationOperations
{
    [TestClass]
    public class TriangleSplitterTest
    {
        private readonly TriangleSplitter splitter = new TriangleSplitter();

        [TestMethod]
        public void SplitTriangle_PointInsideFace()
        {
            // Arrange
            var vA = new Vertex(0, 0);
            var vB = new Vertex(1, 0);
            var vC = new Vertex(0, 1);
            var face = new Face(vA, vB, vC);

            var vD = new Vertex(0.3f, 0.3f);
            var originalEdges = new List<HalfEdge>(face.GetEdges()); // pre-split order

            // Act
            splitter.SplitTriangle(face, vD);

            // Assert (shared)
            AssertSplitFan(originalEdges, vD, "PointInsideFace");
        }

        [TestMethod]
        public void SplitTriangle_PointOnEdge()
        {
            // Arrange
            var vA = new Vertex(0.25f, 0f);
            var vB = new Vertex(0.75f, 0f);
            var vC = new Vertex(0.5f, 0.5f);
            var vD = new Vertex(0.5f, -0.5f);

            var face1 = new Face(vA, vB, vC);
            var face2 = new Face(vB, vA, vD);

            // Link twins
            face1.Edge.Twin = face2.Edge;
            face2.Edge.Twin = face1.Edge;

            // Midpoint on AB
            var edge = face1.Edge;
            var vE = new Vertex(
                (vA.Position.X + vB.Position.X) / 2f,
                (vA.Position.Y + vB.Position.Y) / 2f
            );

            // Pre-split ring of the four edges around the two adjacent faces (order matters)
            var originalEdges = new List<HalfEdge>
            {
                edge.Next!, edge.Next!.Next!,
                edge.Twin!.Next!, edge.Twin!.Next!.Next!
            };

            // Act
            splitter.SplitTriangle_VertexOnEdge(edge, vE);

            // Assert (shared)
            AssertSplitFan(originalEdges, vE, "PointOnEdge");
        }

        /// <summary>
        /// Shared assertions for both split scenarios.
        /// Validates the post-split "fan" wiring from each original edge:
        ///   e1 = e.Next, e1.Dest == inserted
        ///   e2 = e1.Next, e2.Dest == e.Origin
        ///   e1.Twin.Next == originalEdges[i+1] (with wrap)
        /// </summary>
        private static void AssertSplitFan(IReadOnlyList<HalfEdge> originalEdges, Vertex inserted, string tag)
        {
            int n = originalEdges.Count;
            for (int i = 0; i < n; i++)
            {
                var e = originalEdges[i];

                var e1 = e.Next;
                Assert.IsNotNull(e1, $"[{tag} | Edge {i}] e.Next is null after split. Original: {e}");

                Assert.AreSame(
                    inserted, e1!.Dest,
                    $"[{tag} | Edge {i}] Expected e1.Dest == inserted ({inserted}), got {e1.Dest}. Original: {e}"
                );

                var e2 = e1.Next;
                Assert.IsNotNull(e2, $"[{tag} | Edge {i}] e1.Next is null. e1: {e1}");

                Assert.AreSame(
                    e.Origin, e2!.Dest,
                    $"[{tag} | Edge {i}] Expected e2.Dest == e.Origin ({e.Origin}), got {e2.Dest}. e1: {e1}, Original: {e}"
                );

                var expectedNextOriginal = originalEdges[(i + 1) % n];
                Assert.IsNotNull(e1.Twin, $"[{tag} | Edge {i}] e1.Twin is null. e1: {e1}");

                Assert.AreSame(
                    expectedNextOriginal, e1.Twin!.Next,
                    $"[{tag} | Edge {i}] Expected e1.Twin.Next == originalEdges[{(i + 1) % n}] ({expectedNextOriginal}), " +
                    $"but got {e1.Twin.Next}. e1: {e1}, Original: {e}"
                );
            }
        }
    }
}
