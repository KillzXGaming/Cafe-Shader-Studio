# Cafe-Shader-Studio
A program to preview various materials and shaders in games. 

Currently supports shader viewing in:

- Mario Kart 8 Deluxe
- Super Mario Odyssey (set the romfs game path in settings menu)
- NSMBUDX
- Kirby Star Allies
- Splatoon 2 (very WIP)
- Super Mario Maker 2 (very WIP)
- Animal Crossing New Horizons (very WIP, add shader in GlobalShaders folder)

## Features
- Real time viewing and editing of material data with shaders. Shader editing isn't supported.
- Animation playback with proper interpolation handling (previous tools had playback issues).
- Shape animations, material animations and other various types supported.

## Todo
- Better camera viewing. Need to support a proper walk camera.
- Improve default cubemaps.

## Using
Make sure you have [net 5.0 or higher](https://dotnet.microsoft.com/download/dotnet/5.0). Download the releases zip file and run the exe. 

Keep in mind this is very WIP. Editing is limited to material editing. You can export/import materials as json for more direct editing (though keep in mind if the file swapped uses a different shader file, then the bfres will need to be reopened.  

## Screenshots

![image](https://user-images.githubusercontent.com/13475262/115976415-eba45100-a53b-11eb-9893-a6988c57e7d6.png)

![image](https://user-images.githubusercontent.com/13475262/115976191-66b83800-a539-11eb-8569-6d5a947c31f5.png)

## Credits
- KillzXGaming - main developer.
- JuPaHe64 - created animation timeline.
- Ryujinx - for shader libraries used to decompile and translate switch binaries into glsl code.
- OpenTK Team - for opengl c# bindings.
- mellinoe and IMGUI Team - for c# port and creating the IMGUI library
- Syroot - for bfres library and binary IO

HDR Cubemaps from HDRLabs used under the Creative Commons Attribution-Noncommercial-Share Alike 3.0 License. http://www.hdrlabs.com/sibl/index.html
