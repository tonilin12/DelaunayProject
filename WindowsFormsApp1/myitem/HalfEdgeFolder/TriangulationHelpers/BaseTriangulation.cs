using System;
using System.Collections.Generic;
using System.Linq;

public class BaseTriangulationClass
{
    public HashSet<Face> getTriangulation(List<Vertex> points, Face supertriangle)
    {
        var triangles = new HashSet<Face> { supertriangle };
        Face currentFace = supertriangle;

        foreach (var p in points)
        {
            currentFace = InsertPoint(p, triangles, currentFace);
        }

        return triangles;
    }

    private Face InsertPoint(Vertex p, HashSet<Face> triangles, Face currentFace)
    {
        try
        {
            // Find the containing face
            var findpointData = PointEdgeLocator.LocatePointInMesh(currentFace.Edge, p);
            var isOnEdge = findpointData.isOnEdge;

            var t0 = findpointData.destinationEdge.Face;

            var searched_edge=findpointData.destinationEdge;
            List<Face>newTriangles;
            if (isOnEdge)
            {
                newTriangles=TriangulationOperation
                            .SplitTriangleWithEdge(searched_edge,p);

                triangles.Remove(searched_edge.Face);
                triangles.Remove(searched_edge.Twin.Face);
            }
            else
            {
                newTriangles = TriangulationOperation.SplitTriangle(t0, p);
                triangles.Remove(t0);
            }            

            // Add new triangles to the HashSet one by one
            foreach (var newTriangle in newTriangles)
            {
                triangles.Add(newTriangle);  // Add each new triangle to the set
            }

       

            // Legalize edges
            LegalizeEdges(new Stack<Face>(newTriangles), p, triangles);

            return newTriangles[0];  // Return the first new triangle for the next search
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error inserting point {p}: {e}");
            return currentFace;
        }
    }

    private void LegalizeEdges(Stack<Face> stack, Vertex p, HashSet<Face> triangles)
    {
        int iterationLimit = 1000;
        int iterationCount = 0;

        while (stack.Count > 0 && iterationCount++ < iterationLimit)
        {
            var triangle = stack.Pop();
            if (triangle == null) continue;

            var opposite_twin = triangle.GetOppositeTwinEdge(p);
            if (opposite_twin != null && TriangulationOperation.InCircle(opposite_twin.Face, p))
            {
                // Flip edge and update triangle relationships
                TriangulationOperation.FlipEdge(ref opposite_twin);

                // Add flipped triangles back to stack for further checking
                stack.Push(triangle);
                stack.Push(opposite_twin.Face);
            }
        }
    }

    private static void HandleEdgeCase(Vertex p, HashSet<Face> triangles, Face edgeFace)
    {
        // Implementation for edge cases (e.g., edge splitting)
        Console.WriteLine("Edge case handling not implemented");
    }
}