using System;
using System.IO;
using System.Runtime.InteropServices;

namespace obs_net {
	using obs_source_t = IntPtr;
	using profiler_name_store_t = IntPtr;
	using video_t = IntPtr;
	using audio_t = IntPtr;
	public partial class Obs {

		public const string importLibrary = @"obs.dll";  //extension is handled automatically
		public const CallingConvention importCall = CallingConvention.Cdecl;
		public const CharSet importCharSet = CharSet.Ansi;

		/// <summary>
		/// <para>https://obsproject.com/docs/reference-core.html#c.obs_startup</para>
		/// <para>Initializes the OBS core context.</para>
		/// </summary>
		/// <param name="locale">The locale to use for modules (E.G. “en-US”)</param>
		/// <param name="module_config_path">Path to module config storage directory (or NULL if none)</param>
		/// <param name="store">The profiler name store for OBS to use or NULL</param>
		/// <returns>false if already initialized or failed to initialize</returns>
		public static bool obs_startup(string locale, string module_config_path, profiler_name_store_t store) {
			//Directory.SetCurrentDirectory(@"C:\Program Files\obs-studio\bin\64bit\");
			return obs_startup_call(locale, module_config_path, store);
		}
		[DllImport(importLibrary, EntryPoint = "obs_startup", CallingConvention = importCall, CharSet = importCharSet)]
		[return: MarshalAs(UnmanagedType.I1)]
		private static extern bool obs_startup_call(
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string locale,
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string module_config_path,
		profiler_name_store_t store);

