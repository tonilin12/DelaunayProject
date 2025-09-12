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


        [TestMethod]
        public void ToString_ReturnsFormattedString()
        {
            // Step 1: Create vertices
            var origin = new Vertex(new Vector2(1.234567f, 2.345678f));
            var dest = new Vertex(new Vector2(3.456789f, 4.567890f));

            // Step 2: Create a half-edge and set Next to point to destination
            var edge = new HalfEdge(origin);
            edge.Next = new HalfEdge(dest);

            // Step 3: Convert half-edge to string
            string str = edge.ToString(); // Expected: "Vertex(x1, y1) -> Vertex(x2, y2)"

            // Step 4: Extract origin and destination numbers from string
            var parts = str.Replace("Vertex(", "").Replace(")", "").Split(new[] { " -> " }, StringSplitOptions.None);

            var originParts = parts[0].Split(',');
            var destParts = parts[1].Split(',');

            // Step 5: Parse numbers to create new vertices
            var parsedOrigin = new Vertex(new Vector2(
                float.Parse(originParts[0], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(originParts[1], System.Globalization.CultureInfo.InvariantCulture)));

            var parsedDest = new Vertex(new Vector2(
                float.Parse(destParts[0], System.Globalization.CultureInfo.InvariantCulture),
                float.Parse(destParts[1], System.Globalization.CultureInfo.InvariantCulture)));

            // Step 6: Assert positions are approximately equal
            Assert.IsTrue(origin.PositionsEqual(parsedOrigin), "Origin vertex round-trip failed.");
            Assert.IsTrue(dest.PositionsEqual(parsedDest), "Destination vertex round-trip failed.");
        }

    }
}
