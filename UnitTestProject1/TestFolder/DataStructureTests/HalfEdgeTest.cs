using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Numerics;

namespace UnitTestProject1.TestFolder
{
    [TestClass]
    public class HalfEdgeTests
    {
        private Vertex CreateVertex(float x, float y) => new Vertex(new Vector2(x, y));

        [TestMethod]
        public void Constructor_ShouldInitializeOrigin_AndDefaults()
        {
            var vertex = CreateVertex(1f, 2f);
            var edge = new HalfEdge(vertex);

            Assert.AreEqual(vertex, edge.Origin);
            Assert.IsNull(edge.Twin);
            Assert.IsNull(edge.Next);
            Assert.IsNull(edge.Prev);
            Assert.IsNull(edge.Face);
            Assert.IsNull(edge.Dest);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_ShouldThrowIfOriginNull()
        {
            var edge = new HalfEdge(null);
        }

        [TestMethod]
        public void CreateHalfEdgePair_ShouldSetTwinsCorrectly()
        {
            var v1 = CreateVertex(0f, 0f);
            var v2 = CreateVertex(1f, 1f);

            var (edge, twin) = HalfEdge.CreateHalfEdgePair(v1, v2);

            Assert.AreEqual(v1, edge.Origin);
            Assert.AreEqual(v2, twin.Origin);
            Assert.AreSame(edge.Twin, twin);
            Assert.AreSame(twin.Twin, edge);

            // OutgoingHalfEdge
            Assert.AreEqual(edge, v1.OutgoingHalfEdge);
            Assert.AreEqual(twin, v2.OutgoingHalfEdge);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateHalfEdgePair_ShouldThrowIfFromNull()
        {
            var v = CreateVertex(0, 0);
            HalfEdge.CreateHalfEdgePair(null, v);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateHalfEdgePair_ShouldThrowIfToNull()
        {
            var v = CreateVertex(0, 0);
            HalfEdge.CreateHalfEdgePair(v, null);
        }

        [TestMethod]
        public void NextPrevDest_ShouldComputeCorrectlyForTriangle()
        {
            var v1 = CreateVertex(0f, 0f);
            var v2 = CreateVertex(1f, 0f);
            var v3 = CreateVertex(0f, 1f);

            var e1 = new HalfEdge(v1);
            var e2 = new HalfEdge(v2);
            var e3 = new HalfEdge(v3);

            e1.Next = e2;
            e2.Next = e3;
            e3.Next = e1;

            // Dest
            Assert.AreEqual(v2, e1.Dest);
            Assert.AreEqual(v3, e2.Dest);
            Assert.AreEqual(v1, e3.Dest);

            // Prev fallback for triangle
            Assert.AreSame(e3, e1.Prev);
            Assert.AreSame(e1, e2.Prev);
            Assert.AreSame(e2, e3.Prev);
        }

        [TestMethod]
        public void Prev_CanBeExplicitlySet()
        {
            var v1 = CreateVertex(0f, 0f);
            var v2 = CreateVertex(1f, 0f);
            var v3 = CreateVertex(0f, 1f);

            var e1 = new HalfEdge(v1);
            var e2 = new HalfEdge(v2);
            var e3 = new HalfEdge(v3);

            e1.Next = e2;
            e2.Next = e3;
            e3.Next = e1;

            // Override Prev manually
            e1.Prev = e2;
            Assert.AreSame(e2, e1.Prev);
        }

        [TestMethod]
        public void ToString_ShouldReturnOriginToDest()
        {
            var v1 = CreateVertex(0f, 0f);
            var v2 = CreateVertex(1f, 1f);

            var e1 = new HalfEdge(v1);
            var e2 = new HalfEdge(v2);
            e1.Next = e2;

            string str = e1.ToString();
            StringAssert.Contains(str, "Vertex(0.00, 0.00)");
            StringAssert.Contains(str, "Vertex(1.00, 1.00)");
        }


    }
}
