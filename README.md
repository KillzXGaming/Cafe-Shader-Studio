# Cafe-Shader-Studio
A program to preview various materials and shaders in games. 

[![Build status](https://ci.appveyor.com/api/projects/status/366wvxjdim9s5xco?svg=true)](https://ci.appveyor.com/project/KillzXGaming/cafe-shader-studio)

Currently supports shader viewing in:

- Mario Kart 8 U & Deluxe
- Super Mario Odyssey (set the romfs game path in settings menu)
- NSMBUDX
- NSMBU
- Kirby Star Allies
- Splatoon 2 (very WIP)
- Super Mario Maker 2 (very WIP)
- Animal Crossing New Horizons (very WIP, add shader in GlobalShaders folder)
- WWHD (very WIP)

## Features
- Real time viewing and editing of material data with shaders. Shader editing isn't supported.
- Animation playback with proper interpolation handling (previous tools had playback issues).
- Shape animations, material animations and other various types supported.

## Using
Make sure you have [net 5.0 or higher](https://dotnet.microsoft.com/download/dotnet/5.0). Download the releases zip file and run the exe. 

If you are on Linux, the application should be usable with this command `dotnet CafeShaderStudio.dll` in the program directory.

Keep in mind this is very WIP. Editing is limited to material editing. You can export/import materials as json for more direct editing (though keep in mind if the file swapped uses a different shader file, then the bfres will need to be reopened.  

## Notes
**If a material is red (for a game with supported shader viewing) that means it failed to find the shader. This means your model has a skin count not supported by the shader, or the shader option combination and/or render state combination is not supported by the shader.**

## Screenshots

![image](https://user-images.githubusercontent.com/13475262/116014206-24a6f900-a602-11eb-8a34-31d07576909f.png)

![image](https://user-images.githubusercontent.com/13475262/115976415-eba45100-a53b-11eb-9893-a6988c57e7d6.png)

![image](https://user-images.githubusercontent.com/13475262/116013454-41d9c880-a5fe-11eb-9661-63671f92a7ce.png)

## Credits
- KillzXGaming - main developer.
- JuPaHe64 - created animation timeline.
- Ryujinx - for shader libraries used to decompile and translate switch binaries into glsl code.
- OpenTK Team - for opengl c# bindings.
- mellinoe and IMGUI Team - for c# port and creating the IMGUI library
- Syroot - for bfres library and binary IO
- Jasper (no clip developer) - various help for MK8 and AGL usage.
- MasterVermilli0n for swizzle code, SRT calculation code used in parameter loading, and the shader decompiler tool for Wii U shaders.
- Decaf Team - for shader decompiler handling used by the tool.

HDR Cubemaps from HDRLabs used under the Creative Commons Attribution-Noncommercial-Share Alike 3.0 License. http://www.hdrlabs.com/sibl/index.html
