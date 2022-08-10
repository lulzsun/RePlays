using System;
using System.Runtime.InteropServices;

namespace obs_net{
    using obs_output_t = IntPtr;
    using obs_data_t = IntPtr;
    using signal_handler_t = IntPtr;
    using video_t = IntPtr;
    using audio_t = IntPtr;
    using size_t = UIntPtr;

    public partial class Obs {
        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern obs_output_t obs_output_create(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string id,
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string name,
            obs_data_t settings, obs_data_t hotkey_data);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_output_release(obs_output_t output);

        [DllImport(importLibrary, CallingConvention = importCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool obs_output_start(obs_output_t output);

        [DllImport(importLibrary, CallingConvention = importCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool obs_output_can_begin_data_capture(obs_output_t output, uint flags);

        [DllImport(importLibrary, CallingConvention = importCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool obs_output_initialize_encoders(obs_output_t output, uint flags);

        /// <summary>
        /// <para>https://obsproject.com/docs/reference-outputs.html?highlight=obs_output_stop#c.obs_output_stop</para>
        /// <para>Requests the output to stop. The output will wait until all data is sent up until the time the call was made, then when the output has successfully stopped, it will send the “stop” signal.</para>
        /// <para>See <see href="https://obsproject.com/docs/reference-outputs.html?highlight=obs_output_stop#output-signal-handler-reference">Output Signals</see> for more information on output signals.</para>
        /// </summary>
        /// <param name="output"></param>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_output_stop(obs_output_t output);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern signal_handler_t obs_output_get_signal_handler(obs_output_t output);

        /// <summary>
        /// https://obsproject.com/docs/reference-outputs.html#c.obs_output_get_last_error
        /// </summary>
        /// <returns>Gets the translated error message that is presented to a user in case of disconnection, inability to connect, etc.</returns>
        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))]
        public static extern string obs_output_get_last_error(obs_output_t output);

        /// <summary>
        /// <para>https://obsproject.com/docs/reference-outputs.html?highlight=obs_output_update#c.obs_output_update</para>
        /// <para>Updates the settings for this output context.</para>
        /// </summary>
        /// <param name="output"></param>
        /// <param name="settings"></param>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_output_update(obs_output_t output, obs_data_t settings);

        /// <summary>
        /// <para>https://obsproject.com/docs/reference-outputs.html?highlight=obs_output_update#c.obs_output_set_mixers</para>
        /// <para>Sets the current audio mixer for non-encoded outputs. For multi-track outputs, this would be the equivalent of setting the mask only for the specified mixer index.</para>
        /// </summary>
        /// <param name="output"></param>
        /// <param name="mixers"></param>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_output_set_mixers(obs_output_t output, size_t mixers);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern video_t obs_output_video(obs_output_t output);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern audio_t obs_output_audio(obs_output_t output);
    }
}
