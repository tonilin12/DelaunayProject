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
        private Vertex v1, v2, v3, v4;
        private HalfEdge e1, e2, e3, e4;


        [TestInitialize]
        public void Setup()
        {
            // Original vertices
            var origV1 = new Vector2(0, 0);
            var origV2 = new Vector2(1, 0);
            var origV3 = new Vector2(0, 1);
            var origV4 = new Vector2(1, 1);

            // Create vertex instances for testing
            v1 = new Vertex(origV1);
            v2 = new Vertex(origV2);
            v3 = new Vertex(origV3);
            v4 = new Vertex(origV4);

            // Create half-edges using copies of the vertices
            e1 = new HalfEdge(new Vertex(origV1));
            e2 = new HalfEdge(new Vertex(origV2));
            e3 = new HalfEdge(new Vertex(origV3));
            e4 = new HalfEdge(new Vertex(origV4));
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
            var face = new Face(v1, v2, v3);

            Assert.IsNotNull(face.Edge, "Face.Edge should not be null");
            Assert.IsNotNull(face.Edge.Next, "Face.Edge.Next should not be null");
            Assert.IsNotNull(face.Edge.Prev, "Face.Edge.Prev should not be null");
        }



        [TestMethod]
        public void FaceConstructor_EnumerateEdges_ForwardAndBackward_RunsWithoutError()
        {
            // Create face using vertex constructor
            var face = new Face(v1, v2, v3);

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
            var face = new Face(v1, v2, v3);
            var edges = face.EnumerateEdges(e => e).ToList();
            int edgeCount = edges.Count;

            // Forward, 1 step
            var forward1 = face.EnumerateEdges(e => e, steps: 1, forward: true).ToList();
            Assert.AreEqual(1, forward1.Count);
            Assert.AreSame(face.Edge, forward1[0]);

            // Backward, 1 step
            var backward1 = face.EnumerateEdges(e => e, steps: 1, forward: false).ToList();
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
            CollectionAssert.AreEqual(new[] { v1, v2, v3 }, forwardVertices, "Forward enumeration vertex order incorrect");

            var backwardVertices = backwardMany.Select(e => e.Origin).ToList();
            CollectionAssert.AreEqual(new[] { v1, v3, v2 }, backwardVertices, "Backward enumeration vertex order incorrect");
        }


        [TestMethod]
        public void FaceConstructor_GetVertices_ReturnsInputVertices()
        {
            // Arrange: create a face from known vertices
            var inputVertices = new[] { v1, v2, v3 };
            var face = new Face(inputVertices);

            // Act: get vertices from the face
            var outputVertices = face.GetVertices().ToList();

            // Assert: the sequence of vertices matches exactly the input
            CollectionAssert.AreEqual(inputVertices, outputVertices,
                "GetVertices should return the same vertices as were passed to the constructor.");
        }
    }

    
        [TestMethod]
        public void GetOppositeTwinEdge_FindsCorrectTwin()
        {
            // Arrange
            var face = new Face(e1, e2, e3);

            // Act
            var twinForV1 = face.GetOppositeTwinEdge(v1);
            var twinForV2 = face.GetOppositeTwinEdge(v2);
            var twinForV3 = face.GetOppositeTwinEdge(v3);

            // Assert
            Assert.AreSame(e3.Twin, twinForV1, "Twin opposite v1 is incorrect");
            Assert.AreSame(e1.Twin, twinForV2, "Twin opposite v2 is incorrect");
            Assert.AreSame(e2.Twin, twinForV3, "Twin opposite v3 is incorrect");
        }
    }
}
