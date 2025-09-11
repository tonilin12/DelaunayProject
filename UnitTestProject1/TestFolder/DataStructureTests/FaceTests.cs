using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace UnitTestProject1.TestFolder
{

    [TestClass]
    public class FaceTests
    {
        [TestMethod]
        public void Constructor_ShouldCreateTriangleFromVertices()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 0f));
            var v3 = new Vertex(new Vector2(0f, 1f));

            var face = new Face(v1, v2, v3);

            var edges = face.GetEdges();
            var vertices = face.GetVertices();

            Assert.AreEqual(3, edges.Count);
            Assert.AreEqual(3, vertices.Count);

            // Check edge linking
            for (int i = 0; i < 3; i++)
            {
                Assert.AreSame(edges[i].Next.Prev, edges[i]);
                Assert.AreSame(edges[i].Face, face);
            }

            // Check that each vertex has an outgoing half-edge
            Assert.AreSame(v1.OutgoingHalfEdge, edges[0]);
            Assert.AreSame(v2.OutgoingHalfEdge, edges[1]);
            Assert.AreSame(v3.OutgoingHalfEdge, edges[2]);
        }

        [TestMethod]
        public void Constructor_ShouldCreateFaceFromHalfEdges()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 0f));
            var v3 = new Vertex(new Vector2(0f, 1f));

            var e1 = new HalfEdge(v1);
            var e2 = new HalfEdge(v2);
            var e3 = new HalfEdge(v3);

            var face = new Face(e1, e2, e3);

            Assert.AreEqual(face, e1.Face);
            Assert.AreEqual(face, e2.Face);
            Assert.AreEqual(face, e3.Face);

            // Check circular linking
            Assert.AreSame(e1.Next, e2);
            Assert.AreSame(e2.Next, e3);
            Assert.AreSame(e3.Next, e1);

            Assert.AreSame(e2.Prev, e1);
            Assert.AreSame(e3.Prev, e2);
            Assert.AreSame(e1.Prev, e3);
        }

        [TestMethod]
        public void Constructor_ShouldCreatePolygonFaceFromVertexList()
        {
            var vertices = new List<Vertex>
            {
                new Vertex(new Vector2(0f,0f)),
                new Vertex(new Vector2(1f,0f)),
                new Vertex(new Vector2(1f,1f)),
                new Vertex(new Vector2(0f,1f))
            };

            var face = new Face(vertices);
            var edges = face.GetEdges();

            Assert.AreEqual(4, edges.Count);

            // Check circular linking
            for (int i = 0; i < edges.Count; i++)
            {
                var nextEdge = edges[(i + 1) % edges.Count];
                Assert.AreSame(edges[i].Next, nextEdge);
                Assert.AreSame(nextEdge.Prev, edges[i]);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(System.ArgumentException))]
        public void Constructor_ShouldThrow_WhenVerticesLessThan3()
        {
            var vertices = new List<Vertex> { new Vertex(new Vector2(0f, 0f)), new Vertex(new Vector2(1f, 0f)) };
            var face = new Face(vertices); // Should throw
        }

        [TestMethod]
        public void GetVertices_ShouldReturnAllVerticesInOrder()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 0f));
            var v3 = new Vertex(new Vector2(0f, 1f));

            var face = new Face(v1, v2, v3);
            var vertices = face.GetVertices();

            CollectionAssert.AreEqual(new List<Vertex> { v1, v2, v3 }, vertices);
        }

        [TestMethod]
        public void GetEdges_ShouldReturnAllEdges()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 0f));
            var v3 = new Vertex(new Vector2(0f, 1f));

            var face = new Face(v1, v2, v3);
            var edges = face.GetEdges();

            Assert.AreEqual(3, edges.Count);
            Assert.IsTrue(edges.All(e => e.Face == face));
        }

        [TestMethod]
        public void ProcessEdges_ShouldApplyFunctionToAllEdges()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 0f));
            var v3 = new Vertex(new Vector2(0f, 1f));

            var face = new Face(v1, v2, v3);

            var origins = face.ProcessEdges(e => e.Origin).ToList();

            CollectionAssert.AreEqual(new List<Vertex> { v1, v2, v3 }, origins);
        }

        [TestMethod]
        public void ToString_ShouldReturnVertexSequence()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 0f));
            var v3 = new Vertex(new Vector2(0f, 1f));

            var face = new Face(v1, v2, v3);

            string expected = $"{v1} → {v2} → {v3}";
            Assert.AreEqual(expected, face.ToString());
        }

        [TestMethod]
        public void GetOppositeTwinEdge_ShouldReturnCorrectTwinOrNull()
        {
            var v1 = new Vertex(new Vector2(0f, 0f));
            var v2 = new Vertex(new Vector2(1f, 0f));
            var v3 = new Vertex(new Vector2(0f, 1f));
            var v4 = new Vertex(new Vector2(1f, 1f));

            var face1 = new Face(v1, v2, v3);
            var face2 = new Face(v2, v4, v3);

            // Create twins between face1 and face2 along the shared edge v2->v3
            var sharedEdge1 = face1.GetEdges().First(e => e.Origin == v2);
            var sharedEdge2 = face2.GetEdges().First(e => e.Origin == v3);

            sharedEdge1.Twin = sharedEdge2;
            sharedEdge2.Twin = sharedEdge1;

            var result = face1.GetOppositeTwinEdge(v1);
            Assert.AreSame(sharedEdge2, result);
        }
    }

}
