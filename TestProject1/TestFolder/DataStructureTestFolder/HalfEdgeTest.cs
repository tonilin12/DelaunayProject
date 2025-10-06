using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Numerics;

namespace TestProject1.TestFolder.DataStructureTestFolder
{
    [TestClass]
    public class HalfEdgeTests
    {
        [TestMethod]
        public void Constructor_SetsOriginAndOutgoingHalfEdge()
        {
            var v = new Vertex(1, 2);
            var edge = new HalfEdge(v);

            Assert.AreEqual(v, edge.Origin, "Origin should be set by constructor.");
            Assert.AreEqual(edge, v.OutgoingHalfEdge, "Vertex.OutgoingHalfEdge should point to the new half-edge.");
        }

        [TestMethod]
        public void Dest_ReturnsNextOrigin()
        {
            var v1 = new Vertex(0, 0);
            var v2 = new Vertex(1, 0);
            var edge = new HalfEdge(v1);
            var next = new HalfEdge(v2);
            edge.Next = next;

            Assert.AreEqual(v2, edge.Dest, "Dest should return Next.Origin.");
        }

        [TestMethod]
        public void Dest_ReturnsNull_WhenNextIsNull()
        {
            var v = new Vertex(0, 0);
            var edge = new HalfEdge(v);

            Assert.IsNull(edge.Dest, "Dest should be null if Next is null.");
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
            var v = new Vertex(0, 0);
            var pair1 = HalfEdge.CreateHalfEdgePair(null, v);
            var pair2 = HalfEdge.CreateHalfEdgePair(v, null);
        }

        [TestMethod]
        public void CreateHalfEdgePair_SetsTwinCorrectly()
        {
            var v1 = new Vertex(0, 0);
            var v2 = new Vertex(1, 0);

            var (e1, e2) = HalfEdge.CreateHalfEdgePair(v1, v2);

            Assert.AreEqual(e2, e1.Twin, "e1.Twin should point to e2.");
            Assert.AreEqual(e1, e2.Twin, "e2.Twin should point to e1.");
        }

        [TestMethod]
        public void CreateHalfEdgePair_SetsOriginsCorrectly()
        {
            var v1 = new Vertex(0, 0);
            var v2 = new Vertex(1, 0);

            var (e1, e2) = HalfEdge.CreateHalfEdgePair(v1, v2);

            Assert.AreEqual(v1, e1.Origin, "First half-edge should have v1 as origin.");
            Assert.AreEqual(v2, e2.Origin, "Second half-edge should have v2 as origin.");
        }

        [TestMethod]
        public void ToString_ReturnsFormattedString()
        {
            // Arrange
            var origin = new Vertex(1.234567f, 2.345678f);
            var dest = new Vertex(3.456789f, 4.567890f);
            var edge = new HalfEdge(origin);
            edge.Next = new HalfEdge(dest);

            // Act
            string str = edge.ToString(); // Expected: "Vertex(x1, y1) -> Vertex(x2, y2)"

            // Parse back from string
            var parts = str.Replace("Vertex(", "").Replace(")", "").Split(new[] { " -> " }, StringSplitOptions.None);

            var originParts = parts[0].Split(',');
            var destParts = parts[1].Split(',');

            var parsedOrigin = new Vertex(
                float.Parse(originParts[0], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(originParts[1], System.Globalization.CultureInfo.InvariantCulture));

            var parsedDest = new Vertex(
                float.Parse(destParts[0], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(destParts[1], System.Globalization.CultureInfo.InvariantCulture));

            // Assert
            Assert.IsTrue(origin.PositionsEqual(parsedOrigin), "Origin vertex round-trip failed.");
            Assert.IsTrue(dest.PositionsEqual(parsedDest), "Destination vertex round-trip failed.");
        }
    }
}
