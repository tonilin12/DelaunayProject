using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;


namespace TestProject1.TestFolder.TriangulationOperations
{
    [TestClass]
    public class TriangleSplitterTest
    {
        TriangleSplitter splitter = new TriangleSplitter();

        [TestMethod]
        public void SplitTriangle_PointInsideFace()
        {
            // Step 1: Create original triangle
            var vA = new Vertex(0, 0);
            var vB = new Vertex(1, 0);
            var vC = new Vertex(0, 1);
            var face = new Face(vA, vB, vC);


            // Step 2: Add new vertex inside the triangle
            var vD = new Vertex(0.3f, 0.3f);



            // Step 1: Create the HashSet of tuples representing expected edges
            var expectedEdges = 
            new HashSet<(Vertex Origin, Vertex Dest, HalfEdge Twin)>(
                face.GetEdges().Select(e => (e.Origin, e.Dest, e.Twin))
            );


            var original_edges = new HashSet<HalfEdge>(face.GetEdges());

            splitter.SplitTriangle(face, vD);



            vD.GetVertexEdges()                 // returns IEnumerable<HalfEdge>
              .Select(e =>
              {
                  original_edges.Remove(e.Next); // now works based on Origin->Dest equality
                  return e;
              })
              .ToList();


            Assert.AreEqual(0, original_edges.Count);

        }

        [TestMethod]
        public void SplitTriangle_PointOnEdge()
        {

            // Shared middle edge
            var vA = new Vertex(0.25f, 0f);  // left point of middle edge
            var vB = new Vertex(0.75f, 0f);  // right point of middle edge

            // One vertex above and one below, forming a convex quad
            var vC = new Vertex(0.5f, 0.5f);   // top vertex
            var vD = new Vertex(0.5f, -0.5f);  // bottom vertex



            // Faces
            var face1 = new Face(vA, vB, vC);
            var face2 = new Face(vB, vA, vD);

            // Link twin edges
            face1.Edge.Twin = face2.Edge;
            face2.Edge.Twin = face1.Edge;

            var edge = face1.Edge;


            var vE = new Vertex(
                (vA.Position.X + vB.Position.X) / 2f,
                (vA.Position.Y + vB.Position.Y) / 2f
            );


            var originalEdges = new HashSet<HalfEdge>();

            originalEdges.Add(edge.Next);
            originalEdges.Add(edge.Next.Next);
            originalEdges.Add(edge.Twin.Next);
            originalEdges.Add(edge.Twin.Next.Next);

            splitter.SplitTriangle_VertexOnEdge(edge, vE);


            vE.GetVertexEdges()
                .Select(e =>
                {
                    originalEdges.Remove(e.Next); // now works based on Origin->Dest equality
                    return e;
                })
                .ToList();

            Assert.AreEqual(0, originalEdges.Count);


        }
    }
}
