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

    public struct GenericFieldShort3 : ICoherenceComponentData
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct Interop
        {
            [FieldOffset(0)]
            public System.Int16 number;
        }

        public void ResetFrame(AbsoluteSimulationFrame frame)
        {
            FieldsMask |= GenericFieldShort3.numberMask;
            numberSimulationFrame = frame;
        }

        public static unsafe GenericFieldShort3 FromInterop(IntPtr data, Int32 dataSize, InteropAbsoluteSimulationFrame* simFrames, Int32 simFramesCount)
        {
            if (dataSize != 2) {
                throw new Exception($"Given data size is not equal to the struct size. ({dataSize} != 2) " +
                    "for component with ID 116");
            }

            if (simFramesCount != 1) {
                throw new Exception($"Given simFrames size is not equal to the expected length. ({simFramesCount} != 1) " +
                    "for component with ID 116");
            }

            var orig = new GenericFieldShort3();

            var comp = (Interop*)data;

            orig.number = comp->number;
            orig.numberSimulationFrame = simFrames[0].Into();

            return orig;
        }


        public static uint numberMask => 0b00000000000000000000000000000001;
        public AbsoluteSimulationFrame numberSimulationFrame;
        public System.Int16 number;

        public uint FieldsMask { get; set; }
        public uint StoppedMask { get; set; }
        public uint GetComponentType() => 116;
        public int PriorityLevel() => 100;
        public const int order = 0;
        public uint InitialFieldsMask() => 0b00000000000000000000000000000001;
        public bool HasFields() => true;
        public bool HasRefFields() => false;

        private long[] simulationFrames;

        public long[] GetSimulationFrames() {
            if (simulationFrames == null)
            {
                simulationFrames = new long[1];
            }

            simulationFrames[0] = numberSimulationFrame;

            return simulationFrames;
        }

        public int GetFieldCount() => 1;


        
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

            if ((FieldsMask & GenericFieldShort3.numberMask) != 0 && (min == null || this.numberSimulationFrame < min))
            {
                min = this.numberSimulationFrame;
            }

            return min;
        }

        public ICoherenceComponentData MergeWith(ICoherenceComponentData data)
        {
            var other = (GenericFieldShort3)data;
            var otherMask = other.FieldsMask;

            FieldsMask |= otherMask;
            StoppedMask &= ~(otherMask);

            if ((otherMask & 0x01) != 0)
            {
                this.numberSimulationFrame = other.numberSimulationFrame;
                this.number = other.number;
            }

            otherMask >>= 1;
            StoppedMask |= other.StoppedMask;

            return this;
        }

        public uint DiffWith(ICoherenceComponentData data)
        {
            throw new System.NotSupportedException($"{nameof(DiffWith)} is not supported in Unity");
        }

        public static uint Serialize(GenericFieldShort3 data, bool isRefSimFrameValid, AbsoluteSimulationFrame referenceSimulationFrame, IOutProtocolBitStream bitStream, Logger logger)
        {
            if (bitStream.WriteMask(data.StoppedMask != 0))
            {
                bitStream.WriteMaskBits(data.StoppedMask, 1);
            }

            var mask = data.FieldsMask;

            if (bitStream.WriteMask((mask & 0x01) != 0))
            {
                if (isRefSimFrameValid) {
                    var simFrameDelta = data.numberSimulationFrame - referenceSimulationFrame;
                    if (simFrameDelta > byte.MaxValue) {
                        simFrameDelta = byte.MaxValue;
                    }

                    SerializeTools.WriteFieldSimFrameDelta(bitStream, (byte)simFrameDelta);
                } else {
                    SerializeTools.WriteFieldSimFrameDelta(bitStream, 0);
                }


                var fieldValue = data.number;



                bitStream.WriteShort(fieldValue);
            }

            mask >>= 1;

            return mask;
        }

        public static GenericFieldShort3 Deserialize(AbsoluteSimulationFrame referenceSimulationFrame, InProtocolBitStream bitStream)
        {
            var stoppedMask = (uint)0;
            if (bitStream.ReadMask())
            {
                stoppedMask = bitStream.ReadMaskBits(1);
            }

            var val = new GenericFieldShort3();
            if (bitStream.ReadMask())
            {
                val.numberSimulationFrame = referenceSimulationFrame + DeserializerTools.ReadFieldSimFrameDelta(bitStream);

                val.number = bitStream.ReadShort();
                val.FieldsMask |= GenericFieldShort3.numberMask;
            }

            val.StoppedMask = stoppedMask;

            return val;
        }


        public override string ToString()
        {
            return $"GenericFieldShort3(" +
                $" number: { this.number }" +
                $", numberSimFrame: { this.numberSimulationFrame }" +
                $" Mask: { System.Convert.ToString(FieldsMask, 2).PadLeft(1, '0') }, " +
                $"Stopped: { System.Convert.ToString(StoppedMask, 2).PadLeft(1, '0') })";
        }
    }

}
