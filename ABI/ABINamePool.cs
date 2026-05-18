using System;
using System.Text;
using ImGuiOverlay.DMA;

namespace ImGuiOverlay.ABI
{
    // /////////////////////////////////////////////////////////////////////////////////////
    //  ABI NAME POOL  ·  Reads FName strings from the game's encrypted GNames pool.
    //  Decrypt algorithm updated to match current build (key offset 0xA52D2DC).
    // /////////////////////////////////////////////////////////////////////////////////////

    public static class ABINamePool
    {
        private static readonly ulong _gNames = DmaMemory.Base + ABIOffsets.GNames;

        // XOR key is re-read each call so it tracks game patches without restart
        private static byte ReadKey() =>
            DmaMemory.Read<byte>(DmaMemory.Base + 0xA52D2DCul);

        public static string GetName(uint key)
        {
            try
            {
                uint   chunk  = key >> 16;
                ushort offset = (ushort)key;

                ulong poolChunk = DmaMemory.Read<ulong>(_gNames + ((ulong)(chunk + 2) * 8));
                ulong entry     = poolChunk + (ulong)(2 * offset);

                short header = DmaMemory.Read<short>(entry);
                int   len    = header >> 6;
                if (len <= 0 || len > 512) return string.Empty;

                byte[]? buf = DmaMemory.ReadBytes(entry + 2, (uint)len);
                if (buf == null) return string.Empty;

                byte xorKey = ReadKey();
                FNameDecrypt(buf, len, xorKey);
                return Encoding.ASCII.GetString(buf);
            }
            catch { return string.Empty; }
        }

        // /////////////////////////////////////////////////////////////////////////////////////
        //  Decrypt — direct C# port of the updated C++ fname_decrypt:
        //
        //  uint8_t xor_key = *(base + 0xA52D2DC);
        //  for each byte p[i]:
        //      dl  = ((xor_key >> 1) & 0x08) ^ xor_key
        //      cl  = dl ^ ((dl & 0x08) << 1)
        //      al  = cl
        //      al &= 0x10
        //      al ^= 0xEF
        //      al >>= 1          (logical shift — byte is unsigned)
        //      p[i] ^= al
        //      p[i] ^= cl
        // /////////////////////////////////////////////////////////////////////////////////////
        private static void FNameDecrypt(byte[] input, int nameLength, byte xorKey)
        {
            if (nameLength > input.Length) nameLength = input.Length;

            byte dl = (byte)(((xorKey >> 1) & 0x08) ^ xorKey);
            byte cl = (byte)(dl ^ ((dl & 0x08) << 1));

            byte al = cl;
            al &= 0x10;
            al ^= 0xEF;
            al >>= 1;   // byte is unsigned — this is a logical (zero-filling) right shift

            // al and cl are constant across the whole name, so compute once
            byte mask = (byte)(al ^ cl);   // p[i] ^= al; p[i] ^= cl  ==  p[i] ^= (al ^ cl)

            for (int i = 0; i < nameLength; ++i)
                input[i] ^= mask;
        }
    }
}
