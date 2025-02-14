// Copyright (c) coherence ApS.
// For all coherence generated code, the coherence SDK license terms apply. See the license file in the coherence Package root folder for more information.

// <auto-generated>
// Generated file. DO NOT EDIT!
// </auto-generated>
namespace Coherence.Generated
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections.Generic;
    using Coherence.ProtocolDef;
    using Coherence.Serializer;
    using Coherence.SimulationFrame;
    using Coherence.Entities;
    using Coherence.Utils;
    using Coherence.Brook;
    using Coherence.Core;
    using Logger = Coherence.Log.Logger;
    using UnityEngine;
    using Coherence.Toolkit;

    public struct AssetId : ICoherenceComponentData
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct Interop
        {
            [FieldOffset(0)]
            public System.Int32 value;
            [FieldOffset(4)]
            public System.Byte isFromGroup;
        }

        public void ResetFrame(AbsoluteSimulationFrame frame)
        {
            FieldsMask |= AssetId.valueMask;
            valueSimulationFrame = frame;
            FieldsMask |= AssetId.isFromGroupMask;
            isFromGroupSimulationFrame = frame;
        }

        public static unsafe AssetId FromInterop(IntPtr data, Int32 dataSize, InteropAbsoluteSimulationFrame* simFrames, Int32 simFramesCount)
        {
            if (dataSize != 5) {
                throw new Exception($"Given data size is not equal to the struct size. ({dataSize} != 5) " +
                    "for component with ID 16");
            }

            if (simFramesCount != 0) {
                throw new Exception($"Given simFrames size is not equal to the expected length. ({simFramesCount} != 0) " +
                    "for component with ID 16");
            }

            var orig = new AssetId();

            var comp = (Interop*)data;

            orig.value = comp->value;
            orig.isFromGroup = comp->isFromGroup != 0;

            return orig;
        }


        public static uint valueMask => 0b00000000000000000000000000000001;
        public AbsoluteSimulationFrame valueSimulationFrame;
        public System.Int32 value;
        public static uint isFromGroupMask => 0b00000000000000000000000000000010;
        public AbsoluteSimulationFrame isFromGroupSimulationFrame;
        public System.Boolean isFromGroup;

        public uint FieldsMask { get; set; }
        public uint StoppedMask { get; set; }
        public uint GetComponentType() => 16;
        public int PriorityLevel() => 100;
        public const int order = 0;
        public uint InitialFieldsMask() => 0b00000000000000000000000000000011;
        public bool HasFields() => true;
        public bool HasRefFields() => false;


        public long[] GetSimulationFrames() {
            return null;
        }

        public int GetFieldCount() => 2;


        
        public HashSet<Entity> GetEntityRefs()
        {
            return default;
        }

        public uint ReplaceReferences(Entity fromEntity, Entity toEntity)
        {
            return 0;
        }
        
        public IEntityMapper.Error MapToAbsolute(IEntityMapper mapper)
        {
            return IEntityMapper.Error.None;
        }

        public IEntityMapper.Error MapToRelative(IEntityMapper mapper)
        {
            return IEntityMapper.Error.None;
        }

        public ICoherenceComponentData Clone() => this;
        public int GetComponentOrder() => order;
        public bool IsSendOrdered() => false;


        public AbsoluteSimulationFrame? GetMinSimulationFrame()
        {
            AbsoluteSimulationFrame? min = null;


            return min;
        }

        public ICoherenceComponentData MergeWith(ICoherenceComponentData data)
        {
            var other = (AssetId)data;
            var otherMask = other.FieldsMask;

            FieldsMask |= otherMask;
            StoppedMask &= ~(otherMask);

            if ((otherMask & 0x01) != 0)
            {
                this.valueSimulationFrame = other.valueSimulationFrame;
                this.value = other.value;
            }

            otherMask >>= 1;
            if ((otherMask & 0x01) != 0)
            {
                this.isFromGroupSimulationFrame = other.isFromGroupSimulationFrame;
                this.isFromGroup = other.isFromGroup;
            }

            otherMask >>= 1;
            StoppedMask |= other.StoppedMask;

            return this;
        }

        public uint DiffWith(ICoherenceComponentData data)
        {
            throw new System.NotSupportedException($"{nameof(DiffWith)} is not supported in Unity");
        }

        public static uint Serialize(AssetId data, bool isRefSimFrameValid, AbsoluteSimulationFrame referenceSimulationFrame, IOutProtocolBitStream bitStream, Logger logger)
        {
            if (bitStream.WriteMask(data.StoppedMask != 0))
            {
                bitStream.WriteMaskBits(data.StoppedMask, 2);
            }

            var mask = data.FieldsMask;

            if (bitStream.WriteMask((mask & 0x01) != 0))
            {


                var fieldValue = data.value;



                bitStream.WriteIntegerRange(fieldValue, 32, -2147483648);
            }

            mask >>= 1;
            if (bitStream.WriteMask((mask & 0x01) != 0))
            {


                var fieldValue = data.isFromGroup;



                bitStream.WriteBool(fieldValue);
            }

            mask >>= 1;

            return mask;
        }

        public static AssetId Deserialize(AbsoluteSimulationFrame referenceSimulationFrame, InProtocolBitStream bitStream)
        {
            var stoppedMask = (uint)0;
            if (bitStream.ReadMask())
            {
                stoppedMask = bitStream.ReadMaskBits(2);
            }

            var val = new AssetId();
            if (bitStream.ReadMask())
            {

                val.value = bitStream.ReadIntegerRange(32, -2147483648);
                val.FieldsMask |= AssetId.valueMask;
            }
            if (bitStream.ReadMask())
            {

                val.isFromGroup = bitStream.ReadBool();
                val.FieldsMask |= AssetId.isFromGroupMask;
            }

            val.StoppedMask = stoppedMask;

            return val;
        }


        public override string ToString()
        {
            return $"AssetId(" +
                $" value: { this.value }" +
                $" isFromGroup: { this.isFromGroup }" +
                $" Mask: { System.Convert.ToString(FieldsMask, 2).PadLeft(2, '0') }, " +
                $"Stopped: { System.Convert.ToString(StoppedMask, 2).PadLeft(2, '0') })";
        }
    }

}
