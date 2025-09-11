using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsFormsApp1; // Replace with your namespace

namespace UnitTestProject1
{
    [TestClass]
    public class VertexTests
    {
        [TestMethod]
        public void Constructor_SetsPositionAndOutgoingNull()
        {
            var pos = new Vector2(1.0f, 2.0f);
            var vertex = new Vertex(pos);

            Assert.AreEqual(pos, vertex.Position, "Position should match constructor argument.");
            Assert.IsNull(vertex.OutgoingHalfEdge, "OutgoingHalfEdge should be null by default.");
        }

        [TestMethod]
        public void Equals_SameObject_ReturnsTrue()
        {
            var vertex = new Vertex(new Vector2(1.0f, 2.0f));
            Assert.IsTrue(vertex.Equals(vertex), "Equals should return true for the same object reference.");
        }

        [TestMethod]
        public void Equals_NullObject_ReturnsFalse()
        {
            var vertex = new Vertex(new Vector2(1.0f, 2.0f));
            Assert.IsFalse(vertex.Equals(null), "Equals should return false when compared to null.");
        }

        [TestMethod]
        public void Equals_SamePosition_ReturnsTrue()
        {
            var v1 = new Vertex(new Vector2(1.0f, 2.0f));
            var v2 = new Vertex(new Vector2(1.0f, 2.0f));

            Assert.IsTrue(v1.Equals(v2), "Vertices with same position should be equal.");
            Assert.IsTrue(v2.Equals(v1), "Equality should be symmetric.");
            Assert.IsTrue(v1.Equals((object)v2), "Equals(object) should work as well.");
        }

        [TestMethod]
        public void Equals_ClosePositionWithinTolerance_ReturnsTrue()
        {
            var v1 = new Vertex(new Vector2(1.000001f, 2.000001f));
            var v2 = new Vertex(new Vector2(1.000002f, 2.000002f));

            Assert.IsTrue(v1.Equals(v2), "Vertices within tolerance should be considered equal.");
        }

        [TestMethod]
        public void Equals_DifferentPosition_ReturnsFalse()
        {
            var v1 = new Vertex(new Vector2(1.0f, 2.0f));
            var v2 = new Vertex(new Vector2(1.0f + 0.0001f, 2.0f)); // outside tolerance

            Assert.IsFalse(v1.Equals(v2), "Vertices outside tolerance should not be equal.");
        }

        [TestMethod]
        public void ToString_ReturnsFormattedString()
        {
            var vertex = new Vertex(new Vector2(1.23456f, 7.89012f));
            string s = vertex.ToString();

            StringAssert.Contains(s, "1.23", "ToString should format X with 2 decimal places.");
            StringAssert.Contains(s, "7.89", "ToString should format Y with 2 decimal places.");
        }
    }
}
