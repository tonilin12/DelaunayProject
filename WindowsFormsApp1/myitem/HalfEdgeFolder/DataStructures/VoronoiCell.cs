using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using WindowsFormsApp1.myitem.GeometryFolder;

namespace WindowsFormsApp1.myitem.HalfEdgeFolder.DataStructures
{
    public class VoronoiCell
    {
        private readonly Vertex _vertex;
        private List<Vector2> _cachedPolygon;
        public bool IsDirty { get; private set; } = true;

        public VoronoiCell(Vertex v)
        {
            _vertex = v ?? throw new ArgumentNullException(nameof(v));
            _cachedPolygon = new List<Vector2>();
        }

        /// <summary>
        /// Marks the polygon as dirty to force recomputation.
        /// </summary>
        /// Marks the polygon as dirty to force recomputation.
        /// </summary>
        public void MarkDirty()
        {
            IsDirty = true;
        }

        /// <summary>
        /// Returns the polygon representing the Voronoi cell around this vertex.
        /// Polygon is recomputed only if dirty.
        /// </summary>
        public List<Vector2> GetPolygon()
        {
            if (!IsDirty)
                return _cachedPolygon;

            var polygon = new List<Vector2>();

            if (_vertex.OutgoingHalfEdge == null)
            {
                _cachedPolygon = polygon;
                IsDirty = false;
                return polygon;
            }

            // Traverse all edges around the vertex in CCW order
            foreach (var center in _vertex.EnumerateEdges(e => e.Face?.Circumcenter))
            {
                if (center.HasValue)
                {
                    if (polygon.Count == 0 || Vector2.DistanceSquared(polygon.Last(), center.Value) > 1e-8f)
                        polygon.Add(center.Value);
                }
            }

            // Ensure polygon is closed
            if (polygon.Count > 2 && Vector2.DistanceSquared(polygon[0], polygon.Last()) > GeometryUtils.GetEpsilon)
                polygon.Add(polygon[0]);

            _cachedPolygon = polygon;
            IsDirty = false;
            return polygon;
        }
    }
}
