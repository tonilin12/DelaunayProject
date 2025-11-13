using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TestProject1.TestFolder.DataStructureTestFolder
{
    [TestClass]
    public class FaceTests
    {
        private Vertex? vA, vB, vC, vD;
        private HalfEdge? eA, eB, eC, eD;


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
            vA = new Vertex(pA.X, pA.Y);
            vB = new Vertex(pB.X, pB.Y);
            vC = new Vertex(pC.X, pC.Y);
            vD = new Vertex(pD.X, pD.Y);

            // Create half-edges using copies of the vertices
            eA = new HalfEdge(new Vertex(pA.X, pA.Y));
            eB = new HalfEdge(new Vertex(pB.X, pB.Y));
            eC = new HalfEdge(new Vertex(pC.X, pC.Y));
            eD = new HalfEdge(new Vertex(pD.X, pD.Y));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FaceConstructor_FromVertices_CollinearVertices_Throws()
        {
            // Collinear vertices along the X-axis
            var vA = new Vertex(0, 0);
            var vB = new Vertex(1, 0);
            var vC = new Vertex(2, 0);

            // Attempt to create face should throw
            var face = new Face(vA, vB, vC);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FaceConstructor_FromVertices_ClockwiseVertices_Throws()
        {
            _ = new Face(vA,vC,vB);
        }



        [TestMethod]

        public void FaceConstructor_FromVertices_EdgeAndLinks_NotNull()
        {
            // Create face using vertex constructor
            var face = new Face(vA, vB, vC);

            Assert.IsNotNull(face.Edge, "Face.Edge should not be null");
            Assert.IsNotNull(face.Edge.Next, "Face.Edge.Next should not be null");
        }


        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FaceConstructor_FromVertices_NotThree_Throws()
        {
            var vA = new Vertex(0, 0);
            var vB = new Vertex(1, 0);
            var vC = new Vertex(0, 1);
            var vD = new Vertex(1, 1);

            // Too few vertices
            var faceA = new Face(vA, vB);

            // Too many vertices
            var faceB = new Face(vA, vB, vC, vD);
        }

        [TestMethod]
        public void FaceConstructor_FromVertices_NullInput_Throws()
        {
            Vertex[] vertices = null;

            Assert.ThrowsException<ArgumentNullException>(() => new Face(vertices));
        }



        [TestMethod]
        public void EnumerateEdgesAndVertices_RunsWithoutError()
        {
            // Create face using vertex constructor
            var face = new Face(vA, vB, vC);

            // Enumerate edges forward
            List<HalfEdge> edges = null;
            try
            {
                edges = face.GetEdges().ToList();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Enumeration failed: {ex.Message}");
            }
            // Verify that all edges reference the correct face
            foreach (var edge in edges)
            {
                Assert.AreSame(face, edge.Face, "Each half-edge should reference its parent face.");
            }

            // Check edges
            Assert.IsNotNull(edges, "Edges should not be null");
            Assert.AreEqual(3, edges.Count, "Triangle should have exactly 3 edges");

            // Check vertices robustly
            var expectedVertices = new[] { vA, vB, vC };
            var actualVertices = face.GetVertices().ToList(); // assumes face.Vertices exists

            Assert.AreEqual(expectedVertices.Length, actualVertices.Count, "Face should have exactly 3 vertices");

            for (int i = 0; i < expectedVertices.Length; i++)
            {
                Assert.AreSame(expectedVertices[i], actualVertices[i], $"Vertex at index {i} is not the exact instance expected.");
            }

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
        public void FaceToString_RoundTrip_VertexPositionsMatch()
        {


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
                return new Vertex(
                    float.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture),
                    float.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture)
                );
            }).ToList();

            // Step 5: Assert that the parsed vertices match the original vertices
            Assert.IsTrue(vA.PositionsEqual(parsedVertices[0]), "Vertex 1 round-trip failed");
            Assert.IsTrue(vB.PositionsEqual(parsedVertices[1]), "Vertex 2 round-trip failed");
            Assert.IsTrue(vC.PositionsEqual(parsedVertices[2]), "Vertex 3 round-trip failed");
        
        }

    }
}