		/// <summary>
		/// https://obsproject.com/docs/reference-core.html#c.obs_get_version_string
		/// </summary>
		/// <returns>The current core version string</returns>
		[DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
		[return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
		public static extern string obs_get_version_string();


		/// <summary>
		/// https://obsproject.com/docs/reference-core.html#c.obs_initialized
		/// </summary>
		/// <returns>true if the main OBS context has been initialized</returns>
		[DllImport(importLibrary, CallingConvention = importCall)]
		[return: MarshalAs(UnmanagedType.I1)]
		public static extern bool obs_initialized();

		/// <summary>
		/// <para>https://obsproject.com/docs/reference-core.html#c.obs_reset_video</para>
		/// <para>Sets base video output base resolution/fps/format.</para>
		/// <para>Note: This data cannot be changed if an output is currently active.</para>
		/// <para>Note: The graphics module cannot be changed without fully destroying the OBS context.</para>
		/// </summary>
		/// <param name="ovi">Pointer to an obs_video_info structure containing the specification of the graphics subsystem</param>
		/// <returns>
		/// <para>OBS_VIDEO_SUCCESS - Success</para>
		/// <para>OBS_VIDEO_NOT_SUPPORTED - The adapter lacks capabilities</para>
		/// <para>OBS_VIDEO_INVALID_PARAM - A parameter is invalid</para>
		/// <para>OBS_VIDEO_CURRENTLY_ACTIVE - Video is currently active</para>
		/// <para>OBS_VIDEO_MODULE_NOT_FOUND - The graphics module is not found</para>
		/// <para>OBS_VIDEO_FAIL - Generic failure</para>
		/// </returns>
		[DllImport(importLibrary, CallingConvention = importCall)]
		public static extern int obs_reset_video(ref obs_video_info ovi);

		/// <summary>
		/// <para>https://obsproject.com/docs/reference-core.html#c.obs_reset_audio</para>
		/// <para>Sets base audio output format/channels/samples/etc.</para>
		/// <para>Note: Cannot reset base audio if an output is currently active.</para>
		/// </summary>
		/// <param name="oai">Pointer to an obs_audio_info structure containing the specification of the audio subsystem</param>
		/// <returns>true if successful, false otherwise</returns>
		[DllImport(importLibrary, CallingConvention = importCall)]
		[return: MarshalAs(UnmanagedType.I1)]
		public static extern bool obs_reset_audio(ref obs_audio_info oai);

		[DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
		public static extern void obs_add_data_path(
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string path
		);

		[DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
		public static extern void obs_add_module_path(
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string bin, 
			[MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string data
		);

		[DllImport(importLibrary, CallingConvention = importCall)]
		public static extern void obs_log_loaded_modules();

		[DllImport(importLibrary, CallingConvention = importCall)]
		public static extern void obs_load_all_modules();

		[DllImport(importLibrary, CallingConvention = importCall)]
		public static extern void obs_post_load_modules();

		[StructLayout(LayoutKind.Sequential, CharSet = importCharSet)]
		public struct obs_video_info {
			public string graphics_module; //Marshal.PtrToStringAnsi

			public uint fps_num;       //Output FPS numerator
			public uint fps_den;       //Output FPS denominator

			public uint base_width;    //Base compositing width
			public uint base_height;   //Base compositing height

			public uint output_width;  //Output width
			public uint output_height; //Output height
			public video_format output_format; // Output format

			//Video adapter index to use (NOTE: avoid for optimus laptops)
			public uint adapter;

			//Use shaders to convert to different color formats

			[MarshalAs(UnmanagedType.I1)]
			public bool gpu_conversion;

			public video_colorspace colorspace;  //YUV type (if YUV)
			public video_range_type range;       //YUV range (if YUV)

			public obs_scale_type scale_type;    //How to scale if scaling
		};

		public enum obs_scale_type : int {
			OBS_SCALE_DISABLE,
			OBS_SCALE_POINT,
			OBS_SCALE_BICUBIC,
			OBS_SCALE_BILINEAR,
			OBS_SCALE_LANCZOS,
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct obs_audio_info {
			public uint samples_per_sec;
			public speaker_layout speakers;
		};

		private const int MAX_AV_PLANES = 8;

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct resample_info {
			private uint samples_per_sec;
			private audio_format format;
			private speaker_layout speakers;
		};

		[UnmanagedFunctionPointer(importCall, CharSet = importCharSet)]
		[return: MarshalAs(UnmanagedType.I1)]
		public delegate bool audio_input_callback_t(obs_source_t param, uint start_ts, uint end_ts, out uint new_ts, uint active_mixers, uint mixes);

		[StructLayout(LayoutKind.Sequential)]
		public unsafe struct audio_output_info {
			public string name;

			public uint samples_per_sec;
			public audio_format format;
			public speaker_layout speakers;
			public audio_input_callback_t input_callback;
			public void* input_param;
		};

		public enum video_format : int {
			VIDEO_FORMAT_NONE,

			/* planar 4:2:0 formats */
			VIDEO_FORMAT_I420, /* three-plane */
			VIDEO_FORMAT_NV12, /* two-plane, luma and packed chroma */

			/* packed 4:2:2 formats */
			VIDEO_FORMAT_YVYU,
			VIDEO_FORMAT_YUY2, /* YUYV */
			VIDEO_FORMAT_UYVY,

			/* packed uncompressed formats */
			VIDEO_FORMAT_RGBA,
			VIDEO_FORMAT_BGRA,
			VIDEO_FORMAT_BGRX,
			VIDEO_FORMAT_Y800, /* grayscale */

			/* planar 4:4:4 */
			VIDEO_FORMAT_I444,

			/* more packed uncompressed formats */
			VIDEO_FORMAT_BGR3,

			/* planar 4:2:2 */
			VIDEO_FORMAT_I422,

			/* planar 4:2:0 with alpha */
			VIDEO_FORMAT_I40A,

			/* planar 4:2:2 with alpha */
			VIDEO_FORMAT_I42A,

			/* planar 4:4:4 with alpha */
			VIDEO_FORMAT_YUVA,

			/* packed 4:4:4 with alpha */
			VIDEO_FORMAT_AYUV,

			/* planar 4:2:0 format, 10 bpp */
			VIDEO_FORMAT_I010, /* three-plane */
			VIDEO_FORMAT_P010, /* two-plane, luma and packed chroma */

			/* planar 4:2:2 10 bits */
			VIDEO_FORMAT_I210, // Little Endian

			/* planar 4:4:4 12 bits */
			VIDEO_FORMAT_I412, // Little Endian

			/* planar 4:4:4 12 bits with alpha */
			VIDEO_FORMAT_YA2L, // Little Endian
		};

		public enum audio_format : int {
			AUDIO_FORMAT_UNKNOWN,

			AUDIO_FORMAT_U8BIT,
			AUDIO_FORMAT_16BIT,
			AUDIO_FORMAT_32BIT,
			AUDIO_FORMAT_FLOAT,

			AUDIO_FORMAT_U8BIT_PLANAR,
			AUDIO_FORMAT_16BIT_PLANAR,
			AUDIO_FORMAT_32BIT_PLANAR,
			AUDIO_FORMAT_FLOAT_PLANAR,
		};

		public enum speaker_layout : int {
			SPEAKERS_UNKNOWN,
			SPEAKERS_MONO,
			SPEAKERS_STEREO,
			SPEAKERS_2POINT1,
			SPEAKERS_QUAD,
			SPEAKERS_4POINT1,
			SPEAKERS_5POINT1,
			SPEAKERS_5POINT1_SURROUND,
			SPEAKERS_7POINT1,
			SPEAKERS_7POINT1_SURROUND,
			SPEAKERS_SURROUND,
		};

		public enum video_colorspace : int {
			VIDEO_CS_DEFAULT,
			VIDEO_CS_601,
			VIDEO_CS_709,
			VIDEO_CS_SRGB,
			VIDEO_CS_2100_PQ,
			VIDEO_CS_2100_HLG,
		};

		public enum video_range_type : int {
			VIDEO_RANGE_DEFAULT,
			VIDEO_RANGE_PARTIAL,
			VIDEO_RANGE_FULL
		};

		/// <summary>
		/// <para>https://obsproject.com/docs/reference-core.html?highlight=obs_set_output_source#c.obs_set_output_source</para>
		/// <para>Sets the primary output source for a channel.</para>
		/// </summary>
		/// <param name="channel"></param>
		/// <param name="source"></param>
		[DllImport(importLibrary, CallingConvention = importCall)]
		public static extern void obs_set_output_source(uint channel, obs_source_t source);

		[DllImport(importLibrary, CallingConvention = importCall)]
		public static extern obs_source_t obs_get_output_source(uint channel);

		[DllImport(importLibrary, CallingConvention = importCall)]
		public static extern audio_t obs_get_audio();

		[DllImport(importLibrary, CallingConvention = importCall)]
		public static extern video_t obs_get_video();

		public enum VideoResetError {
			OBS_VIDEO_SUCCESS = 0,
			OBS_VIDEO_FAIL = -1,
			OBS_VIDEO_NOT_SUPPORTED = -2,
			OBS_VIDEO_INVALID_PARAM = -3,
			OBS_VIDEO_CURRENTLY_ACTIVE = -4,
			OBS_VIDEO_MODULE_NOT_FOUND = -5
		}
	}
}
