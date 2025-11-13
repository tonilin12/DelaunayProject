using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ClassLibrary2.MeshFolder.Else;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject1.TestFolder.Else
{
    [TestClass]
    public class SpatialGridTests
    {
        private Vertex V(float x, float y) => new Vertex(x, y);

        [TestMethod]
        public void EmptyInput_ThrowsException()
        {
            Assert.ThrowsException<ArgumentException>(() => new SpatialGrid(Array.Empty<Vertex>()));
        }

        [TestMethod]
        public void SinglePoint_GridWorks()
        {
            var v = V(1, 2);
            var grid = new SpatialGrid(new[] { v });

            Assert.AreEqual(1, grid.Points.Length);
            Assert.AreEqual(v, grid.Points[0]);
            Assert.AreEqual(v.Position, grid.MinBounds);
            Assert.AreEqual(v.Position, grid.MaxBounds);
            Assert.AreEqual(1, grid.GridSize); // with 1 point → gridSize=1
        }

        [TestMethod]
        public void BoundingBox_ComputedCorrectly()
        {
            var points = new[]
            {
                V(-1, 2),
                V(3, 5),
                V(0, 0),
                V(2, -4),
            };

            var grid = new SpatialGrid(points);

            Assert.AreEqual(new Vector2(-1, -4), grid.MinBounds);
            Assert.AreEqual(new Vector2(3, 5), grid.MaxBounds);
        }

        [TestMethod]
        public void GridSize_ScalesWithPointCount()
        {
            var points = Enumerable.Range(0, 100).Select(i => V(i, i)).ToArray();
            var grid = new SpatialGrid(points);

            // sqrt(100) = 10 → GridSize should be >= 10
            Assert.IsTrue(grid.GridSize >= 10);
        }

        [TestMethod]
        public void AllPointsPreserved()
        {
            var points = new[]
            {
                V(0, 0),
                V(10, 0),
                V(0, 10),
                V(10, 10),
            };

            var grid = new SpatialGrid(points);

            // Same count
            Assert.AreEqual(points.Length, grid.Points.Length);

            // Every point in output
            foreach (var p in points)
                CollectionAssert.Contains(grid.Points, p);
        }

        [TestMethod]
        public void Points_AreSortedByBins()
        {
            // Arrange: four corners of a square
            var points = new[]
            {
                V(0, 0),   // bottom-left
                V(10, 0),  // bottom-right
                V(0, 10),  // top-left
                V(10, 10)  // top-right
            };

            var grid = new SpatialGrid(points);

            // Act
            var ordered = grid.Points;

            // Assert: first point should be bottom-left (smallest Y, then X)
            Assert.AreEqual(new Vector2(0, 0), ordered[0].Position);
        }
    }
}
