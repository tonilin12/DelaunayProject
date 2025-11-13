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

        /// <summary>
        /// Captures original edges and their references to ensure they are unchanged after split.
        /// </summary>
        private static IReadOnlyList
        <(Vertex OriginRef, Vertex DestRef, HalfEdge EdgeRef, HalfEdge TwinRef)> 
        Snapshot(IReadOnlyList<HalfEdge> edges)
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

            return edges.Select(e => (e.Origin!, e.Dest!, e, e.Twin!)).ToList();
        }

        // ==================== Assertions ====================
        // ==================== Assertions ====================
        private static void AssertFan(
            IReadOnlyList<HalfEdge> ring,
            Vertex inserted,
            IReadOnlyList<(Vertex OriginRef, Vertex DestRef, HalfEdge EdgeRef, HalfEdge TwinRef)> snapshot,
            string tag)
        {
            static string Label(object? o)
            {
                if (o is null) return "null";
                try
                {
                    return $"{o} (hash={o.GetHashCode()})";
                }
                catch
                {
                    return o.ToString() ?? $"<object type={o.GetType().Name}>";
                }
            }

            // Ring size vs snapshot
            Assert.AreEqual(
                ring.Count, snapshot.Count,
                $"[{tag}] Ring size {ring.Count} does not match snapshot {snapshot.Count}."
            );

            var insertedOut = inserted.OutgoingHalfEdge;
            Assert.IsNotNull(
                insertedOut,
                $"[{tag}] Inserted vertex {Label(inserted)} missing OutgoingHalfEdge."
            );
            Assert.AreSame(
                inserted, insertedOut!.Origin,
                $"[{tag}] Inserted vertex OutgoingHalfEdge origin mismatch. Expected: {Label(inserted)}, Actual: {Label(insertedOut.Origin)}."
            );

            bool foundInsertedOut = false;

            for (int i = 0; i < ring.Count; i++)
            {
                var e = ring[i];
                var (originRef, destRef, edgeRef, twinRef) = snapshot[i];

                // Original references
                Assert.AreSame(originRef, e.Origin,
                    $"[{tag}|Triangle {i}] e.Origin changed. Expected: {Label(originRef)}, Actual: {Label(e.Origin)}. Edge: {Label(e)}"
                );
                Assert.AreSame(edgeRef, e,
                    $"[{tag}|Triangle {i}] Edge reference changed. Expected: {Label(edgeRef)}, Actual: {Label(e)}"
                );
                Assert.AreSame(twinRef, e.Twin,
                    $"[{tag}|Triangle {i}] Twin reference changed. Expected: {Label(twinRef)}, Actual: {Label(e.Twin)}"
                );

                // e.Next = e1
                var e1 = e.Next;
                Assert.IsNotNull(e1,
                    $"[{tag}|Triangle {i}] e.Next (e1) is null. e: {Label(e)}"
                );
                Assert.AreSame(destRef, e1!.Origin,
                    $"[{tag}|Triangle {i}] e1.Origin mismatch. Expected destRef: {Label(destRef)}, Actual: {Label(e1.Origin)}"
                );
                Assert.IsNotNull(e1.Twin,
                    $"[{tag}|Triangle {i}] e1.Twin is null. e1: {Label(e1)}"
                );
                Assert.AreSame(e1.Twin.Twin, e1,
                    $"[{tag}|Triangle {i}] e1.Twin.Twin does not reference e1. e1: {Label(e1)}, e1.Twin: {Label(e1.Twin)}"
                );
                Assert.AreSame(e1.Twin.Origin, e1.Dest,
                    $"[{tag}|Triangle {i}] e1.Twin.Origin {Label(e1.Twin.Origin)} != e1.Dest {Label(e1.Dest)}"
                );

                // e2 = e1.Next (inserted vertex)
                var e2 = e1.Next;
                Assert.IsNotNull(e2,
                    $"[{tag}|Triangle {i}] e2 (e1.Next) is null. e1: {Label(e1)}"
                );
                Assert.AreSame(inserted, e2!.Origin,
                    $"[{tag}|Triangle {i}] e2.Origin does not reference inserted vertex. Expected: {Label(inserted)}, Actual: {Label(e2.Origin)}"
                );
                Assert.IsNotNull(e2.Twin,
                    $"[{tag}|Triangle {i}] e2.Twin is null. e2: {Label(e2)}"
                );
                Assert.AreSame(e2.Twin.Twin, e2,
                    $"[{tag}|Triangle {i}] e2.Twin.Twin does not reference e2. e2: {Label(e2)}, e2.Twin: {Label(e2.Twin)}"
                );
                Assert.AreSame(e2.Twin.Origin, e2.Dest,
                    $"[{tag}|Triangle {i}] e2.Twin.Origin {Label(e2.Twin.Origin)} != e2.Dest {Label(e2.Dest)}"
                );

                // Circularity check
                var n3 = e.Next?.Next?.Next;
                Assert.IsNotNull(n3,
                    $"[{tag}|Triangle {i}] Circularity broken: e.Next.Next.Next is null. e: {Label(e)}"
                );
                Assert.AreSame(e, n3,
                    $"[{tag}|Triangle {i}] Ring circularity broken. Expected: {Label(e)}, Actual: {Label(n3)}"
                );

                // Track inserted outgoing edge
                if (e1 == insertedOut || e2 == insertedOut)
                    foundInsertedOut = true;
            }

            Assert.IsTrue(
                foundInsertedOut,
                $"[{tag}] Inserted vertex outgoing half-edge {Label(insertedOut)} not found in fan. Inserted: {Label(inserted)}"
            );
        }

    }
}
