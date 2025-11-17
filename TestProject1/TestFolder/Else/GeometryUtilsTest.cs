using System;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ClassLibrary2.MeshFolder.Else;

namespace TestProject1.TestFolder.Else
{
    [TestClass]
    public class GeometryUtilsTests
    {
        private Vertex vA;
        private Vertex vB;
        private Vertex vC;
        private Vertex vInside;
        private Vertex vOnCircle;
        private Vertex vOutside;

        private Face triangle;

        [TestInitialize]
        public void Setup()
        {
            // Define a right triangle at (0,0), (1,0), (0,1)
            vA = new Vertex(0, 0);
            vB = new Vertex(1, 0);
            vC = new Vertex(0, 1);

            triangle = new Face(vA, vB, vC);

            // Circumcenter is (0.5, 0.5), radius ≈ 0.7071
            vInside = new Vertex(0.4f, 0.4f);       // clearly inside
            vOnCircle = new Vertex(0.5f + 0.7071f, 0.5f); // approximately on circumcircle
            vOutside = new Vertex(1.5f, 0.5f);      // outside the circumcircle
        }

        #region Triangle Orientation Tests

        [TestMethod]
        public void TriangleOrientation_CCW_ReturnsPositive()
        {
            float area = GeometryUtils.GetSignedArea(vA, vB, vC);
            Assert.IsTrue(area > 0, "vvertecies should be counterclockwise (CCW).");
        }

        [TestMethod]
        public void TriangleOrientation_CW_ReturnsNegative()
        {
            float area = GeometryUtils.GetSignedArea(vA, vC, vB);
            Assert.IsTrue(area <-GeometryUtils.EPSILON, "vertecies should be clockwise (CW).");
        }

        [TestMethod]
        public void TriangleOrientation_Collinear_ReturnsZero()
        {
            var v1 = new Vertex(0, 0);
            var v2 = new Vertex(1, 1);
            var v3 = new Vertex(2, 2);

            float area = GeometryUtils.GetSignedArea(v1, v2, v3);
            Assert.IsTrue(Math.Abs(area)<=GeometryUtils.EPSILON, "vertecies should be collienar.");
        }

        #endregion

        #region InCircumcircle Tests

        [TestMethod]
        public void InCircumcircle_PointInside_ReturnsTrue()
        {
            bool result = GeometryUtils.InCircumcircle(triangle, vInside);
            Assert.IsTrue(result, "Point strictly inside circumcircle should return true.");
        }

        [TestMethod]
        public void InCircumcircle_PointOnCircumcircle_ReturnsFalse()
        {
            // Compute exact radius of the circumcircle for the triangle
            Vector2 center = GeometryUtils.Circumcenter(vA, vB, vC);
            float radius = Vector2.Distance(center, vA.Position);

            // Place point approximately on the circumcircle, but allow for floating-point epsilon
            vOnCircle = new Vertex(center.X + radius - GeometryUtils.EPSILON / 2f, center.Y);

            bool result = GeometryUtils.InCircumcircle(triangle, vOnCircle);

            // It should return false, because the point is considered on the circle (not inside)
            Assert.IsFalse(result,
                $"Point on circumcircle (within epsilon {GeometryUtils.EPSILON}) should return false.");
        }


        [TestMethod]
        public void InCircumcircle_PointOutside_ReturnsFalse()
        {
            bool result = GeometryUtils.InCircumcircle(triangle, vOutside);
            Assert.IsFalse(result, "Point outside circumcircle should return false.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InCircumcircle_NullTriangle_ThrowsException()
        {
            GeometryUtils.InCircumcircle(null, vInside);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InCircumcircle_NullVertex_ThrowsException()
        {
            GeometryUtils.InCircumcircle(triangle, null);
        }

        #endregion



        #region Circumcenter Tests

        [TestMethod]
        public void Circumcenter_RightTriangle_ComputesCorrectCenter()
        {
            var a = new Vertex(0, 0);
            var b = new Vertex(4, 0);
            var c = new Vertex(0, 3);

            Vector2 center = GeometryUtils.Circumcenter(a, b, c);
            var expected = new Vector2(2, 1.5f);

            Assert.AreEqual(expected.X, center.X, 1e-4f, "Circumcenter X mismatch.");
            Assert.AreEqual(expected.Y, center.Y, 1e-4f, "Circumcenter Y mismatch.");
        }

        [TestMethod]
        public void Circumcenter_PositionEquidistantFromVertices()
        {
            var a = new Vertex(1, 1);
            var b = new Vertex(4, 5);
            var c = new Vertex(6, 2);

            Vector2 center = GeometryUtils.Circumcenter(a, b, c);

            float dA = Vector2.Distance(center, a.Position);
            float dB = Vector2.Distance(center, b.Position);
            float dC = Vector2.Distance(center, c.Position);

            Assert.AreEqual(dA, dB, 1e-4f, "Distance to A and B mismatch.");
            Assert.AreEqual(dA, dC, 1e-4f, "Distance to A and C mismatch.");
        }

        #endregion

        #region IsOnSegment Tests (Vector2 overload)

        [TestMethod]
        public void IsOnSegment_Vector2_MidpointCollinear_ReturnsTrue()
        {
            Vector2 a = new(0, 0);
            Vector2 b = new(10, 0);
            Vector2 p = new(5, 0);

            Assert.IsTrue(GeometryUtils.IsOnSegment(a, b, p),
                "Point at the midpoint on the segment should be on the segment.");
        }

        [TestMethod]
        public void IsOnSegment_Vector2_Endpoints_ReturnTrue()
        {
            Vector2 a = new(0, 0);
            Vector2 b = new(10, 0);

            Assert.IsTrue(GeometryUtils.IsOnSegment(a, b, a),
                "Point at endpoint A should be on the segment.");
            Assert.IsTrue(GeometryUtils.IsOnSegment(a, b, b),
                "Point at endpoint B should be on the segment.");
        }

        [TestMethod]
        public void IsOnSegment_Vector2_OutsideRange_ReturnsFalse()
        {
            Vector2 a = new(0, 0);
            Vector2 b = new(10, 0);

            Assert.IsFalse(GeometryUtils.IsOnSegment(a, b, new Vector2(-0.0001f, 0)),
                "Slightly before A should not be on the segment.");
            Assert.IsFalse(GeometryUtils.IsOnSegment(a, b, new Vector2(10.0001f, 0)),
                "Slightly after B should not be on the segment.");
        }

        #endregion



    }
}
