using ClassLibrary2.GeometryFolder;
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
                // Link twin edges
                face1.Edge.Twin = face2.Edge;
                face2.Edge.Twin = face1.Edge;
            }



            [TestMethod]
            public void SharedEdge_AfterFlip_TwinAndPositionsCorrect()
            {
                var edge = face1.Edge;
                var twin = face2.Edge;


                TriangulationOperation.FlipEdge(ref edge);


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
                TriangulationOperation.FlipEdge(ref edge);


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


            private bool IsLegalTriangle(IReadOnlyList<Vertex> verts)
            {
                if (verts.Count != 3) return false;

                // Distinct vertices
                if (verts.Distinct().Count() != 3) return false;

                // Check 2D area to ensure non-collinear
                var a = verts[0].Position;
                var b = verts[1].Position;
                var c = verts[2].Position;

                float area = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
                return Math.Abs(area) > float.Epsilon;
            }



            [TestMethod]
            public void TestFlipCorrectness()
            {
                // Arrange: take a shared edge and flip it
                var edge = face1.Edge;



                // The two opposite vertices (one from each adjacent triangle) before the flip
                var oppositeVerticesBefore = (
                    FromFace1: edge.Next.Next.Origin,
                    FromFace2: edge.Twin.Next.Next.Origin
                );


                // Collect edges that should remain unchanged
                // (skip the first edge in each face)

                var edges_nochange = new HashSet<HalfEdge>();

                foreach (var e in face1.GetEdges().Skip(1))
                    edges_nochange.Add(e);
                foreach (var e in face2.GetEdges().Skip(1))
                    edges_nochange.Add(e);




                TriangulationOperation.FlipEdge(ref edge);


                // After flip, edge should connect the two opposite vertices
                bool edgeConnectsOpposites =
                    edge.Origin.PositionsEqual(oppositeVerticesBefore.FromFace1) &&
                    edge.Dest.PositionsEqual(oppositeVerticesBefore.FromFace2);

                // Its twin should connect them in the opposite direction
                bool twinConnectsOpposites =
                    edge.Twin.Origin.PositionsEqual(oppositeVerticesBefore.FromFace2) &&
                    edge.Twin.Dest.PositionsEqual(oppositeVerticesBefore.FromFace1);

                Assert.IsTrue(edgeConnectsOpposites, "Flipped edge should connect the opposite vertices in correct orientation.");
                Assert.IsTrue(twinConnectsOpposites, "Twin of flipped edge should connect the opposite vertices in reverse orientation.");



                // Assert: all other edges remain unchanged
                foreach (var e in face1.GetEdges().Skip(1))
                {
                    bool removed = edges_nochange.Remove(e);
                    Assert.IsTrue(removed, $"Edge {e} from face1 should remain unchanged after flip.");
                }

                foreach (var e in face2.GetEdges().Skip(1))
                {
                    bool removed = edges_nochange.Remove(e);
                    Assert.IsTrue(removed, $"Edge {e} from face2 should remain unchanged after flip.");
                }

                // Final check: all expected unchanged edges were seen
                Assert.IsTrue(edges_nochange.Count == 0, "Some edges expected to remain unchanged were not found after flip.");


             var face1_vertices = face1.GetEdges().Select(e => e.Origin).ToArray();
             var face2_vertices = face2.GetEdges().Select(e => e.Origin).ToArray();


            var orientation1 = GeometryUtils.GetSignedArea(face1_vertices);
                var orientation2 = GeometryUtils.GetSignedArea(face2_vertices);


                // Additionally, they should be consistent
                Assert.IsTrue(orientation1 * orientation2 > 0,
                    "Face1 and Face2 should have consistent orientation after flip.");


            }


        }
    }

