using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;


namespace TestProject1.TestFolder.DataStructureTestFolder
{
    [TestClass]
    public class VertexEnumerateTest

    {


        private Vertex vA, vB, vC;
        private HalfEdge eAB, eBA, eBC, eCB, eCA, eAC;
        private Face faceCW, faceCCW;

        [TestInitialize]
        public void Setup()
        {
            vA = new Vertex(0, 0);
            vB = new Vertex(1, 0);
            vC = new Vertex(0, 1);

            // Create half-edge pairs
            (eAB, eBA) = HalfEdge.CreateHalfEdgePair(vA, vB);
            (eBC, eCB) = HalfEdge.CreateHalfEdgePair(vB, vC);
            (eCA, eAC) = HalfEdge.CreateHalfEdgePair(vC, vA);

            // Construct faces
            faceCW = new Face(eAB, eBC, eCA);
            faceCCW = new Face( eAC,eCB,eBA);
        }



        [TestMethod]

        public void TestVertexEnumerable()
        {


            var edgesByOrigin = new Dictionary<Vertex, List<HalfEdge>>();

            foreach (var face in new[] { faceCW, faceCCW })
            {
                foreach (var e in face.GetEdges())
                {
                    if (!edgesByOrigin.ContainsKey(e.Origin))
                        edgesByOrigin[e.Origin] = new List<HalfEdge>();

                    edgesByOrigin[e.Origin].Add(e);
                }
            }

            // Now test Vertex.EnumerateEdges matches the dictionary
            foreach (var vertex in new[] { vA, vB, vC })
            {
                var expectedEdges = edgesByOrigin[vertex];

                var actualEdges = vertex.GetVertexEdges().ToList();

                CollectionAssert.AreEquivalent(expectedEdges, actualEdges,
                    $"Vertex {vertex} EnumerateEdges did not match edges from faces.");
            }


        }

    }
}
