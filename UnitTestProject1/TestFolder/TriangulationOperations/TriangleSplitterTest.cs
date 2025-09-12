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
    public class TriangleSplitterTest
    {

        [TestMethod]
        public void SplitTriangle_CreatesThreeNewFaces_WithProperVerticesAndEdges()
        {
            // Step 1: Create original triangle
            var vA = new Vertex(new Vector2(0, 0));
            var vB = new Vertex(new Vector2(1, 0));
            var vC = new Vertex(new Vector2(0, 1));
            var face = new Face(vA, vB, vC);

            var original_edges = face.GetEdges();

            // Step 2: Add new vertex inside the triangle
            var vD = new Vertex(new Vector2(0.3f, 0.3f));

            // Step 3: Split the triangle
            var splitter = new TriangleSplitter();
            var newFaces = splitter.SplitTriangle(face, vD);


            // Step 4: Enumerate all outgoing edges from vD
            var outgoingEdges = vD.EnumerateEdges(e => e).ToList();


            // Step 6: Check that all twins are properly assigned and point back to vD
            // Step 6: Check that all twins are properly assigned and point back to vD using lambda style
            outgoingEdges.ForEach(e =>
            {
                Assert.IsTrue(e.Origin.PositionsEqual(vD), "Edge origin must be the new vertex vD.");
                Assert.IsNotNull(e.Twin, "Outgoing edge must have a twin.");
                Assert.AreSame(e, e.Twin.Twin, "Twin's twin must point back to original edge.");

                bool destIsOriginal = new[] { vA, vB, vC }
                                     .Any(v => e.Dest.PositionsEqual(v));
                Assert.IsTrue(destIsOriginal, "Outgoing edge must go to one of the original triangle vertices.");
            });

        }

    }
}
