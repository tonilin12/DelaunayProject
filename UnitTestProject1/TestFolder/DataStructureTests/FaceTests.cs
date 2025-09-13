using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace UnitTestProject1.TestFolder
{
    [TestClass]
    public class FaceTests
    {
        private Vertex vA, vB, vC, vD;
        private HalfEdge eA, eB, eC, eD;


        [TestInitialize]
        public void Setup()
        {
            // Original vertices
            // Shared middle edge
            Vector2 pA = new Vector2(0.25f, 0f);  // left point of middle edge
            Vector2 pB = new Vector2(0.75f, 0f);  // right point of middle edge

            // One vertex above and one below, forming a convex quadrilateral
            Vector2 pC = new Vector2(0.5f, 0.5f);   // top vertex
            Vector2 pD = new Vector2(0.5f, -0.5f);  // bottom vertex


            // Create vertex instances for testing
            vA = new Vertex(pA);
            vB = new Vertex(pB);
            vC = new Vertex(pC);
            vD = new Vertex(pD);

            // Create half-edges using copies of the vertices
            eA = new HalfEdge(new Vertex(pA));
            eB = new HalfEdge(new Vertex(pB));
            eC = new HalfEdge(new Vertex(pC));
            eD = new HalfEdge(new Vertex(pD));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FaceConstructor_FromVertices_CollinearVertices_Throws()
        {
            // Collinear vertices along the X-axis
            var vA = new Vertex(new Vector2(0, 0));
            var vB = new Vertex(new Vector2(1, 0));
            var vC = new Vertex(new Vector2(2, 0));

            // Attempt to create face should throw
            var face = new Face(vA, vB, vC);
        }


        [TestMethod]

        public void FaceConstructor_FromVertices_EdgeAndLinks_NotNull()
        {
            // Create face using vertex constructor
            var face = new Face(vA, vB, vC);

            Assert.IsNotNull(face.Edge, "Face.Edge should not be null");
            Assert.IsNotNull(face.Edge.Next, "Face.Edge.Next should not be null");
            Assert.IsNotNull(face.Edge.Prev, "Face.Edge.Prev should not be null");
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FaceConstructor_FromVertices_NotThree_Throws()
        {
            var vA = new Vertex(new Vector2(0, 0));
            var vB = new Vertex(new Vector2(1, 0));
            var vC = new Vertex(new Vector2(0, 1));
            var vD = new Vertex(new Vector2(1, 1));

            // Too few vertices
            var faceA = new Face(vA, vB);

            // Too many vertices
            var faceB = new Face(vA, vB, vC, vD);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FaceConstructor_FromVertices_NullInput_Throws()
        {
            Vertex[] vertices = null;

            // Null input should throw ArgumentException
            var face = new Face(vertices);
        }



        [TestMethod]
        public void FaceConstructor_EnumerateEdges_ForwardAndBackward_RunsWithoutError()
        {
            // Create face using vertex constructor
            var face = new Face(vA, vB, vC);

            try
            {
                // Enumerate edges forward
                var forwardEdges = face.EnumerateEdges(e => e, forward: true).ToList();

                // Enumerate edges backward
                var backwardEdges = face.EnumerateEdges(e => e, forward: false).ToList();
            }
            catch (Exception ex)
            {
                Assert.Fail($"EnumerateEdges threw an exception: {ex.Message}");
            }

            // If we reach here, enumeration succeeded in both directions
            Assert.IsTrue(true, "EnumerateEdges ran successfully in both forward and backward directions.");
        }


        [TestMethod]
        public void EnumerateEdges_Steps_ForwardBackward()
        {
            // Arrange: triangle face
            var face = new Face(vA, vB, vC);
            var edges = face.EnumerateEdges(e => e).ToList();
            int edgeCount = edges.Count;

            // Forward, 1 step
            var forward1 = face.EnumerateEdges(e => e, steps: 1, forward: true).ToList();
            Assert.AreEqual(1, forward1.Count);
            Assert.AreSame(face.Edge, forward1[0]);

            // Backward, 1 step
            var backward1 = face
                         .EnumerateEdges(e => e, steps: 1, forward: false).ToList();
            Assert.AreEqual(1, backward1.Count);
            Assert.AreSame(face.Edge, backward1[0]);

            // Forward, more steps than edge count
            var forwardMany = face.EnumerateEdges(e => e, steps: edgeCount + 2, forward: true).ToList();
            Assert.AreEqual(edgeCount, forwardMany.Count, "Enumeration should stop at the start edge even if steps exceed edge count");

            // Backward, more steps than edge count
            var backwardMany = face.EnumerateEdges(e => e, steps: edgeCount + 2, forward: false).ToList();
            Assert.AreEqual(edgeCount, backwardMany.Count, "Enumeration should stop at the start edge even if steps exceed edge count");

            // Optional: check order matches expected vertices
            var forwardVertices = forwardMany.Select(e => e.Origin).ToList();
            CollectionAssert.AreEqual(new[] { vA, vB, vC }, forwardVertices, "Forward enumeration vertex order incorrect");

            var backwardVertices = backwardMany.Select(e => e.Origin).ToList();
            CollectionAssert.AreEqual(new[] { vA, vC, vB }, backwardVertices, "Backward enumeration vertex order incorrect");
        }


        [TestMethod]
        public void FaceConstructor_GetVertices_ReturnsInputVertices()
        {
            // Arrange: create a face from known vertices
            var inputVertices = new[] { vA, vB, vC };
            var face = new Face(inputVertices);

            // Act: get vertices from the face
            var outputVertices = face.GetVertices().ToList();

            // Assert: the sequence of vertices matches exactly the input
            CollectionAssert.AreEqual(inputVertices, outputVertices,
                "GetVertices should return the same vertices as were passed to the constructor.");
        }
    

    
        [TestMethod]
        public void GetOppositeTwinEdge_FindsCorrectTwin()
        {
            // Arrange
            var face = new Face(eA, eB, eC);

            // Act
            var twinForvA = face.GetOppositeTwinEdge(vA);
            var twinForvB = face.GetOppositeTwinEdge(vB);
            var twinForvC = face.GetOppositeTwinEdge(vC);

            // Assert
            Assert.AreSame(eC.Twin, twinForvA, "Twin opposite vA is incorrect");
            Assert.AreSame(eA.Twin, twinForvB, "Twin opposite vB is incorrect");
            Assert.AreSame(eB.Twin, twinForvC, "Twin opposite vC is incorrect");
        }


        [TestMethod]

        public void SharedEdgeOfTwoFaces_HasCorrectTwinsAndVertices()
        {
            // Faces using vertices
            var face1 = new Face(vA,vB,vC);
            var face2 = new Face(vB, vA, vD);

            // Link twin edges
            face1.Edge.Twin = face2.Edge;
            face2.Edge.Twin = face1.Edge;

            var edge1 = face1.Edge;
            var edge2 = face2.Edge;

            // Check twin references
            Assert.AreEqual(edge1.Twin, edge2, "Edge1.Twin should be Edge2");
            Assert.AreEqual(edge2.Twin, edge1, "Edge2.Twin should be Edge1");

            // Check positions using Vertex.Position
            Assert.IsTrue(edge1.Origin.PositionsEqual(edge2.Dest), "Edge1 origin should match Edge2 destination");
            Assert.IsTrue(edge1.Dest.PositionsEqual(edge2.Origin), "Edge1 destination should match Edge2 origin");


        }




        [TestMethod]
        public void FaceToString_RoundTrip_VertexPositionsMatch()
        {
            // Step 1: Create vertices
            var vA = new Vertex(new Vector2(1.1f, 2.2f));
            var vB = new Vertex(new Vector2(3.3f, 4.4f));
            var vC = new Vertex(new Vector2(5.5f, 6.6f));

            // Step 2: Create face using vertex constructor
            var face = new Face(vA, vB, vC);

            // Step 3: Convert face to string
            string str = face.ToString(); // Expected format: "Vertex(x1, y1) → Vertex(x2, y2) → Vertex(x3, y3)"

            // Step 4: Split string and extract coordinates
            var parts = str.Replace("Vertex(", "").Replace(")", "").Split(new[] { " → " }, StringSplitOptions.None);
            Assert.AreEqual(3, parts.Length, "ToString should produce exactly 3 vertices in output");

            var parsedVertices = parts.Select(p =>
            {
                var coords = p.Split(',');
                return new Vertex(new Vector2(
                    float.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture)
                ));
            }).ToList();

            // Step 5: Assert that the parsed vertices match the original vertices
            Assert.IsTrue(vA.PositionsEqual(parsedVertices[0]), "Vertex 1 round-trip failed");
            Assert.IsTrue(vB.PositionsEqual(parsedVertices[1]), "Vertex 2 round-trip failed");
            Assert.IsTrue(vC.PositionsEqual(parsedVertices[2]), "Vertex 3 round-trip failed");
        
        }

    }
}
