using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using ImGuiNET;
using Toolbox.Core;
using System.IO;
using Toolbox.Core.Imaging;
using GLFrameworkEngine;

namespace CafeStudio.UI
{
    public class TextureDialog
    {
        public List<ImportedTexture> Textures = new List<ImportedTexture>();

        public List<int> SelectedIndices = new List<int>();

        ImageEditorViewport ImageCanvas;

        private List<TexFormat> SupportedFormats = new List<TexFormat>();

        //The texture instance to display on the viewer
        private int TextureDisplayID { get; set; } = -1;

        private GLTexture2D DecodedTexture;

        //The thread to encode/decode the texture.
        private Thread Thread;

        private string TaskProgress = "";

        bool finishedEncoding = false;
        
        public TextureDialog(Type textureType) {
            var instance = (STGenericTexture)Activator.CreateInstance(textureType);
            var formatList = instance.SupportedFormats;

            //Check if in tool encoder supports them
            foreach (var format in formatList){
                foreach (var decoder in FileManager.GetTextureDecoders()) {
                    if (decoder.CanEncode(format) && !SupportedFormats.Contains(format)) {
                        SupportedFormats.Add(format);
                    }
                }
            }

            if (SupportedFormats.Count == 0)
                SupportedFormats.Add(TexFormat.RGBA8_UNORM);
        }

        public void OnLoad() {
            ImageCanvas = new ImageEditorViewport();
            ImageCanvas.Camera.Zoom = 95;
            ImageCanvas.OnLoad();

            DecodedTexture = GLTexture2D.CreateUncompressedTexture(1, 1);
        }

        public void AddTexture(string fileName) {
            if (!File.Exists(fileName))
                throw new Exception($"Invalid input file path! {fileName}");

            Textures.Add(new ImportedTexture(fileName));

            //Display the first image
            if (Textures.Count == 1)
                ReloadImageDisplay();
        }

        public void Render()
        {
            ImGui.Columns(3);
            DrawList();
            ImGui.NextColumn();

            ImGui.Text(TaskProgress);
            DrawCanvas();
            ImGui.NextColumn();
            DrawProperties();
            ImGui.NextColumn();
            ImGui.Columns(1);
        }

        private void DrawList()
        {
            if (ImGui.BeginChild("##texture_dlg_list")){
                ImGui.Columns(2);

                //Force a selection
                if (SelectedIndices.Count == 0)
                    SelectedIndices.Add(0);

                for (int i = 0; i < Textures.Count; i++)
                {
                    bool isSelected = SelectedIndices.Contains(i);

                    if (ImGui.Selectable(Textures[i].Name, isSelected, ImGuiSelectableFlags.SpanAllColumns)) {
                        SelectedIndices.Clear();
                        SelectedIndices.Add(i);

                        ReloadImageDisplay();
                    }
                    ImGui.NextColumn();
                    ImGui.Text(Textures[i].TargetOutput.ToString());
                    ImGui.NextColumn();
                }
                ImGui.Columns(1);
            }
            ImGui.EndChild();
        }

        private byte[] decodedImage;

        private void DrawCanvas()
        {
            if (ImageCanvas == null)
                OnLoad();

            var selectedIndex = SelectedIndices.FirstOrDefault();
            var texture = Textures[selectedIndex];

            if (finishedEncoding) {
                //Display the decoded data as an RGBA texture
                DecodedTexture.Reload(texture.Width, texture.Height, decodedImage);
                finishedEncoding = false;
            }

            if (ImGui.BeginChild("##texture_dlg_canvas"))
            {
                var size = ImGui.GetWindowSize();

                ImGui.Image((IntPtr)DecodedTexture.ID, size);
                /*
                                if (ActiveTexture != null)
                                {
                                    ImageCanvas.DisplayAlpha = true;
                                    ImageCanvas.ActiveTexture = ActiveTexture;
                                    ImageCanvas.Render((int)size.X, (int)size.Y);
                                }*/
            }
            ImGui.EndChild();
        }

        private void DrawProperties()
        {
            if (ImGui.BeginChild("##texture_dlg_properties"))
            {
                var size = ImGui.GetWindowSize();

                if (Textures.Count != 0)
                {
                    //There is always a selected texture
                    var selectedIndex = SelectedIndices.FirstOrDefault();
                    var texture = Textures[selectedIndex];

                    if (ImGui.BeginCombo("Format", texture.TargetOutput.ToString()))
                    {
                        foreach (var format in SupportedFormats)
                        {
                            bool isSelected = format == texture.TargetOutput;
                            if (ImGui.Selectable(format.ToString())) {
                                texture.TargetOutput = format;
                                ReloadImageDisplay();
                            }
                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }

                        ImGui.EndCombo();
                    }
                }

                var buttonSize = new Vector2(70, 30);

                ImGui.SetCursorPos(new Vector2(size.X - 160, size.Y - 35));
                if (ImGui.Button("Ok", buttonSize)) { }
                ImGui.SameLine();
                if (ImGui.Button("Cancel", buttonSize)) { }
            }
            ImGui.EndChild();
        }

        private void ReloadImageDisplay()
        {
            if (Textures.Count == 0)
                return;

            var selectedIndex = SelectedIndices.FirstOrDefault();
            var texture = Textures[selectedIndex];

            Task task = Task.Factory.StartNew(DisplayEncodedTexture);
            task.Wait();
        }

        private void DisplayEncodedTexture()
        {
            if (Textures.Count == 0)
                return;

            TaskProgress = "Encoding texture..";

            var selectedIndex = SelectedIndices.FirstOrDefault();
            var texture = Textures[selectedIndex];

            Thread = new Thread((ThreadStart)(() =>
            {
                //Encode the current format
                var encoded = texture.EncodeTexture();
                //Decode the newly encoded image data
                decodedImage = texture.DecodeTexture();

                TaskProgress = $"Finished encoding! {texture.Name} {texture.TargetOutput}";
                finishedEncoding = true;
            }));
            Thread.Start();
        }
    }
}
