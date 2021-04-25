using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toolbox.Core.Animations;
using Toolbox.Core;
using BfresLibrary;
using GLFrameworkEngine;
using OpenTK;

namespace BfresEditor
{
    public class BfresShapeAnim : STAnimation
    {
        private string ModelName = null;

        public int KeyIndex = 0;

        public BfresShapeAnim(ShapeAnim anim, string name) {
            ModelName = name;
            CanPlay = true; //Default all animations to not play unless toggled in list
            Reload(anim);
        }

        public override void NextFrame()
        {
            foreach (ShapeAnimGroup group in AnimGroups)
            {
                var meshes = GetMeshes(group.Name);
                foreach (var mesh in meshes)
                    ParseKeyTrack(mesh, group.BaseTarget, group.Tracks);
            }
        }

        private void ParseKeyTrack(BfresMeshAsset mesh, STAnimationTrack baseTarget, List<STAnimationTrack> tracks)
        {
            float[] weights = new float[tracks.Count];
            for (int i = 0; i < tracks.Count; i++)
                weights[i] = tracks[i].GetFrameValue(this.Frame);

            float totalWeight = 0;
            foreach (float f in weights)
                totalWeight += f;

            float baseWeight = 1.0f - totalWeight;
            float total = totalWeight + baseWeight;

            mesh.MorphPositions.Clear();

            //Trasform the existing track key data
            for (int i = 0; i < mesh.Shape.Vertices.Count; i++)
            {
                var vertex = mesh.Shape.Vertices[i];

                //Determine what vertex to morph from.
                if (baseTarget != null && mesh.KeyGroups.ContainsKey(baseTarget.Name))
                {
                    var keyShape = mesh.KeyGroups[baseTarget.Name];
                    //Add the keyed position target
                    mesh.MorphPositions.Add(keyShape.Vertices[i].Position);
                }
                else
                    mesh.MorphPositions.Add(vertex.Position);

                Vector3 position = mesh.MorphPositions[i];
                position *= baseWeight;

                foreach (var track in tracks)
                {
                    if (!mesh.KeyGroups.ContainsKey(track.Name))
                        continue;

                    //The total weight used for a single vertex point
                    var weight = track.GetFrameValue(this.Frame);

                    //Get the track's key shape
                    var keyShape = mesh.KeyGroups[track.Name];
                    //Add the keyed position and weigh it
                    position += keyShape.Vertices[i].Position * weight;
                }

                //Set the output vertex
                position /= total;
                mesh.MorphPositions[i] = position;
            }

            mesh.UpdateVertexBuffer();
        }

        public void Reload(ShapeAnim anim)
        {
            Name = anim.Name;
            FrameCount = anim.FrameCount;
            FrameRate = 60.0f;
            Loop = anim.Flags.HasFlag(ShapeAnimFlags.Looping);

            AnimGroups.Clear();
            foreach (var shapeAnim in anim.VertexShapeAnims) {
                var group = new ShapeAnimGroup();
                AnimGroups.Add(group);
                group.Name = shapeAnim.Name;

                //Get the shape keys used for animating
                int baseIndex = 0;
                for (int i = 0; i < shapeAnim.KeyShapeAnimInfos.Count; i++)
                {
                    int startBaseIndex = shapeAnim.KeyShapeAnimInfos.Count- shapeAnim.BaseDataList.Length;

                    var keyShapeInfo = shapeAnim.KeyShapeAnimInfos[i];

                    //Get the curve index for animated indices
                    int curveIndex = keyShapeInfo.CurveIndex;

                    //Make a new sampler track using step interpolation
                    var track = new KeyShapeTrack();
                    track.InterpolationType = STInterpoaltionType.Step;
                    track.Name = keyShapeInfo.Name;

                    if (group.BaseTarget  == null && curveIndex == -1) {
                        group.BaseTarget = track;
                    }

                    if (curveIndex != -1)
                        BfresAnimations.GenerateKeys(track, shapeAnim.Curves[curveIndex]);
                    else if (i >= startBaseIndex)
                    {
                        float baseWeight = shapeAnim.BaseDataList[baseIndex];
                        track.KeyFrames.Add(new STKeyFrame(0, baseWeight));
                        baseIndex++;
                    }

                    group.Tracks.Add(track);
                }
            }
        }

        public class ShapeAnimGroup : STAnimGroup
        {
            public List<STAnimationTrack> Tracks = new List<STAnimationTrack>();
            public STAnimationTrack BaseTarget = null;

            public override List<STAnimationTrack> GetTracks() { return Tracks; }
        }

        public class KeyShapeTrack : BfresAnimationTrack
        {

        }

        private List<BfresMeshAsset> GetMeshes(string name)
        {
            List<BfresMeshAsset> meshes = new List<BfresMeshAsset>();
            foreach (BfresRender render in DataCache.ModelCache.Values)
            {
                foreach (BfresModelAsset model in render.Models)
                {
                    foreach (var mesh in model.Meshes)
                    {
                        if (mesh.Name == name)
                            meshes.Add(mesh);
                    }
                }
            }
            return meshes;
        }
    }
}
