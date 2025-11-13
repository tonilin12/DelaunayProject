using ClassLibrary2.MeshFolder.Else;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.TestFolder.TriangulationOperations
{
    [TestClass]
    public class FlipTest
    {
        private Face face1, face2;
        private HalfEdge edge;


        [TestInitialize]
        public void Setup()
        {
            // Shared middle edge
            var vA = new Vertex(0.25f, 0f);   // left point of middle edge
            var vB = new Vertex(0.75f, 0f);   // right point of middle edge

            // One vertex above and one below, forming a convex quad
            var vC = new Vertex(0.5f, 0.5f);   // top vertex
            var vD = new Vertex(0.5f, -0.5f);  // bottom vertex

            // Faces
            face1 = new Face(vA, vB, vC);
            face2 = new Face(vB, vA, vD);

            // Link twin edges
            face1.Edge.Twin = face2.Edge;
            face2.Edge.Twin = face1.Edge;

            edge = face1.Edge;
        
        }

  

        [TestMethod]
        public void EdgeFlip_UnchangedEdgesRemainTheSame()
        {
            var edges_nochange = new HashSet<HalfEdge>();
            foreach (var e in face1.GetEdges().Skip(1)) edges_nochange.Add(e);
            foreach (var e in face2.GetEdges().Skip(1)) edges_nochange.Add(e);

            var edgesBefore = edges_nochange.ToDictionary(
                e => e,
                e => new { Origin = e.Origin, Dest = e.Dest, Twin = e.Twin }
            );

            TriangulationOperation.FlipEdge(edge);

            foreach (var kvp in edgesBefore)
            {
                var e = kvp.Key;
                var before = kvp.Value;

                Assert.AreEqual(before.Origin, e.Origin, "Origin should remain the same after flip.");
                Assert.AreEqual(before.Dest, e.Dest, "Dest should remain the same after flip.");
                Assert.AreEqual(before.Twin, e.Twin, "Twin reference should remain the same after flip.");
            }
        }


        [TestMethod]
        public void EdgeFlip_ConnectsOppositeVerticesCorrectly_Compact()
        {
            // Pre-flip snapshot
            var A = edge.Origin;
            var B = edge.Dest!;
            var C = edge.Next!.Dest!;
            var D = edge.Twin!.Next!.Dest!;

            // Act
            TriangulationOperation.FlipEdge(edge);

            // Post-flip expectations for both sides packed into one collection:
            // - edge   should be C -> D with third vertex B
            // - edge.Twin should be D -> C with third vertex A
            var sides = new[]
            {
                new { Diag = edge,       From = C, To = D, Third = B },
                new { Diag = edge.Twin!, From = D, To = C, Third = A }
            };

            foreach (var s in sides)
            {
                Assert.IsTrue(
                    s.Diag.Origin.Equals(s.From) &&
                    s.Diag.Next!.Origin.Equals(s.To) &&
                    s.Diag.Next!.Next!.Origin.Equals(s.Third),
                    "Diagonal endpoints must be former opposites; third vertex must be the original endpoint."
                );

                Assert.AreSame(
                    s.Diag, s.Diag.Next!.Next!.Next,
                    "Each face must form a 3-step .Next cycle."
                );
            }

            // Minimal twin consistency check on the new diagonal
            Assert.AreSame(edge, edge.Twin!.Twin, "Twin pointers on the flipped diagonal must be consistent.");
        }

        [TestMethod]
        public void EdgeFlip_EdgesReferenceCorrectFaces()
        {
            TriangulationOperation.FlipEdge(edge);

            foreach (var e in face1.GetEdges())
                Assert.AreSame(face1, e.Face, "Edge should reference Face1 after flip.");

            foreach (var e in face2.GetEdges())
                Assert.AreSame(face2, e.Face, "Edge should reference Face2 after flip.");
        }

        [TestMethod]
        public void EdgeFlip_VertexOutgoingEdgesConsistency()
        {
            var v1 = edge.Origin;
            var v2 = edge.Twin.Origin;

            // Pre-flip: set outgoing edges for the vertices
            v1.OutgoingHalfEdge = edge;
            v2.OutgoingHalfEdge = edge.Twin;

            // Act: flip the edge
            TriangulationOperation.FlipEdge(edge);

            // Post-flip: check that outgoing edges still originate from the correct vertices
            Assert.AreSame(v1, v1.OutgoingHalfEdge.Origin,
                "After flip, v1's outgoing edge should originate from v1.");
            Assert.AreSame(v2, v2.OutgoingHalfEdge.Origin,
                "After flip, v2's outgoing edge should originate from v2.");
        }


        [TestMethod]
        public void DoubleFlip_Testcase()
        {
            // Arrange
            var edge = face1.Edge;

            // Record original vertices and twin
            var originalEdgeOrigin = edge.Origin;
            var originalEdgeDest = edge.Dest;
            var originalTwin = edge.Twin;

            // Act: flip twice
            TriangulationOperation.FlipEdge(edge);
            TriangulationOperation.FlipEdge(edge);

            bool edge_swapped =
                edge.Origin.PositionsEqual(originalEdgeDest) &&
                edge.Dest.PositionsEqual(originalEdgeOrigin);

            Assert.IsTrue(edge_swapped,
                "Flipping edge twice should restore original topology without corruption.");

            // Twin should remain linked correctly
            Assert.AreEqual(edge.Twin, originalTwin, "Twin reference should remain correct after double flip.");
            Assert.AreEqual(edge.Twin.Twin, edge, "Twin’s twin should still reference the original edge.");
        }
    }
}
