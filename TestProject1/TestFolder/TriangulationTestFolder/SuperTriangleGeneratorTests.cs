using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.TestFolder.TriangulationOperations
{

    [TestClass]
    public class SuperTriangleGeneratorTests
    {
        [TestMethod]
        public void TestSuperTriangleGeneration()
        {
            // Arrange: create some sample vertices as an array
            Vertex[] vertices = new Vertex[]
            {
                new Vertex(new Vector2(100, 100)),
                new Vertex(new Vector2(200, 300)),
                new Vertex(new Vector2(400, 150)),
                new Vertex(new Vector2(50, 250))
            };

            // Create the supertriangle generator
            SuperTriangleGenerator generator = new SuperTriangleGenerator();

            // Act: generate supertriangle
            generator.GetSuperTriangle(vertices, out Face superTriangle);

            // Assert: superTriangle is not null
            Assert.IsNotNull(superTriangle, "SuperTriangle should not be null.");

            // Assert: superTriangle has 3 distinct vertices
            var verts = superTriangle.GetVertices().ToList();
            Assert.AreEqual(3, verts.Count, "SuperTriangle should have 3 vertices.");
            Assert.AreNotEqual(verts[0], verts[1]);
            Assert.AreNotEqual(verts[1], verts[2]);
            Assert.AreNotEqual(verts[0], verts[2]);

            // Optional: check that superTriangle roughly encloses all input vertices
            float minX = Math.Min(Math.Min(verts[0].Position.X, verts[1].Position.X), verts[2].Position.X);
            float maxX = Math.Max(Math.Max(verts[0].Position.X, verts[1].Position.X), verts[2].Position.X);
            float minY = Math.Min(Math.Min(verts[0].Position.Y, verts[1].Position.Y), verts[2].Position.Y);
            float maxY = Math.Max(Math.Max(verts[0].Position.Y, verts[1].Position.Y), verts[2].Position.Y);

            foreach (var v in vertices)
            {
                Assert.IsTrue(v.Position.X >= minX && v.Position.X <= maxX,
                    $"Vertex X {v.Position.X} should be within superTriangle bounds ({minX}-{maxX})");
                Assert.IsTrue(v.Position.Y >= minY && v.Position.Y <= maxY,
                    $"Vertex Y {v.Position.Y} should be within superTriangle bounds ({minY}-{maxY})");
            }
        }
    }

}
