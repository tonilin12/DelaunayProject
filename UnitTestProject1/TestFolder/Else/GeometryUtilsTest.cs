using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Numerics;
using WindowsFormsApp1.myitem.GeometryFolder;

namespace UnitTestProject1.TestFolder.Else
{


    [TestClass]
    public class GeometryUtilsTests
    {
        private Vertex vA;
        private Vertex vB;
        private Vertex vC;
        private Vertex vInside;
        private Vertex vOutside;

        private Face triangle;

        [TestInitialize]
        public void Setup()
        {
            // Triangle vertices
            vA = new Vertex(new Vector2(0, 0));
            vB = new Vertex(new Vector2(1, 0));
            vC = new Vertex(new Vector2(0, 1));

            // Create triangle face
            triangle = new Face(vA, vB, vC);

            // Points to test inside/outside circumcircle
            vInside = new Vertex(new Vector2(0.25f, 0.25f));
            vOutside = new Vertex(new Vector2(2f, 2f));
        }

        #region TriangleOrientation Tests

        [TestMethod]
        public void TriangleOrientation_CCW_Returns1()
        {
            var orientation = GeometryUtils.GetSignedArea(vA, vB, vC);
            Assert.AreEqual(1, orientation, "Triangle should be counterclockwise (CCW).");
        }

        [TestMethod]
        public void TriangleOrientation_CW_ReturnsMinus1()
        {
            var orientation = GeometryUtils.GetSignedArea(vA, vC, vB);
            Assert.AreEqual(-1, orientation, "Triangle should be clockwise (CW).");
        }

        [TestMethod]
        public void TriangleOrientation_Collinear_Returns0()
        {
            Vertex v1 = new Vertex(new Vector2(0, 0));
            Vertex v2 = new Vertex(new Vector2(1, 1));
            Vertex v3 = new Vertex(new Vector2(2, 2));

            var orientation = GeometryUtils.GetSignedArea(v1, v2, v3);
            Assert.AreEqual(0, orientation, "Triangle is collinear, should return 0.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TriangleOrientation_InvalidCount_ThrowsException()
        {
            GeometryUtils.GetSignedArea(vA, vB); // only 2 vertices
        }

        #endregion

        #region IsInsideCircumcircle Tests

        [TestMethod]
        public void IsInsideOrOnCircumcircle_PointInside_ReturnsTrue()
        {
            bool result = GeometryUtils.IsInsideOrOnCircumcircle(triangle, vInside);
            Assert.IsTrue(result, "Point inside circumcircle should return true.");
        }

        [TestMethod]
        public void IsInsideOrOnCircumcircle_PointOutside_ReturnsFalse()
        {
            bool result = GeometryUtils.IsInsideOrOnCircumcircle(triangle, vOutside);
            Assert.IsFalse(result, "Point outside circumcircle should return false.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IsInsideOrOnCircumcircle_NullTriangle_ThrowsException()
        {
            GeometryUtils.IsInsideOrOnCircumcircle(null, vInside);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IsInsideOrOnCircumcircle_NullVertex_ThrowsException()
        {
            GeometryUtils.IsInsideOrOnCircumcircle(triangle, null);
        }
        #endregion



        [TestMethod]
        public void Test_IsPointInsideTriangle_PointInside_ReturnsTrue()
        {
            var a = new Vertex(new Vector2(0, 0));
            var b = new Vertex(new Vector2(5, 0));
            var c = new Vertex(new Vector2(2.5f, 5));
            var face = new Face(a, b, c);

            var pointInside = new Vector2(2.5f, 2);

            bool result = GeometryUtils.IsPointInsideTriangle(face, pointInside);

            Assert.IsTrue(result, "Point inside triangle should return true.");
        }

        [TestMethod]
        public void Test_IsPointInsideTriangle_PointOutside_ReturnsFalse()
        {
            var a = new Vertex(new Vector2(0, 0));
            var b = new Vertex(new Vector2(5, 0));
            var c = new Vertex(new Vector2(2.5f, 5));
            var face = new Face(a, b, c);

            var pointOutside = new Vector2(5, 5);

            bool result = GeometryUtils.IsPointInsideTriangle(face, pointOutside);

            Assert.IsFalse(result, "Point outside triangle should return false.");
        }

        [TestMethod]
        public void Test_IsPointInsideTriangle_PointOnEdge_ReturnsTrue()
        {
            var a = new Vertex(new Vector2(0, 0));
            var b = new Vertex(new Vector2(5, 0));
            var c = new Vertex(new Vector2(2.5f, 5));
            var face = new Face(a, b, c);

            var pointOnEdge = new Vector2(2.5f, 0);

            bool result = GeometryUtils.IsPointInsideTriangle(face, pointOnEdge);

            Assert.IsTrue(result, "Point on triangle edge should be considered inside.");
        }

        [TestMethod]
        public void Test_Circumcircle_ComputesCorrectCenterAndRadius()
        {
            var a = new Vertex(new Vector2(0, 0));
            var b = new Vertex(new Vector2(4, 0));
            var c = new Vertex(new Vector2(0, 3));

            GeometryUtils.Circumcircle(a, b, c, out Vector2 center, out float radius);

            // The circumcenter of this right triangle is at (2,1.5) and radius = sqrt(2^2 + 1.5^2) = 2.5
            var expectedCenter = new Vector2(2, 1.5f);
            var expectedRadius = 2.5f;

            Assert.AreEqual(expectedCenter.X, center.X, 0.0001f, "Circumcircle center X mismatch.");
            Assert.AreEqual(expectedCenter.Y, center.Y, 0.0001f, "Circumcircle center Y mismatch.");
            Assert.AreEqual(expectedRadius, radius, 0.0001f, "Circumcircle radius mismatch.");
        }

        [TestMethod]
        public void Test_Circumcircle_RadiusMatchesDistanceToAllVertices()
        {
            var a = new Vertex(new Vector2(1, 1));
            var b = new Vertex(new Vector2(4, 5));
            var c = new Vertex(new Vector2(6, 2));

            GeometryUtils.Circumcircle(a, b, c, out Vector2 center, out float radius);

            float distA = Vector2.Distance(center, a.Position);
            float distB = Vector2.Distance(center, b.Position);
            float distC = Vector2.Distance(center, c.Position);

            // All distances should equal the computed radius
            Assert.AreEqual(radius, distA, 0.0001f);
            Assert.AreEqual(radius, distB, 0.0001f);
            Assert.AreEqual(radius, distC, 0.0001f);
        }
    }

}
