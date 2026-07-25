# testing

A simple Godot 4.7 + C# 3D test project featuring a first-person controller.

## Features

- First-person movement (walk / sprint)
- Mouse look and controller look
- Jumping with optional auto-bunnyhop
- Basic headbob effect while moving
- Input actions configured for keyboard and gamepad
- Uses Jolt Physics (3D)

## Project Structure

- project.godot — Godot project configuration
- testing.sln / testing.csproj — .NET/C# project files
- FPSController/
  - FpsController.cs — core first-person movement and look script
  - FPSController.tscn — scene for the FPS controller
- area_3d.tscn — main scene entry

## Requirements

- [Godot Engine 4.7](https://godotengine.org/) with .NET support
- .NET SDK compatible with your Godot .NET setup

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/TheInnocent1983/testing.git
   cd testing
   ```

2. Open the project in Godot:
   - Launch Godot (with .NET support)
   - Import/open this folder

3. Run the project:
   - Press Play in the editor  
   - Main scene is configured in project.godot

## Default Controls

### Movement
- Move Forward / Back / Left / Right
- Jump
- Sprint

### Look
- Mouse look when cursor is captured
- Controller right stick for look input

### Mouse Mode
- Click inside the game window to capture the cursor
- Press Esc to release the cursor

## License

No license is currently specified.
