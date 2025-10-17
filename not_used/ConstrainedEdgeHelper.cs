using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;

public class ConstrainedEdgeHelper
{
    const int max_iteration=1000;
    
    public void EnforceConstraints(ref HashSet<Face> triangles, List<(Vertex, Vertex)> constraints) // Changed to value tuple
    {
        foreach (var constrain in constraints)
        {
            EnforceSingleConstraint(ref triangles, constrain, constraints);
        }
        var edge_current=PointEdgeLocator.findHalfEdgeWithEdge(triangles.First(),constraints.First().Item1,constraints.First().Item2).searchedEdge;

        var edge_list=new List<HalfEdge>(){};

        foreach (var constrain in constraints)
        {
            if (!(GeometryUtils.ArePositionsEqual(edge_current.Origin,constrain.Item1)))
            {
                edge_current=edge_current.Twin;
            }
            edge_list.Add(edge_current);
            edge_current=PointEdgeLocator.findHalfEdgeWithEdge(edge_current.Face,constrain.Item1,constrain.Item2).searchedEdge;
        
        }
        edge_list.ForEach(x=>GD.Print(x.ToString()));
        constraints.ForEach(x=>GD.Print(x.ToString()));
    }

    private void EnforceSingleConstraint(ref HashSet<Face> triangles, (Vertex, Vertex) constrain, List<(Vertex, Vertex)> allConstraints) // Changed to value tuple
    {
        var a = constrain.Item1;
        var b = constrain.Item2;

        //GD.Print("---" + constrain.ToString());

        if (ConstraintExists(triangles, a, b))
        {
            return;
        }

        var intersectingEdges = FindIntersectingEdges(triangles, a, b);
        var createdEdges = new List<HalfEdge>();

        while (intersectingEdges.Count > 0)
        {
            var currentEdge = intersectingEdges.Dequeue()!;
            if (FlipIntersectingEdge(ref triangles, ref currentEdge, constrain, intersectingEdges, createdEdges))
            {
                // Edge was flipped, no further action needed for this iteration
            }
        }

        RefineTriangulation(ref triangles, createdEdges, allConstraints);
    }

    private bool ConstraintExists(HashSet<Face> triangles, Vertex a, Vertex b)
    {
        return PointEdgeLocator.findHalfEdgeWithEdge(triangles.First(), a, b).searchedEdge != null;
    }

    private Queue<HalfEdge> FindIntersectingEdges(HashSet<Face> triangles, Vertex a, Vertex b)
    {
        var intersectingEdges = new Queue<HalfEdge>(
            HalfEdgeOperations.findIntersectingEdges(triangles, a, b)
        );
        //GD.Print($"Intersecting edges count: {intersectingEdges.Count}");
        //intersectingEdges.ToList().ForEach(x => GD.Print(x.ToString()));
        return intersectingEdges;
    }

    private bool 
    FlipIntersectingEdge(ref HashSet<Face> triangles, ref HalfEdge currentEdge, (Vertex, Vertex) constrain, Queue<HalfEdge> intersectingEdges, List<HalfEdge> createdEdges) // Changed to value tuple
    {
        var quadrilateral = HalfEdgeOperations.GetQuadrilateral(currentEdge);

        if (quadrilateral.Count != 4)
        {
            // Handle cases where a valid quadrilateral cannot be formed (e.g., boundary edges)
            return false;
        }

        bool is_convex_quadatorial =
            GeometryUtils.CheckIfConvexQuadrilateral
            (
                quadrilateral.ElementAt(0),
                quadrilateral.ElementAt(1),
                quadrilateral.ElementAt(2),
                quadrilateral.ElementAt(3)
            );

        if (!is_convex_quadatorial)
        {
            intersectingEdges.Enqueue(currentEdge);
            return false;
        }

        TriangulationOperation.FlipEdge(ref currentEdge);

        bool is_flippededge_crossing =
            GeometryUtils.AreSegmentsCrossing(
                constrain.Item1.Position,
                constrain.Item2.Position,
                currentEdge.Origin.Position,
                currentEdge.Dest.Position
            );

        if (is_flippededge_crossing)
        {
            intersectingEdges.Enqueue(currentEdge);
        }
        else
        {
            createdEdges.Add(currentEdge);
        }
        return true;
    }

    private void RefineTriangulation(ref HashSet<Face> triangles, List<HalfEdge> createdEdges, List<(Vertex, Vertex)> constraints) // Changed to value tuple
    {
        bool flipped = false;
 

        do
        {
            flipped = false;
            for (int j = 0; j < createdEdges.Count; j++)
            {
                var edge = createdEdges.ElementAt(j);
                var currentFace = edge.Face;
                var currentTwin = edge.Twin; // Twin might be null for boundary edges

                var currentTwinFace = edge.Twin?.Face; // Twin might be null for boundary edges

                if (currentFace == null || currentTwin == null)
                {
                    continue;
                }

                if (IsEdgeConstrained(edge, constraints))
                {
                    continue;
                }

                Vertex current_p = edge.Prev.Origin;
                Vertex current_twin_p =currentTwin.Prev.Origin;

                bool delaunay_condition =
                    TriangulationOperation.InCircle(currentFace, current_twin_p)
                    || TriangulationOperation.InCircle(currentTwinFace, current_p);


                if (delaunay_condition)
                {
                    
                    TriangulationOperation.FlipEdge(ref edge);
                    flipped=true;
                }
            }
        } while (flipped);
    }

    private bool IsEdgeConstrained(HalfEdge edge, List<(Vertex, Vertex)> constraints)
    {
        return constraints.Any(c => GeometryUtils.AreEdgesEqual((edge.Origin, edge.Dest), c));
    }


}
