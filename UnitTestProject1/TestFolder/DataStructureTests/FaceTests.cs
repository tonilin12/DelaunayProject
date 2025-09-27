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

            // --- NEW PART: mark Voronoi cells dirty ---
            foreach (var vertex in actualVertices)
            {
                Assert.IsTrue(vertex.Voronoi.IsDirty, $"Voronoi cell for vertex {vertex.Position} should be dirty after Delaunay update.");
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
        public void GetOppositeEdge_ReturnsCorrectEdge_DirectIteration()
        {
            // Arrange: create a face with any 3 vertices
            var vertices = new[] { vA, vB, vC };
            var face = new Face(vertices[0], vertices[1], vertices[2]);

            // Act & Assert: for each vertex, find the edge opposite it
            foreach (var vertex in vertices)
            {
                var oppositeEdge = face.GetOppositeEdge(vertex);

                // Opposite edge must not be null
                Assert.IsNotNull(oppositeEdge, $"Opposite edge for vertex {vertex} should not be null");

                // Opposite edge must not contain the vertex as origin or next.origin
                Assert.IsFalse(oppositeEdge.Origin.PositionsEqual(vertex), $"Opposite edge origin matches vertex {vertex}");
                Assert.IsFalse(oppositeEdge.Next.Origin.PositionsEqual(vertex), $"Opposite edge next.origin matches vertex {vertex}");

                // Verify that the edge is part of the face by iterating directly
                bool found = false;
                foreach (var e in face.GetEdges())
                {
                    if (e == oppositeEdge)
                    {
                        found = true;
                        break;
                    }
                }

                Assert.IsTrue(found, $"Opposite edge for vertex {vertex} is not part of the face edges");
            }
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
