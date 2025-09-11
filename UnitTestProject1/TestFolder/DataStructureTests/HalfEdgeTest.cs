using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Numerics;
using WindowsFormsApp1; // Replace with your actual namespace

namespace UnitTestProject1.TestFolder
{
    [TestClass]
    public class HalfEdgeTests
    {
        [TestMethod]
        public void Constructor_SetsOriginAndOutgoingHalfEdge()
        {
            var v = new Vertex(new Vector2(1, 2));
            var edge = new HalfEdge(v);

            Assert.AreEqual(v, edge.Origin, "Origin should be set by constructor.");
            Assert.AreEqual(edge, v.OutgoingHalfEdge, "Vertex.OutgoingHalfEdge should point to the new half-edge.");
        }

        [TestMethod]
        public void Dest_ReturnsNextOrigin()
        {
            var v1 = new Vertex(new Vector2(0, 0));
            var v2 = new Vertex(new Vector2(1, 0));
            var edge = new HalfEdge(v1);
            var next = new HalfEdge(v2);
            edge.Next = next;

            Assert.AreEqual(v2, edge.Dest, "Dest should return Next.Origin.");
        }

        [TestMethod]
        public void Dest_ReturnsNull_WhenNextIsNull()
        {
            var v = new Vertex(new Vector2(0, 0));
            var edge = new HalfEdge(v);

            Assert.IsNull(edge.Dest, "Dest should be null if Next is null.");
        }

        [TestMethod]
        public void ToString_ReturnsFormattedString()
        {
            var v1 = new Vertex(new Vector2(1.0f, 2.0f));
            var v2 = new Vertex(new Vector2(3.0f, 4.0f));
            var edge = new HalfEdge(v1);
            edge.Next = new HalfEdge(v2);

            string s = edge.ToString();
            StringAssert.Contains(s, "Vertex(1.00, 2.00) -> Vertex(3.00, 4.00)");
        }

        [TestMethod]
        public void CreateHalfEdgePair_SetsTwinAndOriginsCorrectly()
        {
            var v1 = new Vertex(new Vector2(0, 0));
            var v2 = new Vertex(new Vector2(1, 0));

            var (edge, twin) = HalfEdge.CreateHalfEdgePair(v1, v2);

            // Check origins
            Assert.AreEqual(v1, edge.Origin, "Edge origin should be v1.");
            Assert.AreEqual(v2, twin.Origin, "Twin origin should be v2.");

            // Check twins
            Assert.AreEqual(twin, edge.Twin, "Edge twin should be assigned correctly.");
            Assert.AreEqual(edge, twin.Twin, "Twin twin should point back to edge.");

            // OutgoingHalfEdge should be updated
            Assert.AreEqual(edge, v1.OutgoingHalfEdge, "v1.OutgoingHalfEdge should be edge.");
            Assert.AreEqual(twin, v2.OutgoingHalfEdge, "v2.OutgoingHalfEdge should be twin.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_ThrowsOnNullOrigin()
        {
            var edge = new HalfEdge(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void CreateHalfEdgePair_ThrowsOnNullVertices()
        {
            var v = new Vertex(new Vector2(0, 0));
            var pair1 = HalfEdge.CreateHalfEdgePair(null, v);
            var pair2 = HalfEdge.CreateHalfEdgePair(v, null);
        }
    }
}
