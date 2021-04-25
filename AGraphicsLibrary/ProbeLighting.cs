using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AampLibraryCSharp;
using OpenTK;

namespace AGraphicsLibrary
{
    public class ProbeLighting
    {
        /// <summary>
        /// Index used for when a probe is not used for calculating coefficents in the grid.
        /// </summary>
        public static readonly uint UnusedIndex = 4294377462;

        public Grid RootGrid { get; set; }
        public ProbeParams Params { get; set; }

        public List<ProbeBoxObject> Boxes = new List<ProbeBoxObject>();

        public void LoadValues(AampFile aamp)
        {
            Params = new ProbeParams();
            foreach (var val in aamp.RootNode.paramObjects)
            {
                if (val.HashString == "root_grid")
                    RootGrid = LoadGridData(val.paramEntries);
                if (val.HashString == "param_obj") {
                    foreach (var param in val.paramEntries)
                    {
                        if (param.HashString == "version")
                            Params.Version = (uint)param.Value;
                        if (param.HashString == "dir_light_indirect")
                            Params.IndirectDirectionLight = (float)param.Value;
                        if (param.HashString == "point_light_indirect")
                            Params.IndirectPointLight = (float)param.Value;
                        if (param.HashString == "spot_light_indirect")
                            Params.IndirectSpotLight = (float)param.Value;
                        if (param.HashString == "emission_scale")
                            Params.EmissionScale = (float)param.Value;
                    }
                }
            }

            foreach (var val in aamp.RootNode.childParams)
            {
                ProbeBoxObject box = new ProbeBoxObject();
                Boxes.Add(box);

                foreach (var param in val.paramObjects)
                {
                    if (param.HashString == "grid")
                        box.Grid = LoadGridData(param.paramEntries);
                    if (param.HashString == "sh_data_buffer")
                        box.DataBuffer = LoadDataBuffer(param.paramEntries);
                    if (param.HashString == "sh_index_buffer")
                        box.IndexBuffer = LoadIndexBuffer(param.paramEntries);
                }
            }
        }

        private Grid LoadGridData(ParamEntry[] paramEntries)
        {
            Grid grid = new Grid();

            foreach (var entry in paramEntries)
            {
                if (entry.HashString == "aabb_min_pos")
                    grid.Min = (Syroot.Maths.Vector3F)entry.Value;
                if (entry.HashString == "aabb_max_pos")
                    grid.Max = (Syroot.Maths.Vector3F)entry.Value;
                if (entry.HashString == "voxel_step_pos")
                    grid.Step = (Syroot.Maths.Vector3F)entry.Value;
            }

            return grid;
        }

        private SHDataBuffer LoadDataBuffer(ParamEntry[] paramEntries)
        {
            SHDataBuffer buffer = new SHDataBuffer();
            foreach (var entry in paramEntries)
            {
                if (entry.HashString == "type")
                    buffer.Type = (uint)entry.Value;
                if (entry.HashString == "max_sh_data_num")
                    buffer.MaxDataNum = (uint)entry.Value;
                if (entry.HashString == "used_data_num")
                    buffer.UsedDataNum = (uint)entry.Value;
                if (entry.HashString == "per_probe_float_num")
                    buffer.PerProbeFloatNum = (uint)entry.Value;
                if (entry.HashString == "data_buffer")
                    buffer.DataBuffer = (float[])entry.Value;
            }
            return buffer;
        }

