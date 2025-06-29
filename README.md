# Triangle Splatting Importer

This project implements a system for importing `.off` files generated as a
"Byproduct of the Triangle-Based Representation" from the "Triangle Splatting"
paper into Unity.

- **Triangle Splatting:** https://trianglesplatting.github.io

The second aim of this project is to attempt to create a practical Unity package
using agentic coding. This branch uses the [Gemini CLI] with the Gemini 2.5
Flash model.

[Gemini CLI]: https://github.com/google-gemini/gemini-cli

## Strategy

The `.off` files contain a very large number of triangles. To handle this,
the importer splits the model into smaller chunks that are spatially coherent
and have a vertex count of less than 65535.

## Implementation

A Scripted Importer is used to import the `.off` files. This importer
generates a prefab containing Game Objects for all the chunks. The user can
then simply place this prefab into a scene to render it.
