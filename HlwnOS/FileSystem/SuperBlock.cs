﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HlwnOS.FileSystem
{
    class SuperBlock : AbstractElement
    {
        public const int SIZE = FS_TYPE_MAX_LENGTH + sizeof(ushort) + sizeof(ushort) + sizeof(uint) + sizeof(ushort) + sizeof(ushort) + sizeof(uint) + sizeof(uint);

        //Тип ФС - 6 б
        public const int FS_TYPE_MAX_LENGTH = 6;
        private string fsType;
        public string FsType
        {
            get { return fsType; }
            private set { fsType = UsefulThings.setStringLength(value, FS_TYPE_MAX_LENGTH, '\0', UsefulThings.Alignments.LEFT); }
        }
        //Размер лог. блока - 2 б
        private ushort clusterSize;
        public ushort ClusterSize
        {
            get { return clusterSize; }
            private set { clusterSize = value; }
        }
        //Размер корневого каталога - 2 б
        private ushort rootSize;
        public ushort RootSize
        {
            get { return rootSize; }
            private set { rootSize = value; }
        }
        //Размер раздела - 4 б
        private uint diskSize;
        public uint DiskSize
        {
            get { return diskSize; }
            private set { diskSize = value; }
        }
        //Смещение FAT1 - 2 б
        private ushort fat1Offset;
        public ushort Fat1Offset
        {
            get { return fat1Offset; }
            private set { fat1Offset = value; }
        }
        //Смещение FAT2 - 2 б
        private ushort fat2Offset;
        public ushort Fat2Offset
        {
            get { return fat2Offset; }
            private set { fat2Offset = value; }
        }
        //Смещение корневого каталога - 4 б
        private uint rootOffset;
        public uint RootOffset
        {
            get { return rootOffset; }
            private set { rootOffset = value; }
        }
        //Смещение области данных - 4 б
        private uint dataOffset;
        public uint DataOffset
        {
            get { return dataOffset; }
            private set { dataOffset = value; }
        }

        public SuperBlock(Controller ctrl, Stream input)
        {
            this.ctrl = ctrl;
            fromByteStream(input);
        }

        public SuperBlock(Controller ctrl, byte[] src)
        {
            this.ctrl = ctrl;
            fromByteArray(src);
        }

        public SuperBlock(Controller ctrl, string fsType, ushort clusterSize, ushort rootSize, uint diskSize)
        {
            this.ctrl = ctrl;
            FsType = fsType;
            ClusterSize = clusterSize;
            RootSize = rootSize;
            DiskSize = diskSize;
            Fat1Offset = clusterSize;
            Fat2Offset = (ushort)(fat1Offset + clusterSize * Math.Ceiling((double)(FAT.ELEM_SIZE * diskSize / clusterSize) / clusterSize));
            RootOffset = (uint)(fat2Offset + clusterSize * Math.Ceiling((double)(FAT.ELEM_SIZE * diskSize / clusterSize) / clusterSize));
            DataOffset = (uint)(rootOffset + rootSize);
        }

        public override byte[] toByteArray(bool expandToCluster)
        {
            ArrayList buffer = new ArrayList(expandToCluster ? clusterSize : SIZE);

            buffer.AddRange(Encoding.ASCII.GetBytes(fsType));
            buffer.AddRange(BitConverter.GetBytes(clusterSize));
            buffer.AddRange(BitConverter.GetBytes(rootSize));
            buffer.AddRange(BitConverter.GetBytes(diskSize));
            buffer.AddRange(BitConverter.GetBytes(fat1Offset));
            buffer.AddRange(BitConverter.GetBytes(fat2Offset));
            buffer.AddRange(BitConverter.GetBytes(rootOffset));
            buffer.AddRange(BitConverter.GetBytes(dataOffset));

            if (expandToCluster)
                buffer.AddRange(Encoding.ASCII.GetBytes(new String('\0', clusterSize - SIZE).ToArray()));

            return buffer.OfType<byte>().ToArray();
        }

        public override void fromByteArray(byte[] buffer)
        {
            int offset = 0;
            FsType = Encoding.ASCII.GetString(buffer, offset, FS_TYPE_MAX_LENGTH);
            offset = FS_TYPE_MAX_LENGTH;
            ClusterSize = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);
            RootSize = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);
            DiskSize = BitConverter.ToUInt32(buffer, offset);
            offset += sizeof(uint);
            Fat1Offset = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);
            Fat2Offset = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);
            RootOffset = BitConverter.ToUInt32(buffer, offset);
            offset += sizeof(uint);
            DataOffset = BitConverter.ToUInt32(buffer, offset);
            offset += sizeof(uint);
        }

        public override void fromByteStream(Stream input)
        {
            BinaryReader br = new BinaryReader(input);
            fromByteArray(br.ReadBytes(SIZE));
        }

        public override string ToString()
        {
            return "FS type: " + fsType +
                "\nCluster size: " + clusterSize +
                "\nRoot size: " + rootSize +
                "\nDist size: " + diskSize +
                "\nFAT1 offset: " + fat1Offset +
                "\nFAT2 offset: " + fat2Offset +
                "\nRoot offset: " + rootOffset +
                "\nData offset: " + dataOffset;
        }
    }
}
