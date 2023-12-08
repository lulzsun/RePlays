#pragma warning disable CA1806
using RePlays.Utils;
using System;
using System.IO;
using System.Runtime.InteropServices;
using static RePlays.Utils.Functions;

namespace RePlays.Classes.Utils {
#if !WINDOWS
    public static class LinuxInterface {
#if DEBUG
        static readonly string icon = Path.Join(GetSolutionPath(), "/Resources/logo.png");
#endif
        public static void Create() {
            int argc = 0;
            IntPtr argv = IntPtr.Zero;
            GTK.gtk_init(ref argc, ref argv);

            IntPtr indicator = Ayatana.app_indicator_new("RePlays", icon, 0);

            InitializeWebView();

            // Create a new GTK menu
            IntPtr menu = GTK.gtk_menu_new();

            // Create and connect a check menu item
            IntPtr checkMenuItem = GTK.gtk_check_menu_item_new_with_label("1");
            GTK.g_signal_connect_data(checkMenuItem, "activate", new GTK.ActivateCallback(ItemClickedCallback), Marshal.StringToHGlobalAnsi("1"), IntPtr.Zero, GTK.GConnectFlags.G_CONNECT_AFTER);
            GTK.gtk_menu_shell_append(menu, checkMenuItem);
            GTK.gtk_widget_show(checkMenuItem);

            // Create and connect a radio menu item
            IntPtr radioMenuItem = GTK.gtk_radio_menu_item_new_with_label(IntPtr.Zero, "2");
            GTK.g_signal_connect_data(radioMenuItem, "activate", new GTK.ActivateCallback(ItemClickedCallback), Marshal.StringToHGlobalAnsi("2"), IntPtr.Zero, GTK.GConnectFlags.G_CONNECT_AFTER);
            GTK.gtk_menu_shell_append(menu, radioMenuItem);
            GTK.gtk_widget_show(radioMenuItem);

            // // Create a menu item for "3" with a submenu
            // IntPtr subMenuItem = gtk_menu_item_new_with_label("3");
            // gtk_menu_shell_append(menu, subMenuItem);
            // append_submenu(subMenuItem); // Assuming append_submenu is a function that adds items to the submenu
            // gtk_widget_show(subMenuItem);

            if (indicator == IntPtr.Zero) {
                Logger.WriteLine("Failed to create system tray.");
                return;
            }

            Ayatana.app_indicator_set_status(indicator, 2);
            Ayatana.app_indicator_set_icon(indicator, icon);
            Ayatana.app_indicator_set_menu(indicator, menu);

            // Run the Gtk main loop
            GTK.gtk_main();
        }

        static void InitializeWebView() {
            IntPtr window = GTK.gtk_window_new(GTK.GtkWindowType.GTK_WINDOW_TOPLEVEL);
            GTK.gtk_window_set_default_size(window, 1080, 600);

            // Create a new WebKitGTK WebView
            IntPtr webView = WebKitGtk.webkit_web_view_new();

            // Enable developer extras
            IntPtr settings = WebKitGtk.webkit_settings_new();
            WebKitGtk.webkit_settings_set_enable_developer_extras(settings, true);
            WebKitGtk.webkit_web_view_set_settings(webView, settings);

            // Load a URL into the WebView
            WebKitGtk.webkit_web_view_load_uri(webView, GetRePlaysURI());

            // Add the WebView to the window
            GTK.gtk_container_add(window, webView);

            // Show all widgets in the window
            GTK.gtk_widget_show_all(window);
        }

        public static void Destroy() {
            throw new NotImplementedException();
        }

        public static void ItemClickedCallback(IntPtr widget, IntPtr userData) {
            string label = Marshal.PtrToStringAnsi(userData);
            Console.WriteLine($"Item clicked: {label}");
        }
    }

    class Ayatana {
        const string LibAyatanaAppIndicator3 = "libayatana-appindicator3"; // Adjust the library name based on your system

        [DllImport(LibAyatanaAppIndicator3, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr app_indicator_new(string id, string icon, int category);

        [DllImport(LibAyatanaAppIndicator3, CallingConvention = CallingConvention.Cdecl)]
        public static extern void app_indicator_set_status(IntPtr indicator, int status);

        [DllImport(LibAyatanaAppIndicator3, CallingConvention = CallingConvention.Cdecl)]
        public static extern void app_indicator_set_icon(IntPtr indicator, string icon);

        [DllImport(LibAyatanaAppIndicator3, CallingConvention = CallingConvention.Cdecl)]
        public static extern void app_indicator_set_menu(IntPtr indicator, IntPtr menu);
        // Add other function declarations as needed
    }

    class GTK {
        const string GtkLibrary = "libgtk-3";

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_init(ref int argc, ref IntPtr argv);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gtk_window_new(GtkWindowType windowType);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_window_set_default_size(IntPtr window, int width, int height);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_container_add(IntPtr container, IntPtr widget);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_widget_show_all(IntPtr widget);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_main();

        // GTK types
        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gtk_menu_new();

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gtk_check_menu_item_new_with_label(string label);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gtk_radio_menu_item_new_with_label(IntPtr group, string label);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gtk_menu_item_new_with_label(string label);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_menu_shell_append(IntPtr menuShell, IntPtr child);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_widget_show(IntPtr widget);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint g_signal_connect_data(IntPtr instance, string detailed_signal, ActivateCallback handler, IntPtr data, IntPtr destroy_data, GConnectFlags connect_flags);

        // Callbacks
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ActivateCallback(IntPtr widget, IntPtr userData);

        // Enums
        public enum GConnectFlags {
            G_CONNECT_AFTER = 1 << 0,
            G_CONNECT_SWAPPED = 1 << 1
        }

        public enum GtkWindowType {
            GTK_WINDOW_TOPLEVEL = 0,
        }
    }

    class WebKitGtk {
        const string WebKitGtkLibrary = "libwebkit2gtk-4.0.so";

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr webkit_web_view_new();

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void webkit_web_view_load_uri(IntPtr webView, string uri);

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr webkit_web_view_get_main_frame(IntPtr webView);

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr webkit_web_frame_get_global_context(IntPtr frame);

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr webkit_web_frame_get_js_context(IntPtr frame);

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr webkit_javascript_global_context_get_type();

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr webkit_javascript_global_context_get_global_object(IntPtr context);

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void webkit_web_view_set_settings(IntPtr webView, IntPtr settings);

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr webkit_settings_new();

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void webkit_settings_set_enable_developer_extras(IntPtr settings, bool enable);

    }
#endif
}