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


        [TestInitialize]

        public void Setup()
        {
            // Shared middle edge
            var vA = new Vertex(0.25f, 0f);  // left point of middle edge
            var vB = new Vertex(0.75f, 0f);  // right point of middle edge

            // One vertex above and one below, forming a convex quad
            var vC = new Vertex(0.5f, 0.5f);   // top vertex
            var vD = new Vertex(0.5f, -0.5f);  // bottom vertex


            // Faces
            face1 = new Face(vA, vB, vC);
            face2 = new Face(vB, vA, vD);

            // Link twin edges
            face1.Edge.Twin = face2.Edge;
            face2.Edge.Twin = face1.Edge;

        }



        [TestMethod]
        public void SharedEdge_AfterFlip_TwinAndPositionsCorrect()
        {
            var edge = face1.Edge;
            var twin = face2.Edge;


            TriangulationOperation.FlipEdge(edge);


            // Assert twin references
            Assert.AreEqual(edge.Twin, twin, "Edge.Twin should reference its twin.");
            Assert.AreEqual(twin.Twin, edge, "Twin.Twin should reference the original edge.");

            // Assert positions using PositionsEqual
            Assert.IsTrue(edge.Origin.PositionsEqual(twin.Dest), "Edge origin should match twin destination.");
            Assert.IsTrue(edge.Dest.PositionsEqual(twin.Origin), "Edge destination should match twin origin.");

        }



        [TestMethod]
        public void AfterFlip_HalfEdgeCycleIntegrity()
        {
            // Arrange: take a shared edge and flip it
            var edge = face1.Edge;
            TriangulationOperation.FlipEdge(edge);


            var faces = new[] { (face1, "face1"), (face2, "face2") };

            // Act & Assert: check that half-edge cycles are well-circulated
            foreach (var (face, faceName) in faces)
            {
                List<HalfEdge> edges;
                try
                {
                    edges = face.GetEdges().ToList();
                }
                catch (Exception ex)
                {
                    Assert.Fail($"{faceName} enumeration failed: {ex.Message}");
                    return;
                }

                // Enumeration must always be 3 for triangles
                if (edges.Count != 3)
                    Assert.Fail($"{faceName} enumeration does not have 3 edges: got {edges.Count}");
            }

            // Explicit final check
            Assert.IsTrue(true, @"
                    All half-edge cycles enumerated successfully 
                    with appropriate cycle length."
            );
        }



        // === Helper: attach or snapshot twins for edges expected to stay unchanged ===
        private static Dictionary<HalfEdge, HalfEdge> AttachOrSnapshotTwins(IEnumerable<HalfEdge> edges)
        {
            var map = new Dictionary<HalfEdge, HalfEdge>();
            foreach (var e in edges)
            {
                if (e.Twin == null)
                {
                    // Lightweight dummy twin: reverse direction via Origin = e.Dest
                    var dummy = new HalfEdge(e.Dest!);
                    dummy.Twin = e;
                    e.Twin = dummy;
                    map[e] = dummy;
                }
                else
                {
                    map[e] = e.Twin!;
                }
            }
            return map;
        }


        [TestMethod]
        public void TestFlipCorrectness()
        {
            // Arrange: take a shared edge and flip it
            var edge = face1.Edge;

            // The two opposite vertices (one from each adjacent triangle) before the flip
            var oppositeVerticesBefore = (
                FromFace1: edge?.Next?.Dest,
                FromFace2: edge?.Twin?.Next?.Dest
            );

            // Collect edges that should remain unchanged (skip the first edge in each face)
            var edges_nochange = new HashSet<HalfEdge>();
            foreach (var e in face1.GetEdges().Skip(1)) edges_nochange.Add(e);
            foreach (var e in face2.GetEdges().Skip(1)) edges_nochange.Add(e);

            // NEW: snapshot/attach twins for all no-change edges
            var noChangeTwinSnapshot = AttachOrSnapshotTwins(edges_nochange);

            var nochangeVertecies_positions = noChangeTwinSnapshot.Keys.Select(e => e.Origin.Position).ToList();

            // Act
            TriangulationOperation.FlipEdge(edge);


            for (int i = 0; i < noChangeTwinSnapshot.Count; i++)
            {
                var e0 = noChangeTwinSnapshot.Keys.ElementAt(i);
                var v0 = e0.Origin;




                var expectedPos = nochangeVertecies_positions[i];
                var actualPos = v0.Position;

                Assert.AreEqual(expectedPos, actualPos,
                    $"[NoChange|Vertex {i}] Vertex position changed — expected {expectedPos}, got {actualPos}.");

                // Outgoing-edge consistency check
                var outEdge = v0.OutgoingHalfEdge;
                if (outEdge != null)
                {
                    Assert.AreSame(v0, outEdge.Origin,
                        $"[NoChange|Vertex {i}] Vertex's OutgoingHalfEdge origin mismatch — " +
                        $"expected Origin = {v0}, but found {outEdge.Origin}. " +
                        $"Edge: {e0}, OutgoingEdge: {outEdge}. Possible inconsistent half-edge linkage.");
                }

            }



            // After flip, edge should connect the two opposite vertices
            bool edgeConnectsOpposites =
                edge.Origin.PositionsEqual(oppositeVerticesBefore.FromFace1) &&
                edge.Dest.PositionsEqual(oppositeVerticesBefore.FromFace2);

            bool twinConnectsOpposites =
                edge.Twin.Origin.PositionsEqual(oppositeVerticesBefore.FromFace2) &&
                edge.Twin.Dest.PositionsEqual(oppositeVerticesBefore.FromFace1);

            Assert.IsTrue(edgeConnectsOpposites, "Flipped edge should connect the opposite vertices in correct orientation.");
            Assert.IsTrue(twinConnectsOpposites, "Twin of flipped edge should connect the opposite vertices in reverse orientation.");

            // Assert: all other edges remain unchanged (reference-equal presence in faces)
            foreach (var e in face1.GetEdges().Skip(1))
            {

                // Edge should still belong to face1
                Assert.AreSame(face1, e.Face,
                    $"Edge {e} from face1 lost its face reference — expected Face=face1, actual Face={e.Face}.");

                bool removed = edges_nochange.Remove(e);
                Assert.IsTrue(removed, $"Edge {e} from face1 should remain unchanged after flip.");
            }
            foreach (var e in face2.GetEdges().Skip(1))
            {


                // Edge should still belong to face1
                Assert.AreSame(face2, e.Face,
                    $"Edge {e} from face1 lost its face reference — expected Face=face1, actual Face={e.Face}.");

                bool removed = edges_nochange.Remove(e);
                Assert.IsTrue(removed, $"Edge {e} from face2 should remain unchanged after flip.");
            }
            Assert.IsTrue(edges_nochange.Count == 0, "Some edges expected to remain unchanged were not found after flip.");

            // NEW: twins for no-change edges must be preserved (same refs + back-link)
            foreach (var kvp in noChangeTwinSnapshot)
            {
                var e = kvp.Key;          // original no-change edge instance
                var expectedTwin = kvp.Value; // its twin snapshot (dummy or real)

                Assert.AreSame(expectedTwin, e.Twin,
                    $"Twin reference changed for {e}. Expected: {expectedTwin}, Actual: {e.Twin}");
                Assert.AreSame(e, e.Twin!.Twin,
                    $"Twin back-link broken for {e}. Expected e.Twin.Twin == e, Actual: {e.Twin.Twin}");
            }

            // Orientation consistency
            var face1_vertices = face1.GetEdges().Select(e => e.Origin).ToArray();
            var face2_vertices = face2.GetEdges().Select(e => e.Origin).ToArray();
            var orientation1 = GeometryUtils.GetSignedArea(face1_vertices);
            var orientation2 = GeometryUtils.GetSignedArea(face2_vertices);

            Assert.IsTrue(orientation1 * orientation2 > 0,
                "Face1 and Face2 should have consistent orientation after flip.");
        }


        [TestMethod]
        public void DoubleFlip_Testcase()
        {
            // Arrange: take a shared edge and record initial configuration
            var edge = face1.Edge;

            // Record original vertices of both faces
            var originalFace1Vertices = face1.GetEdges().Select(e => e.Origin).ToArray();
            var originalFace2Vertices = face2.GetEdges().Select(e => e.Origin).ToArray();

            // Record original twin relationships
            var originalTwin = edge.Twin;

            // Record original destinations for easy comparison
            var originalEdgeOrigin = edge.Origin;
            var originalEdgeDest = edge.Dest;


            // Act: flip the edge twice
            TriangulationOperation.FlipEdge(edge); // first flip
            TriangulationOperation.FlipEdge(edge); // first flip

            bool edge_swapped =
            edge.Origin.PositionsEqual(originalEdgeDest) &&
            edge.Dest.PositionsEqual(originalEdgeOrigin);

            Assert.IsTrue(edge_swapped, "flipping edge twice unexpected result topology corruption");
        }
    }
}

