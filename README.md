# A Study and Implementation of Incremental Delaunay Triangulation

This project implements **Delaunay triangulation**, a geometric algorithm that connects a set of 2D points into triangles.

A triangulation can be created in many ways, but Delaunay triangulation tries to avoid thin and narrow triangles when possible. This usually produces a cleaner and more stable mesh, which is useful in computer graphics, simulations, maps, and geometric applications.

The implementation builds the triangulation **incrementally**. Points are inserted one by one, and after each insertion the surrounding triangles are checked and adjusted. When two neighboring triangles do not satisfy the Delaunay condition, their shared edge is flipped to improve the local triangle structure.

The project was developed in **C# with .NET 9**. It uses a **half-edge data structure** to store vertices, edges, and triangle relationships, making it easier to update the mesh during point insertion and edge flipping.

A Windows Forms application is included to visualize the triangulation process step by step.

## Project Overview

The project includes:

* 2D geometric utility functions,
* half-edge based mesh representation,
* incremental point insertion,
* triangle splitting and edge flipping,
* Delaunay triangulation construction,
* Voronoi diagram visualization,
* unit tests for core components,
* and an interactive desktop visualizer.

## Thesis

[View the full thesis (PDF)](thesis.pdf)