        private SHIndexBuffer LoadIndexBuffer(ParamEntry[] paramEntries)
        {
            SHIndexBuffer buffer = new SHIndexBuffer();
            foreach (var entry in paramEntries)
            {
                if (entry.HashString == "type")
                    buffer.Type = (uint)entry.Value;
                if (entry.HashString == "max_index_num")
                    buffer.MaxIndicesNum = (uint)entry.Value;
                if (entry.HashString == "used_index_num")
                    buffer.UsedIndicesNum = (uint)entry.Value;
                if (entry.HashString == "index_buffer")
                {
                    var indices = (uint[])entry.Value;
                    buffer.IndexBuffer = new ushort[indices.Length * 2];
                    for (int i = 0; i < indices.Length; i++) {
                        //Indices are ushorts packed into uints
                        buffer.IndexBuffer[i] =      (ushort)(indices[i] >> 16);
                        buffer.IndexBuffer[i + 1] =  (ushort)(indices[i] & 0xFFFF);
                    }
                }
            }
            return buffer;
        }
    }

    public class ProbeParams
    {
        public uint Version { get; set; }
        public float IndirectDirectionLight { get; set; }
        public float IndirectPointLight { get; set; }
        public float IndirectSpotLight { get; set; }
        public float EmissionScale { get; set; }
    }

    public class ProbeBoxObject
    {
        /// <summary>
        /// The index of the current probe box.
        /// </summary>
        public uint Index { get; set; }

        /// <summary>
        /// The type of probe object used.
        /// </summary>
        public uint Type { get; set; }

        /// <summary>
        /// The grid to determine the boundry used for probe lighting.
        /// </summary>
        public Grid Grid { get; set; }

        /// <summary>
        /// The index buffer used to lookup probe color values.
        /// </summary>
        public SHIndexBuffer IndexBuffer { get; set; }

        /// <summary>
        /// The data buffer used for probe color/lighting calculations.
        /// </summary>
        public SHDataBuffer DataBuffer { get; set; }
    }

    public struct SHIndexBuffer
    {
        /// <summary>
        /// The buffer type. 0 for index buffer, 1 for data buffer
        /// </summary>
        public uint Type { get; set; }

        /// <summary>
        /// The total amount of indices being used in the data buffer.
        /// </summary>
        public uint UsedIndicesNum { get; set; }

        /// <summary>
        /// The max amount of indices in the data buffer.
        /// </summary>
        public uint MaxIndicesNum { get; set; }

        /// <summary>
        /// A list of indices that determine the data to pull from the data buffer.
        /// </summary>
        public ushort[] IndexBuffer { get; set; }
    }

    public struct SHDataBuffer
    {
        /// <summary>
        /// The buffer type. 0 for index buffer, 1 for data buffer
        /// </summary>
        public uint Type { get; set; }

        /// <summary>
        /// The total amount of indices being used in the data buffer.
        /// </summary>
        public uint UsedDataNum { get; set; }

        /// <summary>
        /// The max amount of data in the data buffer.
        /// </summary>
        public uint MaxDataNum { get; set; }

        /// <summary>
        /// The total amount floats used by a single probe.
        /// Typically uses 27 spherical harmonics.
        /// </summary>
        public uint PerProbeFloatNum { get; set; }

        /// <summary>
        /// The data buffer used to store color values for each probe.
        /// </summary>
        public float[] DataBuffer { get; set; }
    }

    public struct Grid
    {
        /// <summary>
        /// The bounding voxel grid min value.
        /// </summary>
        public Syroot.Maths.Vector3F Min { get; set; }

        /// <summary>
        /// The bounding voxel grid max value.
        /// </summary>
        public Syroot.Maths.Vector3F Max { get; set; }

        /// <summary>
        /// The bounding voxel step value for spacing each probe.
        /// </summary>
        public Syroot.Maths.Vector3F Step { get; set; }


        /// <summary>
        /// Checks if the given position is inside the bounding grid.
        /// </summary>
        /// <param name="positionX"></param>
        /// <param name="positionY"></param>
        /// <param name="positionZ"></param>
        /// <returns></returns>
        public bool HasHit(float positionX, float positionY, float positionZ)
        {
            return (positionX >= Min.X && positionX <= Max.X) &&
                   (positionY >= Min.Y && positionY <= Max.Y) &&
                   (positionZ >= Min.Z && positionZ <= Max.Z);
        }
    }
}
