using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject1.TestFolder
{
    [TestClass]
    public class HalfEdgeTests
    {
        [TestMethod]
        public void Constructor_ShouldInitializeOrigin_AndDefaults()
        {
            var vertex = new Vertex(new Vector2(1f, 2f));
            var edge = new HalfEdge(vertex);

            Assert.AreEqual(vertex, edge.Origin);
            Assert.IsNull(edge.Twin);
            Assert.IsNull(edge.Next);
            Assert.IsNull(edge.Prev);
            Assert.IsNull(edge.Face);
            Assert.IsFalse(edge.IsConstrained);
            Assert.IsNull(edge.Dest);
        }

        [TestMethod]
        public void CreateHalfEdgePair_ShouldSetTwinsCorrectly()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 1f));

            var (edge, twin) = HalfEdge.CreateHalfEdgePair(v1, v2);

            Assert.AreEqual(v1, edge.Origin);
            Assert.AreEqual(v2, twin.Origin);
            Assert.AreSame(edge.Twin, twin);
            Assert.AreSame(twin.Twin, edge);
        }

        [TestMethod]
        public void NextPrevDest_ShouldComputeCorrectlyForTriangle()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 0f));
            var v3 = new Vertex(new Vector2(0f, 1f));

            var e1 = new HalfEdge(v1);
            var e2 = new HalfEdge(v2);
            var e3 = new HalfEdge(v3);

            e1.Next = e2;
            e2.Next = e3;
            e3.Next = e1;

            // Prev for triangular face: Next.Next
            Assert.AreSame(e3, e1.Prev);
            Assert.AreSame(e1, e3.Next);
            Assert.AreEqual(v2, e1.Dest);
        }

        [TestMethod]
        public void Destroy_ShouldNullifyReferences()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 1f));

            var (edge, twin) = HalfEdge.CreateHalfEdgePair(v1, v2);

            edge.Destroy();

            Assert.IsNull(edge.Twin);
            Assert.IsNull(twin.Twin);
            Assert.IsNull(edge.Next);
            Assert.IsNull(edge.Prev);
            Assert.IsNull(edge.Face);
            Assert.IsNull(edge.Origin);
        }

        [TestMethod]
        public void ToString_ShouldReturnOriginToDest()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 1f));

            var e1 = new HalfEdge(v1);
            var e2 = new HalfEdge(v2);

            e1.Next = e2;

            Assert.AreEqual("Vertex(0.00, 0.00) -> Vertex(1.00, 1.00)", e1.ToString());
        }

        [TestMethod]
        public void Prev_CanBeExplicitlySet()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 0f));
            var v3 = new Vertex(new Vector2(0f, 1f));

            var e1 = new HalfEdge(v1);
            var e2 = new HalfEdge(v2);
            var e3 = new HalfEdge(v3);

            e1.Next = e2;
            e2.Next = e3;
            e3.Next = e1;

            // Manually override Prev
            e1.Prev = e2;
            Assert.AreSame(e2, e1.Prev);
        }

        [TestMethod]
        public void IsConstrained_DefaultsToFalse_AndCanBeSet()
        {
            var v = new Vertex(new Vector2(0f, 0f));
            var edge = new HalfEdge(v);

            Assert.IsFalse(edge.IsConstrained);

            edge.IsConstrained = true;
            Assert.IsTrue(edge.IsConstrained);
        }
    }

}
