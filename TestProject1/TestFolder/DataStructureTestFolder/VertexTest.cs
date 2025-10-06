using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject1.TestFolder.DataStructureTestFolder
{
    [TestClass]
    public class VertexTests
    {
        [TestMethod]
        public void Constructor_SetsPositionAndOutgoingHalfEdge()
        {
            var pos = new Vector2(5.0f, 6.0f);
            var v = new Vertex(pos.X, pos.Y);

            Assert.AreEqual(pos, v.Position, "Vertex position not set correctly.");
            Assert.IsNull(v.OutgoingHalfEdge, "OutgoingHalfEdge should be null by default.");
        }

        [TestMethod]
        public void Vertex_ToString_RoundTrip()
        {
            var v = new Vertex(1.234567f, 2.345678f);
            string str = v.ToString(); // e.g., "Vertex(1.234567, 2.345678)"

            var parts = str.Replace("Vertex(", "").Replace(")", "").Split(',');
            float x = float.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            float y = float.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);

            var parsedVertex = new Vertex(x, y);

            Assert.IsTrue(v.PositionsEqual(parsedVertex, 1e-6f), "Vertex round-trip via ToString failed.");
        }

        [TestMethod]
        public void PositionsEqual_WithinTolerance_ReturnsTrue()
        {
            var v1 = new Vertex(1.0f, 2.0f);
            var v2 = new Vertex(1.0000005f, 2.0000005f); // within default 1e-6 tolerance

            Assert.IsTrue(v1.PositionsEqual(v2), "Vertices within tolerance should be considered equal.");
        }

        [TestMethod]
        public void PositionsEqual_OutsideTolerance_ReturnsFalse()
        {
            var v1 = new Vertex(1.0f, 2.0f);
            var v2 = new Vertex(1.0001f, 2.0001f); // clearly outside 1e-6 tolerance

            Assert.IsFalse(v1.PositionsEqual(v2), "Vertices outside tolerance should be considered different.");
        }
    }
}
