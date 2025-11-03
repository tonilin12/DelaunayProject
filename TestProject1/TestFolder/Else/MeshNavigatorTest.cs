using ClassLibrary2.MeshFolder.Else;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TestProject1.TestFolder.Else
{
    [TestClass]
    public class MeshNavigatorTest
    {
        private Vertex vA, vB, vC, vD, vE;
        private List<Face> faceList;

        [TestInitialize]
        public void Setup()
        {
            vA = new Vertex(100, 100);
            vB = new Vertex(700, 100);
            vC = new Vertex(400, 500);

            var face0 = new Face(vA, vB, vC);
            faceList = new List<Face> { face0 };

            vD = GetStrictlyInsidePoint(vA, vB, vC);
            TriangulationOperation.SplitTriangle(face0, vD);
            faceList.Remove(face0);
            faceList.AddRange(vD.GetEdges().Select(e => e.Face));

            var face1 = faceList.First();
            var vertices1 = face1.GetVertices().ToArray();
            vE = GetStrictlyInsidePoint(vertices1[0], vertices1[1], vertices1[2]);
            TriangulationOperation.SplitTriangle(face1, vE);
            faceList.Remove(face1);
            faceList.AddRange(vE.GetEdges().Select(e => e.Face));
        }

        private Vertex GetStrictlyInsidePoint(Vertex v1, Vertex v2, Vertex v3)
        {
            float u = 0.3f, v = 0.3f;
            float w = 1f - u - v;
            Vector2 pos = u * v1.Position + v * v2.Position + w * v3.Position;
            return new Vertex(pos.X, pos.Y);
        }

        private static void AssertTraverseCorrect(Vertex vertex, IEnumerable<HalfEdge> allNextTwins)
        {
            foreach (var twin in allNextTwins)
            {
                var v1 = twin.Origin;
                var v2 = twin.Dest!;
                var orientation = GeometryUtils.GetSignedArea(v1, v2, vertex);
                Assert.IsTrue(orientation <0,
                    $"Vertex {vertex} and {twin} orientation mismatch. Orientation={orientation}");
            }
        }

        private static HalfEdge LocateVertexWithTraverse(Face startFace, Vertex vertex, bool expectOnEdge)
        {
            var (locatorEdge, isOnEdge, allNextTwins) = MeshNavigator.LocatePointAndLogTraverse(startFace, vertex);

            Assert.IsNotNull(locatorEdge, "Located edge is null.");
            Assert.AreEqual(expectOnEdge, isOnEdge,
                $"Vertex {vertex} on-edge expectation {expectOnEdge}, got {isOnEdge}.");

            AssertTraverseCorrect(vertex, allNextTwins);

            return locatorEdge;
        }

        private List<HalfEdge> LocateAndAssertMidEdgeVertices(Face startFace, Face targetFace)
        {
            return targetFace.GetEdges()
                .Select(edge =>
                {
                    var midPos = (edge.Origin.Position + edge.Dest.Position) / 2f;
                    var midVertex = new Vertex(midPos.X, midPos.Y);
                    return LocateVertexWithTraverse(startFace, midVertex, true);
                })
                .ToList();
        }

        private void LocateAndAssertInside(Face startFace, Face targetFace)
        {
            var vertices = targetFace.GetVertices().ToArray();
            var insidePoint = GetStrictlyInsidePoint(vertices[0], vertices[1], vertices[2]);

            var locatorEdge = LocateVertexWithTraverse(startFace, insidePoint, false);
            Assert.AreSame(targetFace, locatorEdge.Face,
                $"Vertex {insidePoint.Position} should be inside the target face.");
        }

        private List<HalfEdge> LocateAndAssertFaceVerticesStrict(Face startFace, Face targetFace)
        {
            return targetFace.GetVertices()
                .Select(vertex =>
                {
                    var locatorEdge = LocateVertexWithTraverse(startFace, vertex, true);
                    Assert.IsTrue(locatorEdge.Origin.PositionsEqual(vertex),
                        $"Located edge origin {locatorEdge.Origin.Position} does not match vertex {vertex.Position}.");
                    return locatorEdge;
                })
                .ToList();
        }

        [TestMethod]
        public void LocateMidEdgeVertices_AllPairs()
        {
            foreach (var startFace in faceList)
                foreach (var targetFace in faceList)
                    LocateAndAssertMidEdgeVertices(startFace, targetFace);
        }

        [TestMethod]
        public void LocateInsidePoint_AllPairs()
        {
            foreach (var startFace in faceList)
                foreach (var targetFace in faceList)
                    LocateAndAssertInside(startFace, targetFace);
        }

        [TestMethod]
        public void LocateFaceVerticesStrict_AllPairs()
        {
            foreach (var startFace in faceList)
                foreach (var targetFace in faceList)
                    LocateAndAssertFaceVerticesStrict(startFace, targetFace);
        }
    }
}
