using System;
using System.Runtime.InteropServices;

namespace obs_net {
	using obs_source_t = IntPtr;
	using obs_data_t = IntPtr;

	public partial class Obs {
		[DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
		public static extern string obs_source_get_display_name(
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string id);

		[DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
		public static extern obs_source_t obs_source_create(
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string id,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string name,
			obs_data_t settings, obs_data_t hotkey_data);
	}
}
