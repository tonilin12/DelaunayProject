using ClassLibrary2.MeshFolder.Else;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TestProject1.TestFolder.TriangulationOperations
{
    [TestClass]
    public class TriangleSplitterTest
    {
        private readonly TriangleSplitter splitter = new TriangleSplitter();

        // ==================== Tests ====================

        [TestMethod]
        public void SplitTriangle_PointInsideFace()
        {
            var vA = new Vertex(0, 0);
            var vB = new Vertex(1, 0);
            var vC = new Vertex(0, 1);
            var face = new Face(vA, vB, vC);

            var inserted = new Vertex(0.3f, 0.3f);

            var ring = face.GetEdges().ToList();
            var snap = Snapshot(ring);

            splitter.SplitTriangle(face, inserted);

            AssertFan(ring, inserted, snap, "Inside");
        }

        [TestMethod]
        public void SplitTriangle_PointOnEdge()
        {
            var vA = new Vertex(0.25f, 0f);
            var vB = new Vertex(0.75f, 0f);
            var vC = new Vertex(0.5f, 0.5f);
            var vD = new Vertex(0.5f, -0.5f);

            var f1 = new Face(vA, vB, vC);
            var f2 = new Face(vB, vA, vD);
            f1.Edge!.Twin = f2.Edge;
            f2.Edge!.Twin = f1.Edge;

            var splitEdge = f1.Edge!;
            var inserted = new Vertex((vA.Position.X + vB.Position.X) / 2f, (vA.Position.Y + vB.Position.Y) / 2f);

            var ring = new List<HalfEdge>
            {
                splitEdge.Next!, splitEdge.Next!.Next!,
                splitEdge.Twin!.Next!, splitEdge.Twin!.Next!.Next!
            };
            var snap = Snapshot(ring);

            splitter.SplitTriangle_VertexOnEdge(splitEdge, inserted);

            AssertFan(ring, inserted, snap, "OnEdge");
        }

        // ==================== Snapshot ====================

        private static IReadOnlyList<(Vector2 OriginPos, HalfEdge TwinRef)> Snapshot(IReadOnlyList<HalfEdge> edges)
        {
            foreach (var e in edges)
            {
                if (e.Twin == null)
                {
                    var dummy = new HalfEdge(e.Dest!);
                    dummy.Twin = e;
                    e.Twin = dummy;
                }
            }

            return edges.Select(e => (e.Origin!.Position, e.Twin!)).ToList();
        }

        // ==================== Assertions ====================

        private static void AssertFan(IReadOnlyList<HalfEdge> ring, Vertex inserted, IReadOnlyList<(Vector2, HalfEdge)> snap, string tag)
        {
            Assert.AreEqual(ring.Count, snap.Count, $"[{tag}] snapshot size mismatch.");

            var insertedOut = inserted.OutgoingHalfEdge;
            Assert.IsNotNull(insertedOut, $"[{tag}] inserted.OutgoingHalfEdge is null.");
            Assert.AreSame(inserted, insertedOut!.Origin, $"[{tag}] inserted.OutgoingHalfEdge.Origin != inserted.");

            for (int i = 0; i < ring.Count; i++)
            {
                var e = ring[i];
                var nextOrig = ring[(i + 1) % ring.Count];

                var e1 = e.Next;
                Assert.IsNotNull(e1, $"[{tag}|{i}] e.Next null.");
                Assert.AreSame(inserted, e1!.Dest, $"[{tag}|{i}] e1.Dest != inserted.");
                Assert.IsNotNull(e1.Twin, $"[{tag}|{i}] e1.Twin null.");
                Assert.AreSame(nextOrig, e1.Twin!.Next, $"[{tag}|{i}] e1.Twin.Next != next original.");

                var e2 = e1.Next;
                Assert.IsNotNull(e2, $"[{tag}|{i}] e1.Next null.");
                Assert.AreSame(inserted, e2!.Origin, $"[{tag}|{i}] e2.Origin != inserted.");
                Assert.AreSame(e.Origin, e2.Dest, $"[{tag}|{i}] e2.Dest != e.Origin.");
            }
        }
    }
}
