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
            int orientation = GeometryUtils.TriangleOrientation(vA, vB, vC);
            Assert.AreEqual(1, orientation, "Triangle should be counterclockwise (CCW).");
        }

        [TestMethod]
        public void TriangleOrientation_CW_ReturnsMinus1()
        {
            int orientation = GeometryUtils.TriangleOrientation(vA, vC, vB);
            Assert.AreEqual(-1, orientation, "Triangle should be clockwise (CW).");
        }

        [TestMethod]
        public void TriangleOrientation_Collinear_Returns0()
        {
            Vertex v1 = new Vertex(new Vector2(0, 0));
            Vertex v2 = new Vertex(new Vector2(1, 1));
            Vertex v3 = new Vertex(new Vector2(2, 2));

            int orientation = GeometryUtils.TriangleOrientation(v1, v2, v3);
            Assert.AreEqual(0, orientation, "Triangle is collinear, should return 0.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TriangleOrientation_InvalidCount_ThrowsException()
        {
            GeometryUtils.TriangleOrientation(vA, vB); // only 2 vertices
        }

        #endregion

        #region IsInsideCircumcircle Tests

        [TestMethod]
        public void IsInsideCircumcircle_PointInside_ReturnsTrue()
        {
            bool result = GeometryUtils.IsInsideCircumcircle(triangle, vInside);
            Assert.IsTrue(result, "Point inside circumcircle should return true.");
        }

        [TestMethod]
        public void IsInsideCircumcircle_PointOutside_ReturnsFalse()
        {
            bool result = GeometryUtils.IsInsideCircumcircle(triangle, vOutside);
            Assert.IsFalse(result, "Point outside circumcircle should return false.");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IsInsideCircumcircle_NullTriangle_ThrowsException()
        {
            GeometryUtils.IsInsideCircumcircle(null, vInside);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void IsInsideCircumcircle_NullVertex_ThrowsException()
        {
            GeometryUtils.IsInsideCircumcircle(triangle, null);
        }

        #endregion
    }

}
