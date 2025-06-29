# Triangle Splatting Importer

![Screenshot](https://github.com/user-attachments/assets/a76de5e0-df61-4a10-b57f-2d2fb5e4f043)

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

## About the Agentic Coding Experience

A general guide ([GEMINI.md](GEMINI.md)) and a design document
([Design.md](Design.md)) were prepared before the session. The development steps
were then manually directed in the subsequent session with the Gemini CLI. The
prompts used can be found in [Log.md](Log.md).

During the session, the free tier of the Gemini API was exhausted, so it fell
back to using the `gemini-2.5-flash` model. While it sometimes entered
infinite loops that required manual cancellation, it successfully handled most
of the tasks.

The Model Context Protocol (MCP) was not used this time, so errors from Unity's
`Editor.log` were manually copy-pasted. This process could be improved by
creating a custom script to automate these actions.

In another session, screenshots were used to show erratic behavior. Although
Gemini is multi-modal and can "see" the images, this did not significantly help
the debugging process in this particular case.

It is worth noting that this project is fairly simple. It does not implement the
actual Triangle Splatting technique but only uses its byproduct as a simple
colored mesh. The custom importer's sole function is to parse the `.off` file
and split it into spatially clustered mesh chunks.

An initial attempt to implement the actual Triangle Splatting technique as
described in the original paper was unsuccessful. The current conclusion is that
while the Gemini model is proficient at implementing standard modules with
detailed prompting, it is not yet capable of materializing novel techniques
from high-level ideas.
