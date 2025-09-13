using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject1.TestFolder.TriangulationOperations
{

    [TestClass]
    class FlipTest
    {

        private Vertex vA, vB, vC, vD;
        private Face face1, face2;

        [TestInitialize]
        public void Setup()
        {
            // Shared middle edge
            vA = new Vertex(new Vector2(0.25f, 0f));  // left point of middle edge
            vB = new Vertex(new Vector2(0.75f, 0f));  // right point of middle edge

            // One vertex above and one below, forming a convex quad
            vC = new Vertex(new Vector2(0.5f, 0.5f));   // top vertex
            vD = new Vertex(new Vector2(0.5f, -0.5f));  // bottom vertex


            // Faces
            face1 = new Face(vA, vB, vC);
            face2 = new Face(vB, vA, vD);

            // Link twin edges
            face1.Edge.Twin = face2.Edge;
            face2.Edge.Twin = face1.Edge;
            // Link twin edges
            face1.Edge.Twin = face2.Edge;
            face2.Edge.Twin = face1.Edge;
        }

        public bool AreTupleValuesEqual<T>((T, T) t1, (T, T) t2)
        {
            return (t1.Item1.Equals(t2.Item1) && t1.Item2.Equals(t2.Item2)) ||
                   (t1.Item1.Equals(t2.Item2) && t1.Item2.Equals(t2.Item1));
        }


        [TestMethod]
        public void SharedEdge_AfterFlip_TwinAndPositionsCorrect()
        {
            var edge = face1.Edge;
            var twin = face2.Edge;

            TriangulationOperation.FlipEdge(ref edge);


            // Assert twin references
            Assert.AreEqual(edge.Twin, twin, "Edge.Twin should reference its twin.");
            Assert.AreEqual(twin.Twin, edge, "Twin.Twin should reference the original edge.");

            // Assert positions using PositionsEqual
            Assert.IsTrue(edge.Origin.PositionsEqual(twin.Dest), "Edge origin should match twin destination.");
            Assert.IsTrue(edge.Dest.PositionsEqual(twin.Origin), "Edge destination should match twin origin.");

        }
    }
}

