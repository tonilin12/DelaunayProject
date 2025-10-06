using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Numerics;

namespace TestProject1.TestFolder.TriangulationOperations
{
    [TestClass]
    public class SuperTriangleGeneratorTests
    {
        private static Random _random = new Random();

        [TestMethod]
        public void TestSuperTriangleGeneration_WithRandomVertices()
        {
            // Arrange: create a random set of vertices
            int vertexCount = _random.Next(4, 10); // between 4 and 10 vertices
            Vertex[] vertices = new Vertex[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                float x = (float)_random.NextDouble() * 500; // range 0–500
                float y = (float)_random.NextDouble() * 500; // range 0–500
                vertices[i] = new Vertex(x, y);
            }

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

            // Check that the supertriangle roughly encloses all input vertices
            float minX = verts.Min(v => v.Position.X);
            float maxX = verts.Max(v => v.Position.X);
            float minY = verts.Min(v => v.Position.Y);
            float maxY = verts.Max(v => v.Position.Y);

            foreach (var v in vertices)
            {
                Assert.IsTrue(v.Position.X >= minX && v.Position.X <= maxX,
                    $"Vertex X {v.Position.X} should be within superTriangle bounds ({minX}-{maxX})");
                Assert.IsTrue(v.Position.Y >= minY && v.Position.Y <= maxY,
                    $"Vertex Y {v.Position.Y} should be within superTriangle bounds ({minY}-{maxY})");
            }

            foreach (var edge in superTriangle.GetEdges())
            {
                if (edge.Twin != null)
                {
                    Assert.Fail($"SuperTriangle edge twin should be null.\n" +
                                $"Edge: {edge}\n" +
                                $"Twin: {edge.Twin}\n" +
                                $"Face: {edge.Face}\n" +
                                $"Twin.Face: {edge.Twin.Face}");
                }
            }

        }
    }
}
