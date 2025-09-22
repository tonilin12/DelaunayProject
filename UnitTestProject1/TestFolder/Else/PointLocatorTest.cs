using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WindowsFormsApp1.myitem.GeometryFolder;



namespace UnitTestProject1.TestFolder.Else
{
    [TestClass]
    public class PointLocatorTest
    {
        // Shared vertices and face list for general use
        private Vertex vA, vB, vC, vD, vE;
        private List<Face> faceList;



        /// <summary>
        /// Generate a point strictly inside a triangle using barycentric coordinates.
        /// Guarantees 0 < u,v,w < 1 to avoid edges.
        /// </summary>
        private Vertex GetStrictlyInsidePoint(Vertex v1, Vertex v2, Vertex v3)
        {
            // Use fixed barycentric weights strictly inside (can also randomize slightly)
            float u = 0.3f;
            float v = 0.3f;
            float w = 1f - u - v; // w = 0.5f

            // Compute the position
            Vector2 pos = u * v1.Position + v * v2.Position + w * v3.Position;
            return new Vertex(pos);
        }



        [TestInitialize]
        public void Setup()
        {
            // Initial triangle vertices
            vA = new Vertex(new Vector2(100, 100));
            vB = new Vertex(new Vector2(700, 100));
            vC = new Vertex(new Vector2(400, 500));

            // Split triangle to add internal vertices
            vD = GetStrictlyInsidePoint(vA, vB, vC);
            var face0 = new Face(vA, vB, vC);
            faceList = new List<Face> { face0 };

            var split1 = TriangulationOperation.SplitTriangle(face0, vD);
            faceList.Remove(face0);
            faceList.AddRange(split1);

            var face1= faceList.First();
            var vertecies=face1.GetVertices().ToArray();    
            vE = GetStrictlyInsidePoint(vertecies[0], vertecies[1], vertecies[2]);
            var split2 = TriangulationOperation.SplitTriangle(face1, vE);
            faceList.Remove(face1);
            faceList.AddRange(split2);

        }



        /// <summary>
        /// Locate midpoints of all edges of a target face and assert they are on edges.
        /// </summary>
        private List<HalfEdge> LocateAndAssertMidEdgeVertices(Face startFace, Face targetFace)
        {
            var locatedEdges = new List<HalfEdge>();

            foreach (var edge in targetFace.GetEdges())
            {
                Vector2 midPoint = (edge.Origin.Position + edge.Dest.Position) / 2f;
                var vertex = new Vertex(midPoint);

                var locator = PointLocator.LocatePointInMesh(startFace, vertex);

                // Assert the point is on an edge
                Assert.IsTrue(locator.isOnEdge,
                    $"Vertex at {vertex.Position} was expected to lie on an edge, but isOnEdge is false.");

                var destEdge = locator.destinationEdge;
                Assert.IsNotNull(destEdge, "Located edge is null.");

                // Assert collinearity
                int orientation = GeometryUtils.TriangleOrientation(destEdge.Origin, destEdge.Dest, vertex);
                Assert.IsTrue(orientation == 0,
                    $"Vertex at {vertex.Position} is not collinear with edge {destEdge.Origin.Position} -> {destEdge.Dest.Position}.");

                locatedEdges.Add(destEdge);
            }

            return locatedEdges;
        }


        [TestMethod]
        public void LocateMidEdgeVertices_AllPairs()
        {
            foreach (var startFace in faceList)
            {
                foreach (var targetFace in faceList)
                {
                    // Call your helper directly
                    var locatedEdges = LocateAndAssertMidEdgeVertices(startFace, targetFace);

                    // Optional: verify the number of located edges matches the target face's edges
                    Assert.AreEqual(targetFace.GetEdges().Count(), locatedEdges.Count,
                        $"StartFace -> TargetFace: Number of located edges does not match.");
                }
            }
        }


        /// <summary>
        /// Locate a point strictly inside a face and assert it's detected as inside.
        /// </summary>
        private void LocateAndAssertInside(Face startFace, Face targetFace)
        {
            var vertices = targetFace.GetVertices().ToArray();
            var insidePoint = GetStrictlyInsidePoint(vertices[0], vertices[1], vertices[2]);

            var locator = PointLocator.LocatePointInMesh(startFace, insidePoint);


            var destEdge = locator.destinationEdge;
            Assert.IsNotNull(destEdge, "Located edge is null.");


            // Point must not be on edge
            Assert.IsFalse(locator.isOnEdge, $"Vertex at {insidePoint.Position} should not lie on an edge {destEdge}.");


            var locatedFace = destEdge.Face;

            // Assert reference equality
            Assert.AreSame(targetFace, locatedFace,
                $"Point {insidePoint.Position} was expected to be inside the target face by reference.");
        }


        [TestMethod]
        public void LocateInsidePoint_AllPairs()
        {
            foreach (var startFace in faceList)
            {
                foreach (var targetFace in faceList)
                {
                    // Call the helper directly for inside points
                    LocateAndAssertInside(startFace, targetFace);
                }
            }
        }




        /// <summary>
        /// Locate all vertices of a target face and assert that the located edge's origin matches exactly the same vertex.
        /// </summary>
        private List<HalfEdge> LocateAndAssertFaceVerticesStrict(Face startFace, Face targetFace)
        {
            var locatedEdges = new List<HalfEdge>();

            // Get the vertices of the target face
            var targetVertices = targetFace.GetVertices().ToList();

            foreach (var vertex in targetVertices)
            {
                // Locate the vertex in the mesh
                var locator = PointLocator.LocatePointInMesh(startFace, vertex);

                // Assert that a destination edge was found
                Assert.IsNotNull(locator.destinationEdge, "Located edge is null.");

                var destEdge = locator.destinationEdge;

                // Assert that the located edge's origin matches the specific vertex
                Assert.IsTrue(destEdge.Origin.PositionsEqual(vertex),
                    $"Located edge origin {destEdge.Origin.Position} does not match the expected vertex {vertex.Position}.");


                locatedEdges.Add(destEdge);
            }

            return locatedEdges;
        }

        [TestMethod]
        public void LocateFaceVerticesStrict_AllPairs()
        {
            foreach (var startFace in faceList)
            {
                foreach (var targetFace in faceList)
                {
                    var locatedEdges = LocateAndAssertFaceVerticesStrict(startFace, targetFace);

                    // Verify the number of located edges matches the number of vertices of the target face
                    Assert.AreEqual(targetFace.GetVertices().Count(), locatedEdges.Count,
                        $"StartFace -> TargetFace: Number of located edges does not match number of vertices.");
                }
            }
        }
    }
}

