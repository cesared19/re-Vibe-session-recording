// Only in the Editor (the window and tests): the headset never talks to SparkyTC.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XRSessionCapture.SparkyTC
{
    /// <summary>
    /// A small .zip writer that stores its files without compressing them. It exists because
    /// System.IO.Compression.ZipArchive isn't available to code compiled with Unity's .NET Standard 2.0 profile
    /// (error CS1069: the type is forwarded to an assembly that isn't referenced). A replay's files are mostly PNG
    /// images, which don't shrink anyway. Zip64 isn't supported: up to 65535 files and 4 GB.
    /// </summary>
    public sealed class SimpleZip : IDisposable
    {
        const uint LocalHeader = 0x04034b50;
        const uint CentralHeader = 0x02014b50;
        const uint EndRecord = 0x06054b50;
        const ushort Version = 20;
        // bit 11: names are UTF-8
        const ushort Utf8Flag = 0x0800;
        const long MaxSize = 0xFFFFFFFEL;

        sealed class Entry
        {
            public byte[] name;
            public uint crc;
            public uint size;
            public long offset;
            public ushort date;
            public ushort time;
        }

        static readonly uint[] s_CrcTable = BuildCrcTable();

        readonly Stream m_Stream;
        readonly BinaryWriter m_Writer;
        readonly List<Entry> m_Entries = new List<Entry>();
        readonly HashSet<string> m_Names = new HashSet<string>(StringComparer.Ordinal);
        bool m_Finished;

        /// <summary>Writes into <paramref name="stream"/>, which stays open and belongs to the caller.</summary>
        public SimpleZip(Stream stream)
        {
            m_Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            m_Writer = new BinaryWriter(stream, new UTF8Encoding(false), true);
        }

        public void AddFile(string entryName, string path)
        {
            AddBytes(entryName, File.ReadAllBytes(path), File.GetLastWriteTime(path));
        }

        public void AddBytes(string entryName, byte[] data, DateTime lastWriteLocalTime)
        {
            if (m_Finished)
                throw new InvalidOperationException("The zip is already finished.");
            var name = Normalize(entryName);
            if (!m_Names.Add(name))
                throw new ArgumentException("The zip already has an entry named " + name, nameof(entryName));
            var nameBytes = Encoding.UTF8.GetBytes(name);
            if (nameBytes.Length > ushort.MaxValue)
                throw new ArgumentException("The entry name is too long: " + name, nameof(entryName));
            if (m_Entries.Count >= ushort.MaxValue)
                throw new InvalidOperationException("A zip without Zip64 holds up to 65535 files.");
            if (m_Stream.Position + data.LongLength + 4096 + nameBytes.Length > MaxSize)
                throw new InvalidOperationException("A zip without Zip64 can be up to 4 GB.");

            var entry = new Entry { name = nameBytes, crc = Crc32(data), size = (uint)data.Length, offset = m_Stream.Position };
            DosDateTime(lastWriteLocalTime, out entry.date, out entry.time);

            m_Writer.Write(LocalHeader);
            m_Writer.Write(Version);
            m_Writer.Write(Utf8Flag);
            m_Writer.Write((ushort)0); // method 0: stored
            m_Writer.Write(entry.time);
            m_Writer.Write(entry.date);
            m_Writer.Write(entry.crc);
            m_Writer.Write(entry.size); // compressed size
            m_Writer.Write(entry.size); // uncompressed size
            m_Writer.Write((ushort)nameBytes.Length);
            m_Writer.Write((ushort)0); // extra field length
            m_Writer.Write(nameBytes);
            m_Writer.Write(data);
            m_Entries.Add(entry);
        }

        /// <summary>Writes the table of contents. Disposing does it too, once.</summary>
        public void Finish()
        {
            if (m_Finished)
                return;
            m_Finished = true;
            var start = m_Stream.Position;
            foreach (var entry in m_Entries)
            {
                m_Writer.Write(CentralHeader);
                m_Writer.Write(Version); // made by
                m_Writer.Write(Version); // needed to extract
                m_Writer.Write(Utf8Flag);
                m_Writer.Write((ushort)0);
                m_Writer.Write(entry.time);
                m_Writer.Write(entry.date);
                m_Writer.Write(entry.crc);
                m_Writer.Write(entry.size);
                m_Writer.Write(entry.size);
                m_Writer.Write((ushort)entry.name.Length);
                m_Writer.Write((ushort)0); // extra field length
                m_Writer.Write((ushort)0); // comment length
                m_Writer.Write((ushort)0); // disk number
                m_Writer.Write((ushort)0); // internal attributes
                m_Writer.Write(0u); // external attributes
                m_Writer.Write((uint)entry.offset);
                m_Writer.Write(entry.name);
            }
            var size = m_Stream.Position - start;
            m_Writer.Write(EndRecord);
            m_Writer.Write((ushort)0); // this disk
            m_Writer.Write((ushort)0); // disk with the table of contents
            m_Writer.Write((ushort)m_Entries.Count);
            m_Writer.Write((ushort)m_Entries.Count);
            m_Writer.Write((uint)size);
            m_Writer.Write((uint)start);
            m_Writer.Write((ushort)0); // comment length
            m_Writer.Flush();
        }

        public void Dispose()
        {
            Finish();
            m_Writer.Dispose();
        }

        static string Normalize(string entryName)
        {
            var name = (entryName ?? string.Empty).Replace('\\', '/').TrimStart('/');
            if (name.Length == 0)
                throw new ArgumentException("An entry needs a name.", nameof(entryName));
            return name;
        }

        public static uint Crc32(byte[] data)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var b in data)
                crc = s_CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            return ~crc;
        }

        static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                var c = i;
                for (var k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[i] = c;
            }
            return table;
        }

        // The zip format keeps local time with two-second resolution, from 1980 to 2107.
        static void DosDateTime(DateTime t, out ushort date, out ushort time)
        {
            if (t.Year < 1980)
                t = new DateTime(1980, 1, 1, 0, 0, 0);
            else if (t.Year > 2107)
                t = new DateTime(2107, 12, 31, 23, 59, 58);
            date = (ushort)(((t.Year - 1980) << 9) | (t.Month << 5) | t.Day);
            time = (ushort)((t.Hour << 11) | (t.Minute << 5) | (t.Second / 2));
        }
    }
}
#endif
