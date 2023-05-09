using System.Runtime.InteropServices;

namespace obs_net {
    public partial class Obs {
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern long bnum_allocs();
    }
}
