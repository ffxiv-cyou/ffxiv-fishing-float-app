using Microsoft.Extensions.Logging;
using System;

namespace FishingFloatApp.memory
{
    class MemoryScanner
    {
        ILogger Logger { get; }
        SigScanner sigScanner { get; }
        public MemoryScanner(ILogger logger, SigScanner sigScanner)
        {
            this.sigScanner = sigScanner;
            this.Logger = logger;
        }

        public void Init()
        {
            if (sigScanner == null || !sigScanner.Opened)
                return;

            SearchNetworkingModule();
        }

        IntPtr NetworkModulePtr { get; set; }

        public bool NetworkConnected { get; private set; } = false;

        public void SearchNetworkingModule()
        {
            var pFrameworkBytes = sigScanner.FindSignature("48 8B 1D ?? ?? ?? ?? 8B 7C 24");
            if (!pFrameworkBytes.Valid) 
                throw new Exception("failed to find Framework instance");

            var pFrameworkAddr = pFrameworkBytes.GetRelative(0);
            var pFramework = sigScanner.GetIntPtr(pFrameworkAddr);
            var pNetworkModuleProxy = sigScanner.GetIntPtr(pFramework, 0x1678);
            NetworkModulePtr = sigScanner.GetIntPtr(pNetworkModuleProxy, 0x8);

            Logger.LogInformation("Network module pointer: {pNetworkModule:X8}, text base: {pText:X8}", NetworkModulePtr.ToInt64(), sigScanner.TextBase.ToInt64());

            var pZoneClient = sigScanner.GetIntPtr(NetworkModulePtr, 0xA70);
            var pChatClient = sigScanner.GetIntPtr(NetworkModulePtr, 0xA78);

            if (pZoneClient == IntPtr.Zero || pChatClient == IntPtr.Zero)
            {
                NetworkConnected = false;
                return;
            }

            NetworkConnected = true;

            //var pZoneConnection = sigScanner.GetIntPtr(pZoneClient, 0x98); // astruct_8*
            //var pSession = sigScanner.GetIntPtr(pZoneConnection, 0x10); // SessionBase

            //var pSessionTx = pSession + 0x190;
            //var pSessionRx = pSession + 0x200;
        }

        IntPtr GetZoneSession()
        {
            var pZoneClient = sigScanner.GetIntPtr(NetworkModulePtr, 0xA70);
            if (pZoneClient == IntPtr.Zero)
                return IntPtr.Zero;

            var pZoneConnection = sigScanner.GetIntPtr(pZoneClient, 0x98); // astruct_8*
            if (pZoneConnection == IntPtr.Zero)
                return IntPtr.Zero;

            var pSession = sigScanner.GetIntPtr(pZoneConnection, 0x10); // SessionBase
            return pSession;
        }

        IntPtr GetChatSession()
        {
            var pChatClient = sigScanner.GetIntPtr(NetworkModulePtr, 0xA78);
            if (pChatClient == IntPtr.Zero)
                return IntPtr.Zero;

            var pChatConnection = sigScanner.GetIntPtr(pChatClient, 0xa0); // astruct_8*
            if (pChatConnection == IntPtr.Zero)
                return IntPtr.Zero;

            var pSession = sigScanner.GetIntPtr(pChatConnection, 0x10); // SessionBase
            return pSession;
        }

        public ClientState? GetZoneState(OodleSizes size)
        {
            var pSession = GetZoneSession();
            if (pSession == IntPtr.Zero)
                return null;

            return GetOodleState(pSession, size);
        }

        public ClientState? GetChatState(OodleSizes size)
        {
            var pSession = GetChatSession();
            if (pSession == IntPtr.Zero)
                return null;

            return GetOodleState(pSession, size);
        }

        ClientState GetOodleState(IntPtr pSession, OodleSizes size)
        {
            var tx = pSession + 0x190;
            var rx = pSession + 0x200;

            return new ClientState()
            {
                Tx = handleNetworkSession(tx, size),
                Rx = handleNetworkSession(rx, size)
            };
        }

        OodleState handleNetworkSession(IntPtr ptr, OodleSizes size)
        {
            var result = new OodleState(size);

            var pShared = sigScanner.GetIntPtr(ptr, 0x28);
            var pRootState = sigScanner.GetIntPtr(ptr, 0x30);
            var counter = sigScanner.GetInt32(ptr, 0x38);
            var pTreeNode = sigScanner.GetIntPtr(ptr, 0x40);
            var pWindow = sigScanner.GetIntPtr(ptr, 0x58);
            var pTcp = sigScanner.GetIntPtr(pRootState, 0x0);
            var pUdp = sigScanner.GetIntPtr(pRootState, 0x8);

            Logger.LogInformation("Network session: {0:X8}, state count: {counter}, pState: {pTcp:X8}, pShared: {pShared:X8}, pWindow: {pWindow:X8}", ptr.ToInt64(), counter, pTcp.ToInt64(), pShared.ToInt64(),  pWindow.ToInt64());

            var pWindowStart = sigScanner.GetIntPtr(pShared, 0);
            var pWindowEnd = sigScanner.GetIntPtr(pShared, 8);

            if (pWindowStart != pWindow || pWindow + 0x100000 != pWindowEnd)
                Logger.LogWarning("Oodle shared data first 16 bytes not match. {pWindow:X8}, {pWindowStart:X8}, {pWindowEnd:X8}", pWindow, pWindowStart, pWindowEnd);

            sigScanner.GetBytes(pShared, result.Shared);
            sigScanner.GetBytes(pWindow, result.Window);
            sigScanner.GetBytes(pTcp, result.State);

            return result;
        }

        public struct OodleSizes
        {
            public int StateSize;
            public int SharedSize;
            public int WindowSize;
        }
    
        public struct OodleState
        {
            public byte[] State;
            public byte[] Shared;
            public byte[] Window;

            public OodleState(OodleSizes size) : this(size.StateSize, size.SharedSize, size.WindowSize)
            {
            }

            public OodleState(int stateSize, int sharedSize, int windowSize)
            {
                State = new byte[stateSize];
                Shared = new byte[sharedSize];
                Window = new byte[windowSize];
            }
        }

        public struct ClientState
        {
            public OodleState Tx;
            public OodleState Rx;
        }
    }

}
