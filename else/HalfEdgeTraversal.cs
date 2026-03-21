using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class HalfEdgeTraversal
{
    /// <summary>
    /// Lazily enumerates all faces in a half-edge mesh starting from a given half-edge.
    /// Each face is yielded exactly once.
    /// </summary>
    /// <param name="initialHalfEdge">A half-edge in the mesh to start traversal from.</param>
    public static IEnumerable<Face> EnumerateFaces(HalfEdge initialHalfEdge)
    {
        if (initialHalfEdge == null) yield break;

        var seenFaces = new HashSet<Face>();
        var toVisit = new Queue<HalfEdge>();
        toVisit.Enqueue(initialHalfEdge);

        while (toVisit.Count > 0)
        {
            var currentEdge = toVisit.Dequeue();
            var currentFace = currentEdge.Face;

            if (currentFace == null || seenFaces.Contains(currentFace))
                continue;

            seenFaces.Add(currentFace);
            yield return currentFace;

            // Enqueue neighboring faces via twin edges
            foreach (var edge in currentFace.GetEdges())
            {
                var twinFace = edge.Twin?.Face;
                if (twinFace != null && !seenFaces.Contains(twinFace))
                    toVisit.Enqueue(edge.Twin);
            }
        }
    }
}
