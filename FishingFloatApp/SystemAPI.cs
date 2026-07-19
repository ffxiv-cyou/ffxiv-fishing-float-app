using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FishingFloatApp
{
    class SystemAPI
    {

        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string sClass, string sWindow);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess,
            IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, int dwSize, int flNewProtect, out int lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In] byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, [Out] StringBuilder lpExeName, ref int lpdwSize);

        public const uint PROCESS_VM_OPERATION = 0x8;
        public const uint PROCESS_VM_READ = 0x10;
        public const uint PROCESS_VM_WRITE = 0x20;
        public const uint PROCESS_DUP_HANDLE = 0x40;
        public const uint PROCESS_QUERY_INFORMATION = 0x400;
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public int AllocationProtect;
            public Int16 PartitionId;
            public ulong RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        public const int MEM_COMMIT = 0x1000;
        public const int MEM_FREE = 0x10000;
        public const int MEM_RESERVE = 0x2000;

        public const int MEM_IMAGE = 0x1000000;
        public const int MEM_MAPPED = 0x40000;
        public const int MEM_PRIVATE = 0x20000;

        public const int PAGE_EXECUTE = 0x10;
        public const int PAGE_READWRITE = 0x04;
    }
}