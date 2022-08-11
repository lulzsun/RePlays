using System;
using System.Runtime.InteropServices;

namespace obs_net {
    using signal_handler_t = IntPtr;
    using calldata_t = IntPtr;

    public partial class Obs {
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern long bnum_allocs();
    }
}
