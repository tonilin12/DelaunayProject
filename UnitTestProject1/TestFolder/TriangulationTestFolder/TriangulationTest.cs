using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WindowsFormsApp1.myitem.GeometryFolder; // Adjust namespace
using MathNet.Numerics.Random;
using MathNet.Numerics.Distributions;

namespace UnitTestProject1.TestFolder.TriangulationFolder
{
    [TestClass]
    public class TriangulationTest
    {
        private Vertex[] vertexArray;
        private TriangulationBuilder triangulator;
        private Face superTriangle;

        private static float Clamp(float value, float min, float max) =>
            (float)Math.Max(min, Math.Min(value, max));

        [TestInitialize]
        public void SetupVertices()
        {
            // Use Mersenne Twister RNG for reproducibility
            var rng = new MersenneTwister(12345);

            // Define the containing rectangle
            float rectMinX = 0;
            float rectMaxX = 1000;
            float rectMinY = 0;
            float rectMaxY = 800;

            // Dense clusters
            int numClusters = 5;
            int pointsPerCluster = 200;
            float clusterRadius = 15f;

            var randomVertices = new List<Vertex>();

            for (int c = 0; c < numClusters; c++)
            {
                // Random cluster center inside the rectangle
                double centerX = ContinuousUniform.Sample(rng, rectMinX, rectMaxX);
                double centerY = ContinuousUniform.Sample(rng, rectMinY, rectMaxY);
                var center = new Vector2((float)centerX, (float)centerY);

                for (int i = 0; i < pointsPerCluster; i++)
                {
                    // Offset within cluster radius using uniform distribution
                    double offsetX = ContinuousUniform.Sample(rng, -clusterRadius, clusterRadius);
                    double offsetY = ContinuousUniform.Sample(rng, -clusterRadius, clusterRadius);

                    var vertexPos = center + new Vector2((float)offsetX, (float)offsetY);

                    // Clip vertex to rectangle bounds
                    vertexPos.X = Clamp(vertexPos.X, rectMinX, rectMaxX);
                    vertexPos.Y = Clamp(vertexPos.Y, rectMinY, rectMaxY);

                    randomVertices.Add(new Vertex(vertexPos));
                }
            }

            // Remove exact duplicates
            vertexArray = randomVertices
                .GroupBy(v => v.Position)
                .Select(g => g.First())
                .ToArray();

            // Initialize super-triangle and triangulator
            TriangulationOperation.getSuperTriangle(ref vertexArray, out superTriangle);
            triangulator = new TriangulationBuilder(superTriangle);
        }

        [TestMethod]
        public void RandomDenseRectangle_ShouldInitializeVertices()
        {
            Assert.IsNotNull(vertexArray, "Vertex array should not be null.");
            Assert.IsTrue(vertexArray.Length > 0, "Vertex array should contain vertices.");
        }

        [TestMethod]
        public void TriangulationBuilder_ShouldBuildValidTriangulation()
        {
            foreach (var v in vertexArray)
            {
                triangulator.AddVertices(v);
                triangulator.ProcessSingleVertex();
            }

            var triangles = triangulator.GetInternalTriangles();

            Assert.IsNotNull(triangles, "Triangulation result should not be null.");
            Assert.IsTrue(triangles.Count > 0, "There should be at least one triangle.");

            // All vertices must be part of some triangle
            var verticesInTriangles = new HashSet<Vertex>();
            foreach (var face in triangles)
                foreach (var v in face.GetVertices())
                    verticesInTriangles.Add(v);

            foreach (var v in vertexArray)
                Assert.IsTrue(verticesInTriangles.Contains(v), $"Vertex {v.Position} not included in any triangle.");

            // Validate triangles: no duplicate vertices, positive area
            foreach (var face in triangles)
            {
                var verts = face.GetVertices().ToList();
                Assert.AreEqual(3, verts.Distinct().Count(), "Triangle has duplicate vertices.");
                float area = GeometryUtils.GetSignedArea(verts[0], verts[1], verts[2]);
                Assert.IsTrue(area > 0, "Triangle has zero or negative area.");
            }
        }
    }
}
