using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WindowsFormsApp1.myitem.GeometryFolder;

[TestClass]
public class PointLocatorTest
{
    // Shared vertices and face list for general use
    private Vertex vA, vB, vC, vD, vE;
    private List<Face> faceList;

    [TestInitialize]
    public void Setup()
    {
        vA = new Vertex(new Vector2(100, 100));
        vB = new Vertex(new Vector2(700, 100));
        vC = new Vertex(new Vector2(400, 500));
        vD = new Vertex(new Vector2(200, 200));
        vE = new Vertex(new Vector2(600, 200));

        var originalFace = new Face(vA, vB, vC);
        faceList = new List<Face> { originalFace };

        var firstSplit = TriangulationOperation.SplitTriangle(originalFace, vD);
        faceList.Remove(originalFace);
        faceList.AddRange(firstSplit);

        var secondSplit = TriangulationOperation.SplitTriangle(firstSplit[0], vE);
        faceList.Remove(firstSplit[0]);
        faceList.AddRange(secondSplit);
    }

 

    public static List<HalfEdge> LocateAndAssertMidEdgeVertices(Face startFace, Face targetFace)
    {
        var locatedEdges = new List<HalfEdge>();

        foreach (var edge in targetFace.GetEdges())
        {
            // Compute midpoint
            Vector2 midPoint = (edge.Origin.Position + edge.Dest.Position) / 2f;
            var vertex = new Vertex(midPoint);

            // Locate vertex starting from startFace
            var locator = PointLocator.LocatePointInMesh(startFace, vertex);

            // Assert that the point is on an edge
            Assert.IsTrue(locator.isOnEdge,
                $"Vertex at {vertex.Position} was expected to lie on an edge, but isOnEdge is false.");

            var destEdge = locator.destinationEdge;
            Assert.IsNotNull(destEdge, "Located edge is null.");

            // Assert collinear using TriangleOrientation
            int orientation = GeometryUtils.TriangleOrientation(destEdge.Origin, destEdge.Dest, vertex);
            Assert.IsTrue(orientation == 0,
                $"Vertex at {vertex.Position} is not collinear with edge {destEdge.Origin.Position} -> {destEdge.Dest.Position}.");

            // Assert strictly between endpoints
            Vector2 edgeVec = destEdge.Dest.Position - destEdge.Origin.Position;
            Vector2 vertexVec = vertex.Position - destEdge.Origin.Position;
            float dot = Vector2.Dot(edgeVec, vertexVec);
            float lenSq = edgeVec.LengthSquared();
            Assert.IsTrue(dot > 0 && dot < lenSq,
                $"Vertex at {vertex.Position} is not strictly between endpoints {destEdge.Origin.Position} -> {destEdge.Dest.Position}.");

            locatedEdges.Add(destEdge);
        }

        return locatedEdges;
    }



    /// <summary>
    /// Test locating mid-edge vertices on all pairs of faces using the consolidated helper.
    /// </summary>
    [TestMethod]
    public void LocateVertexOnEdgeAllPairOfFace()
    {
        foreach (var startFace in faceList)
        {
            foreach (var targetFace in faceList)
            {
                // Locate and assert mid-edge vertices
                var locatedEdges = LocateAndAssertMidEdgeVertices(startFace, targetFace);

                // Verify the number of located edges matches the number of edges in targetFace
                Assert.AreEqual(targetFace.GetEdges().Count(), locatedEdges.Count,
                    $"StartFace {startFace.Id}, TargetFace {targetFace.Id}: Number of located edges does not match.");
            }
        }
    }


}
