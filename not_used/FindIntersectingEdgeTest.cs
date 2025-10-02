using MathNet.Numerics.Distributions;
using MathNet.Numerics.Random;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using WindowsFormsApp1.myitem.GeometryFolder;


   public static IEnumerable<HalfEdge> FindIntersectingEdges(Vertex a, Vertex b)
   {
       if (a == null || b == null)
           throw new ArgumentNullException();

       var visitedEdges = new HashSet<HalfEdge>();
       var edgesToVisit = new Queue<HalfEdge>();

       // Start with all edges from vertex 'a'
       foreach (var edge in a.EnumerateEdges(e => e.Next))
       {
           if (DoesEdgeCrossSegment(edge, a, b))
               edgesToVisit.Enqueue(edge);
       }

       const int MAX_ITER = 10000; // safety
       int iterations = 0;

       while (edgesToVisit.Count > 0)
       {
           if (++iterations > MAX_ITER)
               throw new InvalidOperationException($"Max iterations ({MAX_ITER}) reached while walking along segment.");

           var current = edgesToVisit.Dequeue();
           if (current == null || visitedEdges.Contains(current)) continue;

           visitedEdges.Add(current);

           // Stop if the segment reaches destination
           if (current.Face.GetVertices().Contains(b))
               yield break;

           if (DoesEdgeCrossSegment(current, a, b))
               yield return current;

           // enqueue neighboring edges via twins
           foreach (var edge in current.Face.GetEdges())
           {
               if (edge.Twin != null && !visitedEdges.Contains(edge.Twin))
                   edgesToVisit.Enqueue(edge.Twin);
           }
       }
   }

namespace UnitTestProject1.TestFolder.Else
{
    [TestClass]
    public class FindIntersectingEdgeTest
    {
        private Vertex[] vertexArray;
        private TriangulationBuilder triangulator;
        private Face superTriangle;


        [TestInitialize]
        public void SetupVertices()
        {
            vertexArray = new Vertex[]
{
                new Vertex(new Vector2(10, 35)),  // Far Left Dot
                new Vertex(new Vector2(25, 20)),  // Lower Dot Near Left
                new Vertex(new Vector2(40, 10)),  // Lowest Center-Left Dot
                new Vertex(new Vector2(50, 90)),  // Highest Central Dot (Peak)
                new Vertex(new Vector2(60, 15)),  // Lower Center-Right Dot
                new Vertex(new Vector2(75, 55)),  // Middle-Right Dot
                new Vertex(new Vector2(90, 35))   // Far Right Dot
            };

            TriangulationOperation.getSuperTriangle(ref vertexArray, out superTriangle);
            triangulator = new TriangulationBuilder(superTriangle, vertexArray);
            triangulator.ProcessAllVertices();
        }



        [TestMethod]
        [Timeout(5000)] // Timeout in milliseconds (5 seconds)
        public void TriangulationBuilder_ShouldFindIntersectingEdges_Safe()
        {
            // Arrange
            int lastIndex = vertexArray.Length - 1;
            Vertex startVertex = vertexArray[0];
            Vertex endVertex = vertexArray[lastIndex];

            try
            {
                // Act
                var result = MeshNavigator.FindIntersectingEdges(startVertex, endVertex);

                // Assert
                Assert.IsNotNull(result, "FindIntersectingEdges returned null.");
                Console.WriteLine($"Number of intersecting edges found: {result.Count()}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"FindIntersectingEdges threw an exception: {ex.Message}");
            }
        }


    }
}
