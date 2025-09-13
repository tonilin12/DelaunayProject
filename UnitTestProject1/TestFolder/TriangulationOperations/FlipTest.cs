using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject1.TestFolder.TriangulationOperations
{

    [TestClass]
    public class FlipTest
    {

        private Face face1, face2;


        [TestInitialize]

        public void Setup()
        {
            // Shared middle edge
            var vA = new Vertex(new Vector2(0.25f, 0f));  // left point of middle edge
            var vB = new Vertex(new Vector2(0.75f, 0f));  // right point of middle edge

            // One vertex above and one below, forming a convex quad
            var vC = new Vertex(new Vector2(0.5f, 0.5f));   // top vertex
            var vD = new Vertex(new Vector2(0.5f, -0.5f));  // bottom vertex


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
            var directions = new[] { (true, "forward"), (false, "backward") };

            // Act & Assert: check that half-edge cycles are well-circulated
            foreach (var (face, faceName) in faces)
            {
                int? expectedLength = null;

                foreach (var (forward, dirName) in directions)
                {
                    List<HalfEdge> edges;
                    try
                    {
                        edges = face.EnumerateEdges(e => e, forward: forward).ToList();
                    }
                    catch (Exception ex)
                    {
                        Assert.Fail($"{faceName} {dirName} enumeration failed: {ex.Message}");
                        return;
                    }

                    // Ensure both directions enumerate the same number of edges
                    if (expectedLength == null)
                        expectedLength = edges.Count;
                    else if (edges.Count != expectedLength)
                        Assert.Fail($"{faceName} {dirName} enumeration length mismatch: expected {expectedLength}, got {edges.Count}");

                    // Forward enumeration must always be 3 for triangles
                    if (forward && edges.Count != 3)
                        Assert.Fail($"{faceName} forward enumeration does not have 3 edges: got {edges.Count}");
                }
            }
            // Explicit final check
            Assert.IsTrue(true, $@"
                All half-edge cycles enumerated successfully 
                in both directions with appropriate cycle length."
            );
        }


    }
}

