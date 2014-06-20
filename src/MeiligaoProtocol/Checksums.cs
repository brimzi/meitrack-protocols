using System;
namespace MeiligaoProtocol
{
    public enum InitialCrcValue { Zeros, NonZero1 = 0xffff, NonZero2 = 0x1D0F }
    internal class Crc16Ccitt
    {
        const ushort poly = 4129;
        ushort[] table = new ushort[256];
        ushort initialValue = 0;

        internal int ComputeChecksum(byte[] bytes)
        {
            ushort crc = this.initialValue;
            for (int i = 0; i < bytes.Length; ++i)
            {
                crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & bytes[i]))]);
            }
            return crc;
        }

        internal byte[] ComputeChecksumBytes(byte[] bytes)
        {
            var crc = ComputeChecksum(bytes);
            return BitConverter.GetBytes((short)crc);
        }

        internal Crc16Ccitt(InitialCrcValue initialValue)
        {
            this.initialValue = (ushort)initialValue;
            ushort temp, a;
            for (int i = 0; i < table.Length; ++i)
            {
                temp = 0;
                a = (ushort)(i << 8);
                for (int j = 0; j < 8; ++j)
                {
                    if (((temp ^ a) & 0x8000) != 0)
                    {
                        temp = (ushort)((temp << 1) ^ poly);
                    }
                    else
                    {
                        temp <<= 1;
                    }
                    a <<= 1;
                }
                table[i] = temp;
            }
        }

        internal byte[] ComputeChecksumBytes(byte[] bytes, int startIndex, int count)
        {
            var crc = ComputeChecksum(bytes,startIndex,count);
            return BitConverter.GetBytes((short)crc);
        }

        private int ComputeChecksum(byte[] bytes, int startIndex, int count)
        {
            ushort crc = this.initialValue;

            for (int i = startIndex; i < startIndex+count; ++i)
            {
                crc = (ushort)((crc << 8) ^ table[((crc >> 8) ^ (0xff & bytes[i]))]);
            }
            return crc;
        }
    }

    public class ChecksumUtility
    {
        private static Crc16Ccitt _Crc16Ccitt = new Crc16Ccitt(InitialCrcValue.NonZero1);
        public static byte[] ComputeMeiligaoChecksum(byte[] bytes)
        {
            var chec = _Crc16Ccitt.ComputeChecksumBytes(bytes);
            if(BitConverter.IsLittleEndian)
            Array.Reverse(chec);
            return chec;
        }

        public static byte[] ComputeMeiligaoChecksum(byte[] bytes,int startIndex,int count)
        {
            var chec = _Crc16Ccitt.ComputeChecksumBytes(bytes,startIndex,count);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(chec);
            return chec;
        }
    }


    public class TestClass
    {
        //public static void Main()
        //{

        //    var data = new byte[] { 0x24, 0x24, 0x00, 0x11, 0x13, 0x61, 0x23, 0x45, 0x67, 0x8f, 0xff, 0x50, 0x00 };
        //    //var data = new byte[] {};
        //    var c = ChecksumUtility.ComputeMeiligaoChecksum(data);
        //}
    }

}


