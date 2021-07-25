using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Toolbox.Core.IO;
using Toolbox.Core;
using CafeStudio.UI;
using GLFrameworkEngine;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK;

namespace CafeShaderStudio
{
    public class BatchRenderingTool
    {
        //UI settings
        public static string InputFolder = "";
        public static string OutputFolder = "Batch";
        public static int ImageWidth = 512;
        public static int ImageHeight = 512;

        public static bool OdysseyActor = false;

        public static bool CancelOperation = false;
        public static bool IsOperationActive = false;

        public static int ProcessAmount;
        public static int ProcessTotal;
        public static string ProcessName;

        //Batch globals
        IGraphicsContext Context;
        Framebuffer Framebuffer;
        GLContext Control;

        public float CameraRotationX = 0;
        public float CameraRotationY = 0;
        public float CameraDistance = 0;

        public BatchRenderingTool() { }

        public void StartRender(string inputFolder, string outputFolder, int width, int height)
        {
            ProcessTotal = 0;
            ProcessAmount = 0;

            IsOperationActive = true;

            Framebuffer?.Dispoe();

            OutputFolder = outputFolder;
            Framebuffer = new Framebuffer(FramebufferTarget.Framebuffer, width, height, PixelInternalFormat.Rgba);

            Control = new GLContext();
            Control.Camera = new Camera();

            Control.Width = width;
            Control.Height = height;
            Control.Camera.Width = width;
            Control.Camera.Height = height;

            Control.ScreenBuffer = Framebuffer;

            GraphicsMode mode = new GraphicsMode(new ColorFormat(32), 24, 8, 4, new ColorFormat(32), 2, false);
            var window = new GameWindow(width, height, mode);

            Context = new GraphicsContext(mode, window.WindowInfo);
            Context.MakeCurrent(window.WindowInfo);

            List<string> files = new List<string>();
            GetFiles(inputFolder, files);

            ProcessTotal = files.Count;
            for (int i = 0; i < files.Count; i++)
            {
                ProcessAmount = i;

                if (CancelOperation) {
                    break;
                }

                try
                {
                    ProcessName = $"Processing {files[i]}";
                    BatchRenderFile(files[i]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            ProcessAmount = files.Count - 1;
            IsOperationActive = false;
            CancelOperation = false;
        }

        public void GetFiles(string folder, List<string> fileList)
        {
            foreach (var dir in Directory.GetDirectories(folder))
                GetFiles(dir, fileList);

            foreach (var file in Directory.GetFiles(folder))
                fileList.Add(file);
        }

        public void BatchRenderFile(string fileName)
        {
            ClearWorkspace();

            var format = STFileLoader.OpenFileFormat(fileName);
            if (format is IArchiveFile)
            {
                if (OdysseyActor)
                    RunOdysseyActor((IArchiveFile)format);

                foreach (var file in ((IArchiveFile)format).Files) {
                    if (file.FileName.EndsWith(".bfres")) {
                        var fileFormat = file.OpenFile();
                        BatchFileFormat(fileFormat, fileName);
                    }
                }
            }
            else if (format is IRenderableFile)
                BatchFileFormat(format, fileName);
        }

        private void RunOdysseyActor(IArchiveFile format)
        {
            var actorData = new RedStarLibrary.ActorBase();
            actorData.LoadActor((IArchiveFile)format);
            actorData.InitModelFile();
            if (actorData.TextureArchive != null)
            {
                actorData.TextureArchive.Renderer.ID = DataCache.ModelCache.Values.Count.ToString();
                DataCache.ModelCache.Add(actorData.TextureArchive.Renderer.ID.ToString(), actorData.TextureArchive.Renderer);
            }
        }

        private void BatchFileFormat(IFileFormat format, string fileName)
        {
            if (format == null)
                return;

            if (CancelOperation) {
                IsOperationActive = false;
                return;
            }

            //Add file to pipeline
            var modelRender = format as IRenderableFile;
            if (modelRender.Renderer.Models.Count == 0)
                return;

            modelRender.Renderer.ID = DataCache.ModelCache.Values.Count.ToString();
            DataCache.ModelCache.Add(modelRender.Renderer.ID.ToString(), modelRender.Renderer);

            //Setup the camera
            var boundingSphere = modelRender.Renderer.BoundingSphere;
            Control.Camera.FrameBoundingSphere(boundingSphere);
            Control.Camera.RotationX = CameraRotationX * STMath.Deg2Rad;
            Control.Camera.RotationY = -CameraRotationY * STMath.Deg2Rad;
            Control.Camera.TargetDistance += CameraDistance;

            if (fileName.EndsWith("2D.szs"))
            {
                Control.Camera.RotationY = -90 * STMath.Deg2Rad;
            }
            else
            {
                Control.Camera.RotationY = 0;
            }

            Control.Camera.UpdateMatrices();

            //Render out the file to the pipeline FBO
            Framebuffer.Bind();

            GL.Viewport(0, 0, Control.Width, Control.Height);
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            if (Control.UseSRBFrameBuffer)
                GL.Enable(EnableCap.FramebufferSrgb);

            modelRender.Renderer.DrawModel(Control, Pass.OPAQUE, Vector4.Zero);
            modelRender.Renderer.DrawModel(Control, Pass.TRANSPARENT, Vector4.Zero);

            GL.Flush();
            Context.SwapBuffers();

            string name = Path.GetFileNameWithoutExtension(fileName);
            name = PreventDuplicateNames(name);
            Framebuffer.ReadImagePixels(false).Save($"{OutputFolder}\\{name}.png");

            Framebuffer.Unbind();
        }

        private string PreventDuplicateNames(string name, int i = 0)
        {
            if (!File.Exists($"{OutputFolder}\\{name}.png"))
                return name;

            return PreventDuplicateNames($"{name}{i}", i++);
        }

        private void ClearWorkspace()
        {
            foreach (var render in DataCache.ModelCache.Values)
                render.Dispose();

            foreach (var tex in Runtime.TextureCache)
                tex.RenderableTex?.Dispose();

            Control.Scene.PickableObjects.Clear();
            DataCache.ModelCache.Clear();
            Runtime.TextureCache.Clear();
            BfresEditor.BfresRender.ClearShaderCache();

            GC.Collect();
        }
    }
}
