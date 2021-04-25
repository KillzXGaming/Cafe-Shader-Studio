using System;
using System.Collections.Generic;
using System.IO;
using Toolbox.Core;
using Toolbox.Core.IO;
using ByamlExt.Byaml;
using GLFrameworkEngine;

namespace RedStarLibrary
{
    public class ActorBase
    {
        /// <summary>
        /// The sub actor data used for linking part data.
        /// </summary>
        public InitSubActor InitSubActor { get; set; }

        /// <summary>
        /// The model data for configuring external animation and texture paths.
        /// </summary>
        public InitModel InitModel { get; set; }

        /// <summary>
        /// The model file used for rendering.
        /// </summary>
        public IRenderableFile ModelFile { get; set; }

        /// <summary>
        /// The external texture archive (null if unused).
        /// </summary>
        public IRenderableFile TextureArchive { get; set; }

        /// <summary>
        /// Loads the actor from an archive file given a file path.
        /// </summary>
        public void LoadActor(string archiveFilePath) {
            LoadActor((IArchiveFile)STFileLoader.OpenFileFormat(archiveFilePath));
        }

        //Cache of files for looking up files needed to use.
        Dictionary<string, ArchiveFileInfo> Files = new Dictionary<string, ArchiveFileInfo>();

        //Part model fixes (InitPartsFixInfo)
        public Dictionary<string, ActorBase> PartActors = new Dictionary<string, ActorBase>();

        /// <summary>
        /// Loads the actor from an archive file given an IArchiveFile.
        /// </summary>
        public void LoadActor(IArchiveFile fileArchive) {
            //Add files to a lookup for quick searching
            foreach (var file in fileArchive.Files) {
                Files.Add(file.FileName, file);

                //Initalize sub actor data to determine how to display part information
                if (file.FileName == "InitSubActor.byml")
                    InitSubActor = new InitSubActor(ByamlFile.LoadN(file.FileData).RootNode);
                if (file.FileName == "InitModel.byml")
                    InitModel = new InitModel(ByamlFile.LoadN(file.FileData).RootNode);
                if (file.FileName.EndsWith(".bfres")) //Note there should only be one model file
                    ModelFile = file.OpenFile() as IRenderableFile;
            }
        }

        /// <summary>
        /// Inits model data of the actor.
        /// </summary>
        public void InitModelFile() {
            if (InitModel == null)
                return;

            if (!string.IsNullOrEmpty(InitModel.ExternalTextureFile))
            {
                string path = $"{GlobalSettings.GamePath}\\ObjectData\\{InitModel.ExternalTextureFile}.szs";
                if (!File.Exists(path))
                {
                    Console.WriteLine($"Failed to find external texture file at path {path}!");
                    return;
                }

                var fileArchive = STFileLoader.OpenFileFormat(path) as IArchiveFile;
                foreach (var file in fileArchive.Files)
                    TextureArchive = file.OpenFile() as IRenderableFile;
            }
        }

        /// <summary>
        /// Inits part data of the actor.
        /// </summary>
        public void InitActorPartList(bool isPlayerCostume = true)
        {
            foreach (var actor in InitSubActor.Actors) {
                if (actor.ClassName == "PartsModel") {
                    LoadPart(actor.FixFileSuffixName, actor.ModelName);
                }
            }

            //Todo figure out how the head is linked (probably hardcoded)
            if (isPlayerCostume && Files.ContainsKey($"InitPartsFixInfoHead.byml"))
                LoadPart("Head", "MarioHead");
        }

        /// <summary>
        /// Loads part data from the given suffix and model name.
        /// This will attach the part to a joint of the current actor model and add to the PartActors list.
        /// </summary>
        public void LoadPart(string suffix, string modelName)
        {
            string path = $"{GlobalSettings.GamePath}\\ObjectData\\{modelName}.szs";
            if (!File.Exists(path)) {
                Console.WriteLine($"Failed to find part at path {path}!");
                return;
            }

            //Init parameter data
            var partParams = Files[$"InitPartsFixInfo{suffix}.byml"];
            var partModel = new PartsModel(ByamlFile.LoadN(partParams.FileData).RootNode);

            var actor = new ActorBase();
            actor.LoadActor(path);
            PartActors.Add(modelName, actor);

            if (actor.ModelFile == null) {
                Console.WriteLine($"Failed to find model for {modelName}!");
                return;
            }
            //Parts attach to joints of the current model from the part model
            AttachJoint(partModel, actor.ModelFile);
        }

        private void AttachJoint(PartsModel partInfo, IRenderableFile actorRender)
        {
            var modelPart = actorRender.Renderer.Models[0];

            foreach (ModelAsset model in ModelFile.Renderer.Models) {
                var bone = model.ModelData.Skeleton.SearchBone(partInfo.JointName);

                if (bone != null) {
                    //Setup local transform
                    var localPosition = new OpenTK.Vector3(
                        partInfo.LocalTranslate.X,
                        partInfo.LocalTranslate.Y,
                        partInfo.LocalTranslate.Z);
                    var localRotation = STMath.FromEulerAngles(new OpenTK.Vector3(
                        partInfo.LocalRotate.X,
                        partInfo.LocalRotate.Y,
                        partInfo.LocalRotate.Z) * STMath.Deg2Rad);
                    var localScale = new OpenTK.Vector3(
                        partInfo.LocalScale.X,
                        partInfo.LocalScale.Y,
                        partInfo.LocalScale.Z);

                    modelPart.ModelData.Skeleton.LocalTransform =
                        OpenTK.Matrix4.CreateScale(localScale) *
                        OpenTK.Matrix4.CreateFromQuaternion(localRotation) *
                        OpenTK.Matrix4.CreateTranslation(localPosition);

                    //Attach bone children
                    bone.AttachSkeleton(modelPart.ModelData.Skeleton);
                    model.ModelData.Skeleton.Update();
                    break;
                }
            }
        }
    }
}
