# Technology Choices

- The project is built with Unity.
- We use the Universal Render Pipeline (URP) for rendering.
- UI Toolkit is used for building runtime user interfaces.
- IMGUI may be used for editor-only UI elements.

# Directory Structure

- Editable project source files are located in the `Assets/` directory and its
  subdirectories.
- Read-only package source files are located in `Library/PackageCache/`. **Do not
  modify** files in this directory.

# Code Style Guidelines

- All comments must be written in English.
- Do not use documentation-style comments (`///` or `/** */`), as we do not
  generate documentation from comments.
- Use `var` for local variables whenever the type can be inferred.
- Omit the `private` access modifier when it is implicit and does not harm
  clarity.
- Omit braces for single-statement blocks (`if`, `for`, `while`, etc.).
- Use expression-bodied members whenever appropriate (for properties, methods,
  lambdas, etc.).

# Workflow Instructions

- Focus primarily on writing or modifying source code.
- If an operation requires scene editing or interaction with the Unity Editor,
  provide clear, step-by-step instructions.
- Write all Git commit messages in English.
