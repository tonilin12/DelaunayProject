using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsFormsApp1; // Fixed namespace


namespace UnitTestProject1
{
    [TestClass]
    public class VertexTests
    {
        private const float Tolerance = Vertex.Tolerance;

        [TestMethod]
        public void Constructor_ShouldInitializePosition_AndOutgoingHalfEdgeIsNull()
        {
            var position = new Vector2(1.5f, -2.5f);
            var vertex = new Vertex(position);

            Assert.AreEqual(position.X, vertex.Position.X, Tolerance);
            Assert.AreEqual(position.Y, vertex.Position.Y, Tolerance);
            Assert.IsNull(vertex.OutgoingHalfEdge);
        }

        [TestMethod]
        public void ToString_ShouldReturnFormattedStringWithTwoDecimals()
        {
            var vertex = new Vertex(new Vector2(1.2345f, 2.3456f));
            Assert.AreEqual("Vertex(1.23, 2.35)", vertex.ToString());
        }

        [TestMethod]
        public void Equals_SameReference_ShouldReturnTrue()
        {
            var vertex = new Vertex(new Vector2(0f, 0f));
            Assert.IsTrue(vertex.Equals(vertex));
        }

        [TestMethod]
        public void Equals_Null_ShouldReturnFalse()
        {
            var vertex = new Vertex(new Vector2(0f, 0f));
            Assert.IsFalse(vertex.Equals((Vertex)null));
            Assert.IsFalse(vertex.Equals((object)null));
        }

        [TestMethod]
        public void Equals_VerticesWithinTolerance_ShouldReturnTrue()
        {
            var v1 = new Vertex(new Vector2(3.000010f, 4.000010f));
            var v2 = new Vertex(new Vector2(3.000012f, 4.000012f));

            Assert.IsTrue(v1.Equals(v2));
            Assert.IsTrue(v2.Equals(v1));
        }

        [TestMethod]
        public void Equals_VerticesOutsideTolerance_ShouldReturnFalse()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 1f));

            Assert.IsFalse(v1.Equals(v2));
        }

        [TestMethod]
        public void GetHashCode_VerticesWithinTolerance_ShouldBeEqual()
        {
            var v1 = new Vertex(new Vector2(2.000010f, 3.000010f));
            var v2 = new Vertex(new Vector2(2.000012f, 3.000012f));

            Assert.AreEqual(v1.GetHashCode(), v2.GetHashCode());
        }

        [TestMethod]
        public void HashSet_ShouldTreatVerticesWithinToleranceAsEqual()
        {
            var v1 = new Vertex(new Vector2(1.000010f, 1.000010f));
            var v2 = new Vertex(new Vector2(1.000012f, 1.000012f));

            var set = new System.Collections.Generic.HashSet<Vertex>();
            set.Add(v1);
            set.Add(v2);

            Assert.AreEqual(1, set.Count);
            Assert.IsTrue(set.Contains(v1));
            Assert.IsTrue(set.Contains(v2));
        }

        [TestMethod]
        public void OutgoingHalfEdge_CanBeAssignedAndRetrieved()
        {
            var vertex = new Vertex(new Vector2(0f, 0f));
            var edge1 = new HalfEdge(vertex);
            var edge2 = new HalfEdge(vertex);

            vertex.OutgoingHalfEdge = edge1;
            Assert.AreEqual(edge1, vertex.OutgoingHalfEdge);

            vertex.OutgoingHalfEdge = edge2;
            Assert.AreEqual(edge2, vertex.OutgoingHalfEdge);
        }
    }
}
