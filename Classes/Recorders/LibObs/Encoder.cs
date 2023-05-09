using System;
using System.Runtime.InteropServices;

namespace obs_net {
    using audio_t = IntPtr;
    using obs_data_t = IntPtr;
    using obs_encoder_t = IntPtr;
    using obs_output_t = IntPtr;
    using size_t = UIntPtr;
    using video_t = IntPtr;
    public partial class Obs {
        /// <summary>
        /// https://obsproject.com/docs/reference-encoders.html?highlight=obs_video_encoder_create#c.obs_video_encoder_create
        /// </summary>
        /// <param name="id">The encoder type string identifier</param>
        /// <param name="name">The desired name of the encoder. If this is not unique, it will be made to be unique</param>
        /// <param name="settings">The settings for the encoder, or NULL if none</param>
        /// <param name="hotkey_data">Saved hotkey data for the encoder, or NULL if none</param>
        /// <returns>A reference to the newly created encoder, or NULL if failed</returns>
        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern obs_encoder_t obs_video_encoder_create(
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string id,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string name,
        obs_data_t settings, obs_data_t hotkey_data);

        [DllImport(importLibrary, CallingConvention = importCall, CharSet = importCharSet)]
        public static extern obs_encoder_t obs_audio_encoder_create(
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string id,
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] string name,
        obs_data_t settings, UIntPtr mixer_idx, obs_data_t hotkey_data);

        /// <summary>
        /// https://obsproject.com/docs/reference-outputs.html?highlight=obs_output_set_video_encoder#c.obs_output_set_video_encoder
        /// </summary>
        /// <param name="output"></param>
        /// <param name="encoder">The video/audio encoder</param>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_output_set_video_encoder(obs_output_t output, obs_encoder_t encoder);

        [DllImport(importLibrary, CallingConvention = importCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool obs_enum_encoder_types(size_t idx, [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(UTF8StringMarshaler))] ref string id);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_output_set_audio_encoder(obs_output_t output, obs_encoder_t encoder, UIntPtr idx);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_encoder_set_video(obs_encoder_t encoder, video_t video);

        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_encoder_set_audio(obs_encoder_t encoder, audio_t audio);

        /// <summary>
        /// <para>https://obsproject.com/docs/reference-encoders.html?highlight=obs_encoder_release#c.obs_encoder_release</para>
        /// <para>Releases a reference to an encoder. When the last reference is released, the encoder is destroyed.</para>
        /// </summary>
        /// <param name="encoder"></param>
        [DllImport(importLibrary, CallingConvention = importCall)]
        public static extern void obs_encoder_release(obs_encoder_t encoder);
    }
}
