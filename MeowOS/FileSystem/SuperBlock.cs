﻿using System;
using System.IO;
using System.Linq;

namespace MeowOS.FileSystem
{
    public class SuperBlock : AbstractElement
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
        public string FsTypeWithoutZeros => UsefulThings.truncateZeros(fsType);
        //Размер лог. блока - 2 б
        private ushort clusterSize;
        public ushort ClusterSize
        {
            get => clusterSize;
            set => clusterSize = value;
        }

        //Размер корневого каталога - 2 б
        private ushort rootSize;
        public ushort RootSize
        {
            get => rootSize;
            set => rootSize = value;
        }

        //Размер раздела - 4 б
        private uint diskSize;
        public uint DiskSize
        {
            get => diskSize;
            set => diskSize = value;
        }

        //Смещение FAT1 - 2 б
        private ushort fat1Offset;
        public ushort Fat1Offset
        {
            get => fat1Offset;
            set => fat1Offset = value;
        }

        //Смещение FAT2 - 2 б
        private ushort fat2Offset;
        public ushort Fat2Offset
        {
            get => fat2Offset;
            set => fat2Offset = value;
        }

        //Смещение корневого каталога - 4 б
        private uint rootOffset;
        public uint RootOffset
        {
            get => rootOffset;
            set => rootOffset = value;
        }

        //Смещение области данных - 4 б
        private uint dataOffset;
        public uint DataOffset
        {
            get => dataOffset;
            set => dataOffset = value;
        }

        public SuperBlock(FileSystemController fsctrl, Stream input)
        {
            this.fsctrl = fsctrl;
            fromByteStream(input);
        }

        public SuperBlock(FileSystemController fsctrl, byte[] src)
        {
            this.fsctrl = fsctrl;
            fromByteArray(src);
        }

        public SuperBlock(FileSystemController fsctrl, string fsType, ushort clusterSize, ushort rootSize, uint diskSize)
        {
            this.fsctrl = fsctrl;
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
            byte[] buffer = new byte[expandToCluster ? clusterSize : SIZE];
            int offset = 0;
            Buffer.BlockCopy(UsefulThings.ENCODING.GetBytes(fsType), 0, buffer, offset, FS_TYPE_MAX_LENGTH);
            offset += FS_TYPE_MAX_LENGTH;
            Buffer.BlockCopy(BitConverter.GetBytes(clusterSize), 0, buffer, offset, sizeof(ushort));
            offset += sizeof(ushort);
            Buffer.BlockCopy(BitConverter.GetBytes(rootSize), 0, buffer, offset, sizeof(ushort));
            offset += sizeof(ushort);
            Buffer.BlockCopy(BitConverter.GetBytes(diskSize), 0, buffer, offset, sizeof(uint));
            offset += sizeof(uint);
            Buffer.BlockCopy(BitConverter.GetBytes(fat1Offset), 0, buffer, offset, sizeof(ushort));
            offset += sizeof(ushort);
            Buffer.BlockCopy(BitConverter.GetBytes(fat2Offset), 0, buffer, offset, sizeof(ushort));
            offset += sizeof(ushort);
            Buffer.BlockCopy(BitConverter.GetBytes(rootOffset), 0, buffer, offset, sizeof(uint));
            offset += sizeof(uint);
            Buffer.BlockCopy(BitConverter.GetBytes(dataOffset), 0, buffer, offset, sizeof(uint));
            return buffer;
        }

        public override void fromByteArray(byte[] buffer)
        {
            int offset = 0;
            FsType = UsefulThings.ENCODING.GetString(buffer, offset, FS_TYPE_MAX_LENGTH);
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
