using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FishingFloatApp.memory
{
    internal class SigScanner
    {
        IntPtr ProcessPtr { get; set; }
        public IntPtr TextBase { get; private set; }
        public int ProcessID { get; private set; }
        byte[] TextSectionData { get; set; }
        int TextSize { get; set; }

        public bool Opened => ProcessPtr != IntPtr.Zero;

        public SigScanner(Process process)
        {
            Open(process);
        }

        public SigScanner()
        {
        }

        public static Process[] GetFFXIVProcesses()
        {
            return Process.GetProcessesByName("ffxiv_dx11");
        }

        public void Close()
        {
            if (ProcessPtr != IntPtr.Zero)
            {
                SystemAPI.CloseHandle(ProcessPtr);
                ProcessPtr = IntPtr.Zero;
            }
            TextBase = IntPtr.Zero;
            TextSize = 0;
            ProcessID = 0;
            TextSectionData = Array.Empty<byte>();
        }

        ~SigScanner()
        {
            Close();
        }

        public void Open(Process process)
        {
            if (ProcessPtr != IntPtr.Zero)
            {
                // check if process is the same
                if (process.Id != ProcessID)
                {
                    Close();
                }
            }

            ProcessID = process.Id;
            ProcessPtr = SystemAPI.OpenProcess(SystemAPI.PROCESS_VM_READ | SystemAPI.PROCESS_QUERY_INFORMATION, false, (uint)process.Id);
            
            var module = process.MainModule;
            TextBase = module.BaseAddress;
            TextSize = module.ModuleMemorySize;

            TextSectionData = ReadMemory(TextBase, TextSize);
        }

        public int GetBytes(IntPtr address, byte[] buffer)
        {
            if (ProcessPtr == IntPtr.Zero)
                return 0;

            SystemAPI.ReadProcessMemory(ProcessPtr, address, buffer, buffer.Length, out var size);
            return size.ToInt32();
        }

        byte[] ReadMemory(IntPtr address, int length)
        {
            if (ProcessPtr == IntPtr.Zero)
                return Array.Empty<byte>();

            var buffer = new byte[length];
            SystemAPI.ReadProcessMemory(ProcessPtr, address, buffer, length, out var size);

#if DEBUG
            if (size.ToInt64() != length)
            {
                var error = Marshal.GetLastWin32Error();
                throw new Exception($"Failed to read memory at address {address}. Error code: {error}");
            }
#endif
            return buffer;
        }

        #region QuickAccess

        /// <summary>
        /// Convert byte array to object
        /// </summary>
        /// <param name="t"></param>
        /// <param name="bytes"></param>
        /// <see cref="https://stackoverflow.com/a/2887"/>
        /// <returns></returns>
        public object ByteArrayToStructure(Type t, byte[] bytes)
        {
            object stuff;
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                stuff = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), t);
            }
            finally
            {
                handle.Free();
            }
            return stuff;
        }

        public T Get<T>(IntPtr address, int offset = 0) where T : struct
        {
            if (ProcessPtr == IntPtr.Zero)
                return default(T);

            var len = Marshal.SizeOf(typeof(T));
            var buffer = new byte[len];
            SystemAPI.ReadProcessMemory(ProcessPtr, IntPtr.Add(address, offset), buffer, len, out _);
            return (T)ByteArrayToStructure(typeof(T), buffer);
        }
        public char GetChar(IntPtr address, int offset = 0) => (char)ReadMemory(IntPtr.Add(address, offset), 1)[0];
        public Int16 GetInt16(IntPtr address, int offset = 0) => BitConverter.ToInt16(ReadMemory(IntPtr.Add(address, offset), 2), 0);
        public Int32 GetInt32(IntPtr address, int offset = 0) => BitConverter.ToInt32(ReadMemory(IntPtr.Add(address, offset), 4), 0);
        public Int64 GetInt64(IntPtr address, int offset = 0) => BitConverter.ToInt64(ReadMemory(IntPtr.Add(address, offset), 8), 0);
        public float GetFloat(IntPtr address, int offset = 0) => BitConverter.ToSingle(ReadMemory(IntPtr.Add(address, offset), 4), 0);
        public double GetDouble(IntPtr address, int offset = 0) => BitConverter.ToDouble(ReadMemory(IntPtr.Add(address, offset), 8), 0);
        public byte GetByte(IntPtr address, int offset = 0) => ReadMemory(IntPtr.Add(address, offset), 1)[0];
        public byte[] GetBytes(IntPtr address, int length, int offset = 0) => ReadMemory(IntPtr.Add(address, offset), length);
        public UInt16 GetUInt16(IntPtr address, int offset = 0) => BitConverter.ToUInt16(ReadMemory(IntPtr.Add(address, offset), 2), 0);
        public UInt32 GetUInt32(IntPtr address, int offset = 0) => BitConverter.ToUInt32(ReadMemory(IntPtr.Add(address, offset), 4), 0);
        public UInt64 GetUInt64(IntPtr address, int offset = 0) => BitConverter.ToUInt64(ReadMemory(IntPtr.Add(address, offset), 8), 0);
        public IntPtr GetIntPtr(IntPtr address, int offset = 0) => new IntPtr(BitConverter.ToInt64(ReadMemory(IntPtr.Add(address, offset), 8), 0));
        public UIntPtr GetUIntPtr(IntPtr address, int offset = 0) => new UIntPtr(BitConverter.ToUInt64(ReadMemory(IntPtr.Add(address, offset), 8), 0));
        #endregion

        #region scanner

        public struct SigBytes
        {
            public byte[] Bytes { get; set; }
            public bool[] Mask { get; set; }

            public SigBytes(string sig)
            {
                var parts = sig.Split(' ');
                var bytes = new byte[parts.Length];
                var mask = new bool[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i] == "??")
                    {
                        bytes[i] = 0;
                        mask[i] = false;
                    }
                    else
                    {
                        bytes[i] = Convert.ToByte(parts[i], 16);
                        mask[i] = true;
                    }
                }
                this.Bytes = bytes;
                this.Mask = mask;
            }

            public bool Match(byte[] data, int offset)
            {
                for (int i = 0; i < Bytes.Length; i++)
                {
                    if (Mask[i] && data[offset + i] != Bytes[i])
                        return false;
                }
                return true;
            }

            public byte[][] GetMasked(byte[] data, int offset = 0)
            {
                var result = new List<byte[]>();
                for (int i = 0; i < Bytes.Length; i++)
                {
                    if (!Mask[i])
                    {
                        int j;
                        for (j = i; j < Bytes.Length && !Mask[j]; j++) ;

                        var span = new byte[j - i];
                        Array.Copy(data, offset + i, span, 0, j - i);
                        result.Add(span);
                        i = j;
                    }
                }
                return result.ToArray();
            }

            public (int, int) GetNthRange(int n)
            {
                int count = 0;
                for (int i = 0; i < Bytes.Length; i++)
                {
                    if (!Mask[i])
                    {
                        int j;
                        for (j = i; j < Bytes.Length && !Mask[j]; j++) ;

                        if (count == n)
                            return (i, j);

                        count++;
                        i = j;
                    }
                }
                return (0, 0);
            }

            public static byte[] GetRange(byte[] data, int start, int end)
            {
                return Slice(data, start, end - start);
            }

            public byte[] GetNthMasked(int n, byte[] data)
            {
                var (begin, end) = GetNthRange(n);
                if (begin == end)
                    return null;

                return GetRange(data, begin, end);
            }
        }

        public struct SigResult
        {
            public IntPtr Address { get; set; }
            public byte[] Bytes { get; set; }
            public SigBytes Sig { get; set; }

            public UInt32 GetUInt32(int index) => BitConverter.ToUInt32(Sig.GetNthMasked(index, Bytes), 0);

            public Int32 GetInt32(int index) => BitConverter.ToInt32(Sig.GetNthMasked(index, Bytes), 0);

            public Int16 GetInt16(int index) => BitConverter.ToInt16(Sig.GetNthMasked(index, Bytes), 0);

            public UInt16 GetUInt16(int index) => BitConverter.ToUInt16(Sig.GetNthMasked(index, Bytes), 0);

            public IntPtr GetRelative(int index)
            {
                var (begin, end) = Sig.GetNthRange(index);
                var offset = BitConverter.ToInt32(Slice(Bytes, begin, end - begin), 0);
                return IntPtr.Add(Address, offset + begin + 4);
            }

            public IntPtr GetCallAddress(int index)
            {
                return GetRelative(index);
            }

            public bool Valid => Address != IntPtr.Zero;
        }

        static byte[] Slice(byte[] data, int offset, int count)
        {
            var result = new byte[count];
            Array.Copy(data, offset, result, 0, count);
            return result;
        }

        /// <summary>
        /// 扫描内存中的字节序列，支持通配符 "??"
        /// </summary>
        /// <param name="sig"></param>
        /// <returns></returns>
        public SigResult FindSignature(string sig, int offset = 0)
        {
            if (ProcessPtr == IntPtr.Zero)
                return new SigResult();

            var sigBytes = new SigBytes(sig);
            for (int i = offset; i < TextSize - sigBytes.Bytes.Length; i++)
            {
                if (sigBytes.Match(TextSectionData, i))
                {
                    return new SigResult
                    {
                        Address = IntPtr.Add(TextBase, i),
                        Bytes = Slice(TextSectionData, i, sigBytes.Bytes.Length),
                        Sig = sigBytes
                    };
                }
            }
            return new SigResult();
        }

        public SigResult FindSignature(string sig, IntPtr searchStart)
        {
            var offset = (int)(searchStart.ToInt64() - TextBase.ToInt64());
            return FindSignature(sig, offset);
        }

        #endregion

        #region MemoryScanner

        public IntPtr ScanBytes(byte[] data, IntPtr start, int size)
        {
            if (ProcessPtr == IntPtr.Zero)
                return IntPtr.Zero;

            const int ChunkSize = 16 * 1024 * 1024;
            var buf = new byte[ChunkSize];

            int step = 1;
            if (data.Length % 2 == 0)
                step = 2;
            if (data.Length % 4 == 0)
                step = 4;
            if (data.Length % 8 == 0)
                step = 8;

            int offset = 0;
            while (offset < size)
            {
                int toRead = Math.Min(size - offset, ChunkSize);
                bool success = SystemAPI.ReadProcessMemory(ProcessPtr, IntPtr.Add(start, offset), buf, toRead, out var chunkSz);
                if (!success)
                {
                    offset += toRead;
                    continue;
                }

                // 比较数据
                for (int i = 0; i < toRead - data.Length; i += step)
                {
                    bool match = true;
                    for (int j = 0; j < data.Length; j++)
                    {
                        if (data[j] != buf[i + j])
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                        return IntPtr.Add(start, offset + i);
                }

                offset += toRead;
            }

            return IntPtr.Zero;
        }

        public IntPtr FindInMemory(byte[] data)
        {
            if (ProcessPtr == IntPtr.Zero)
                return IntPtr.Zero;

            IntPtr addr = IntPtr.Zero;
            long maxAddress = long.MaxValue;

            while ((long)addr < maxAddress)
            {
                var result = SystemAPI.VirtualQueryEx(ProcessPtr, addr, out var mbi, (uint)Marshal.SizeOf(typeof(SystemAPI.MEMORY_BASIC_INFORMATION)));
                if (result == 0)
                    return IntPtr.Zero;

                if (mbi.State == SystemAPI.MEM_COMMIT && mbi.Protect == SystemAPI.PAGE_READWRITE && mbi.Type == SystemAPI.MEM_PRIVATE)
                {
                    var ptr = ScanBytes(data, mbi.BaseAddress, (int)mbi.RegionSize);
                    if (ptr != IntPtr.Zero)
                        return ptr;
                }

                addr = new IntPtr(mbi.BaseAddress.ToInt64() + (long)mbi.RegionSize);
            }

            return IntPtr.Zero;
        }

        #endregion
    }
}
