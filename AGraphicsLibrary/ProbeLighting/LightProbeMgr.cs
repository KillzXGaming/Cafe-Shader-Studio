using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

namespace AGraphicsLibrary
{
    public partial class LightProbeMgr
    {
        /*
         * Game Struct
            struct Grid
            {
	            MinX, //0x24
	            MinY, //0x28
	            MinZ, //0x2C
	
	            MaxX, //0x3C
	            MaxY, //0x40
	            MaxZ, //0x44
	
	            StepX, //0x54
	            StepY, //0x58
	            StepZ, //0x5C

	            StrideX, //0x60
	            StrideY, //100
	            StrideZ, //0x68

                MinXYZ (adjusted in setup) //0x6C, 0x70, 0x74
                MaxXYZ (adjusted in setup) //0x78, 0x7C, 0x80
            }
        */

        public static bool GetInterpolatedSH(ProbeLighting probeLighting, Vector3 worldPosition, bool isTriLinear, ref float[] shData)
        {
            if (!probeLighting.RootGrid.IsInside(worldPosition.X, worldPosition.Y, worldPosition.Z))
                return false;

            foreach (ProbeVolume volume in probeLighting.Boxes) {
                if (volume.Grid.IsInside(worldPosition.X, worldPosition.Y, worldPosition.Z)) {
                    VoxelState state = VoxelState.Empty;
                    if (isTriLinear)
                        state = GetSHTriLinear(volume, worldPosition, ref shData);
                    else
                        state = GetSHNearest(volume, worldPosition, ref shData);

                    //Found voxel hit, return true
                    if ((int)state > -1)
                        return true;

                    //Skip if there is a volume using an invisible state
                    if (state == VoxelState.Invisible) {
                        return false;
                    }
                }
            }
            return false;
        }

        static VoxelState GetSHTriLinear(ProbeVolume v, Vector3 worldPosition, ref float[] shData)
        {
            int voxelIndex = v.Grid.GetVoxelIndex(worldPosition);
            if (voxelIndex < 0)
                return VoxelState.Empty;

            VoxelState voxelState = GetVoxelState(v, voxelIndex);
            if (voxelState != VoxelState.Valid)
                return VoxelState.Empty;

            float[] weights = v.Grid.GetVoxelTriLinearWeight(worldPosition, voxelIndex);

            //Blend 4 regions
            var lerp1 = GetLerp(weights[0], v.GetSHData(voxelIndex, 0), v.GetSHData(voxelIndex, 1));
            var lerp2 = GetLerp(weights[0], v.GetSHData(voxelIndex, 4), v.GetSHData(voxelIndex, 5));
            var leftBlend = GetLerp(weights[1], lerp1, lerp2);

            //Blend 4 regions
            var lerp3 = GetLerp(weights[0], v.GetSHData(voxelIndex, 2), v.GetSHData(voxelIndex, 3));
            var lerp4 = GetLerp(weights[0], v.GetSHData(voxelIndex, 6), v.GetSHData(voxelIndex, 7));
            var rightBlend = GetLerp(weights[1], lerp3, lerp4);

            //Blend the 2 outputs into a final output
            var finalBlend = GetLerp(weights[2], leftBlend, rightBlend);
            shData = v.GetSHData(voxelIndex, 0);

            return VoxelState.Valid;
        }

        static float[] GetLerp(double weight, float[] blend1, float[] blend2)
        {
            float[] output = new float[27];
            for (int i = 0; i < 27; i++) {
                output[i] = Lerp(blend1[i], blend2[i], weight);
            }
            return output;
        }

        static float Lerp(float a, float b, double weight) {
            return (float)(a * (1 - weight) + b * weight);
        }

        static VoxelState GetSHNearest(ProbeVolume volume, Vector3 worldPosition, ref float[] shData)
        {
            int voxelIndex = volume.Grid.GetVoxelIndex(worldPosition);
            if (voxelIndex < 0)
                return VoxelState.Empty;

            VoxelState voxelState = GetVoxelState(volume, voxelIndex);
            if (voxelState != VoxelState.Valid)
                return voxelState;

            int nearestIndex = volume.Grid.GetNearestLocalProbeIndex(worldPosition, voxelIndex);
            if (nearestIndex >= 0)
            {
                uint dataIndex = volume.IndexBuffer.GetSHDataIndex(nearestIndex, 0);
                bool isValid = volume.IndexBuffer.IsIndexValueValid(dataIndex);
                if (isValid)
                {
                    shData = volume.DataBuffer.GetSHData((int)dataIndex);
                    return VoxelState.Valid;
                }
            }
            return VoxelState.Empty;
        }

        static VoxelState GetVoxelState(ProbeVolume v, int voxelIndex)
        {
            const int numDivide = 8;
            for (int i = 0; i < numDivide; i++)
            {
                uint dataIndex = v.IndexBuffer.GetSHDataIndex(voxelIndex, i);
                bool isEmpty = v.IndexBuffer.IsIndexValueEmpty(dataIndex);

                if (isEmpty)
                    Console.WriteLine($"isEmpty at {i} dataIndex {dataIndex}");

                if (isEmpty)
                    return VoxelState.Empty;

                bool isInvisible = v.IndexBuffer.IsIndexValueInvisible(dataIndex);
                if (isInvisible)
                    Console.WriteLine($"isInvisible at {i} dataIndex {dataIndex}");

                if (isInvisible)
                    break;

                //Data passed through 8 times with valid values
                if (i == numDivide - 1)
                    return VoxelState.Valid;
            }
            return VoxelState.Invisible;
        }

        public enum VoxelState : uint
        {
            Valid = 0,
            Empty = 0xfffffffd,
            Invisible = 0xfffffffe,
        }

    }
}
