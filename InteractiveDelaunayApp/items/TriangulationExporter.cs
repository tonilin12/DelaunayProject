using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WinFormsApp2.items
{
    // Exporter (instance-based)
    public class TriangulationExporter
    {
        public TriangulationExporter() { }

        public void Export(List<Face> triangulation, List<Vertex> insertedVertices, string exportDir)
        {
            Directory.CreateDirectory(exportDir);

            string objFilePath = Path.Combine(exportDir, "triangulation.obj");
            string logFilePath = Path.Combine(exportDir, "insertion_log.txt");

            ExportTriangulationToObj(triangulation, objFilePath);
            ExportInsertionLog(insertedVertices, logFilePath);
        }

        private void ExportTriangulationToObj(List<Face> triangulation, string filePath)
        {
            if (triangulation == null || triangulation.Count == 0) return;

            using var writer = new StreamWriter(filePath);
            writer.WriteLine("# Triangulation OBJ Export");

            var allVerts = triangulation.SelectMany(f => f.GetVertices()).ToList();
            var vertexList = allVerts
                .GroupBy(v => (v.Position.X, v.Position.Y))
                .Select(g => g.First())
                .OrderBy(v => v.Position.X)
                .ThenBy(v => v.Position.Y)
                .ToList();

            var vertexIndices = new Dictionary<(float x, float y), int>();
            for (int i = 0; i < vertexList.Count; i++)
            {
                var v = vertexList[i];
                var key = (v.Position.X, v.Position.Y);
                vertexIndices[key] = i + 1;
                writer.WriteLine($"v {v.Position.X:F6} {v.Position.Y:F6} 0.000000");
            }

            foreach (var face in triangulation)
            {
                var verts = face.GetVertices().ToArray();
                if (verts.Length != 3) continue;
                int idx1 = vertexIndices[(verts[0].Position.X, verts[0].Position.Y)];
                int idx2 = vertexIndices[(verts[1].Position.X, verts[1].Position.Y)];
                int idx3 = vertexIndices[(verts[2].Position.X, verts[2].Position.Y)];
                writer.WriteLine($"f {idx1} {idx2} {idx3}");
            }
        }

        private void ExportInsertionLog(List<Vertex> insertedVertices, string logFilePath)
        {
            using var writer = new StreamWriter(logFilePath);
            writer.WriteLine("# Point Insertion Log");
            writer.WriteLine("# Index X Y");
            for (int i = 0; i < insertedVertices.Count; i++)
            {
                var v = insertedVertices[i];
                writer.WriteLine($"{i + 1} {v.Position.X:F6} {v.Position.Y:F6}");
            }
        }
    }
}
