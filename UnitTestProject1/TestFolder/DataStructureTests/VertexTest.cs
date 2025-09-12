using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsFormsApp1; // Replace with your namespace

namespace UnitTestProject1
{
    [TestClass]
    public class VertexTests
    {
        [TestMethod]
        public void Constructor_SetsPositionAndOutgoingHalfEdge()
        {
            var pos = new Vector2(5.0f, 6.0f);
            var v = new Vertex(pos);

            Assert.AreEqual(pos, v.Position);
            Assert.IsNull(v.OutgoingHalfEdge, "OutgoingHalfEdge should be null by default.");
        }


        [TestMethod]
        public void Vertex_ToString_RoundTrip()
        {
            // Step 1: Create a vertex
            var v = new Vertex(new Vector2(1.234567f, 2.345678f));

            // Step 2: Convert vertex to string
            string str = v.ToString(); // Expected format: "Vertex(x, y)"

            // Step 3: Extract numbers from string
            var parts = str.Replace("Vertex(", "").Replace(")", "").Split(',');

            float x = float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);
            float y = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);

            // Step 4: Reconstruct vertex from parsed values
            var parsedVertex = new Vertex(new Vector2(x, y));

            // Step 5: Assert that the positions are approximately equal
            Assert.IsTrue(v.PositionsEqual(parsedVertex), "Vertex round-trip via ToString failed.");
        }




        [TestMethod]
        public void PositionsEqual_WithinTolerance_ReturnsTrue()
        {
            var v1 = new Vertex(new Vector2(1.0f, 2.0f));
            var v2 = new Vertex(new Vector2(1.000001f, 2.000001f)); // smaller than tolerance 1e-5
            Assert.IsTrue(v1.PositionsEqual(v2), "Vertices within tolerance should be considered equal.");
        }

        [TestMethod]
        public void PositionsEqual_OutsideTolerance_ReturnsFalse()
        {
            var v1 = new Vertex(new Vector2(1.0f, 2.0f));
            var v2 = new Vertex(new Vector2(1.0001f, 2.0001f)); // larger than tolerance
            Assert.IsFalse(v1.PositionsEqual(v2), "Vertices outside tolerance should be considered different.");
        }
    }

}
