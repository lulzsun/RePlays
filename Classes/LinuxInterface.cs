#pragma warning disable CA1806
using RePlays.Utils;
using RePlays.Services;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using static RePlays.Utils.Functions;

namespace RePlays {
#if !WINDOWS
    public static class LinuxInterface {
#if DEBUG
        static readonly string icon = Path.Join(GetSolutionPath(), "/Resources/logo.svg");
#endif
        static IntPtr window;
        public static void Create() {
            int argc = 0;
            IntPtr argv = IntPtr.Zero;
            GTK.gtk_init(ref argc, ref argv);

            if (!SettingsService.Settings.generalSettings.startMinimized)
                InitializeWebView();

            // Create a new GTK menu
            IntPtr menu = GTK.gtk_menu_new();

            // Create menu item to open interface
            IntPtr openMenuItem = GTK.gtk_menu_item_new_with_label("Open");
            GTK.g_signal_connect_data(openMenuItem, "activate",
                new GTK.ActivateCallback((_, _) => {
                    InitializeWebView();
                }),
                Marshal.StringToHGlobalAnsi("Open"),
                IntPtr.Zero,
                GTK.GConnectFlags.G_CONNECT_AFTER
            );
            GTK.gtk_menu_shell_append(menu, openMenuItem);
            GTK.gtk_widget_show(openMenuItem);

            // Create menu item to check for updates
            IntPtr updateMenuItem = GTK.gtk_menu_item_new_with_label("Check for updates");
            GTK.g_signal_connect_data(updateMenuItem, "activate",
                new GTK.ActivateCallback((widget, userData) => {
                    string label = Marshal.PtrToStringAnsi(userData);
                    Logger.WriteLine($"Item clicked: {label}");
                }),
                Marshal.StringToHGlobalAnsi("Check for updates"),
                IntPtr.Zero,
                GTK.GConnectFlags.G_CONNECT_AFTER
            );
            GTK.gtk_menu_shell_append(menu, updateMenuItem);
            GTK.gtk_widget_show(updateMenuItem);

            // Create menu item to open recent links
            IntPtr linksMenuItem = GTK.gtk_menu_item_new_with_label("Recent links");
            GTK.gtk_menu_shell_append(menu, linksMenuItem);
            IntPtr subMenu = GTK.gtk_menu_new();
            IntPtr subMenuItem = GTK.gtk_menu_item_new_with_label("Left click to copy");
            GTK.gtk_menu_shell_append(subMenu, subMenuItem);
            GTK.gtk_widget_show_all(subMenu);
            GTK.gtk_menu_item_set_submenu(linksMenuItem, subMenu);
            GTK.gtk_widget_show(linksMenuItem);

            // Create separator menu item
            IntPtr separatorMenuItem = GTK.gtk_separator_menu_item_new();
            GTK.gtk_menu_shell_append(menu, separatorMenuItem);
            GTK.gtk_widget_show(separatorMenuItem);

            // Create menu item to quit application
            IntPtr quitMenuItem = GTK.gtk_menu_item_new_with_label("Quit");
            GTK.g_signal_connect_data(quitMenuItem, "activate",
                new GTK.ActivateCallback((widget, userData) => {
                    Environment.Exit(1);
                }),
                Marshal.StringToHGlobalAnsi("Quit"),
                IntPtr.Zero,
                GTK.GConnectFlags.G_CONNECT_AFTER
            );
            GTK.gtk_menu_shell_append(menu, quitMenuItem);
            GTK.gtk_widget_show(quitMenuItem);
#if true
            IntPtr indicator = Ayatana.app_indicator_new("RePlays", icon, 0);
            if (indicator == IntPtr.Zero) {
                Logger.WriteLine("Failed to create system tray.");
                return;
            }
            Ayatana.app_indicator_set_status(indicator, 1);
            Ayatana.app_indicator_set_icon(indicator, icon);
            Ayatana.app_indicator_set_menu(indicator, menu);
            GTK.g_signal_connect_data(indicator, "activate",
                new GTK.ActivateCallback((widget, userData) => {
                    string label = Marshal.PtrToStringAnsi(userData);
                    Logger.WriteLine($"Item clicked: {label}");
                }),
                Marshal.StringToHGlobalAnsi("Tray"),
                IntPtr.Zero,
                GTK.GConnectFlags.G_CONNECT_AFTER
            );
#else
            // Create status icon tray
            IntPtr statusIcon = GTK.gtk_status_icon_new();
            GTK.gtk_status_icon_set_tooltip_text(statusIcon, "RePlays");
            GTK.gtk_status_icon_set_from_file(statusIcon, icon);
            GTK.gtk_status_icon_set_visible(statusIcon, true);
            var menuPositionCallback = new GTK.MenuPositionCallback((nint _, out int x, out int y, out bool push_in, nint userData) => {
                GTK.gtk_status_icon_position_menu(menu, out int _x, out int _y, out bool _push_in, userData);
                x = _x;
                y = _y;
                push_in = _push_in;
            });
            GTK.g_signal_connect_data(statusIcon, "activate",
                new GTK.ActivateCallback((widget, userData) => {
                    InitializeWebView();
                }),
                Marshal.StringToHGlobalAnsi("Tray"),
                IntPtr.Zero,
                GTK.GConnectFlags.G_CONNECT_AFTER
            );
            GTK.g_signal_connect_data(statusIcon, "popup-menu",
                new GTK.PopupMenuCallback((statusIcon, button, activateTime, userData) => {
                    GTK.gtk_widget_show_all(menu);
                    GTK.gtk_menu_popup(menu, IntPtr.Zero, IntPtr.Zero, menuPositionCallback, statusIcon, button, activateTime);
                }),
                IntPtr.Zero,
                IntPtr.Zero,
                GTK.GConnectFlags.G_CONNECT_AFTER
            );
#endif

            // Run the Gtk main loop
            GTK.gtk_main();
        }

        static void InitializeWebView() {
            if (window != IntPtr.Zero) {
                GTK.gtk_window_present(window);
                GTK.gtk_window_set_keep_above(window, true);
                GTK.gtk_window_set_keep_above(window, false);
                return;
            }
            window = GTK.gtk_window_new(GTK.GtkWindowType.GTK_WINDOW_TOPLEVEL);
            GTK.gtk_window_set_default_size(window, 1080, 600);
            GTK.gtk_window_set_icon_from_file(window, icon);

            // Create a new WebKitGTK WebView
            IntPtr webView = WebKitGtk.webkit_web_view_new();

            // Enable extra settings
            IntPtr settings = WebKitGtk.webkit_settings_new();
            WebKitGtk.webkit_settings_set_user_agent(settings, "RePlays/WebView");
            WebKitGtk.webkit_settings_set_disable_web_security(settings, true);
            WebKitGtk.webkit_settings_set_enable_developer_extras(settings, true);
            WebKitGtk.webkit_settings_set_allow_file_access_from_file_urls(settings, true);
            WebKitGtk.webkit_settings_set_allow_universal_access_from_file_urls(settings, true);
            WebKitGtk.webkit_web_view_set_settings(webView, settings);

            // Load a URL into the WebView
            WebKitGtk.webkit_web_view_load_uri(webView, GetRePlaysURI());

            // Add the WebView to the window
            GTK.gtk_container_add(window, webView);

            // Show all widgets in the window
            GTK.gtk_widget_show_all(window);

            GTK.g_signal_connect_data(window, "destroy",
                new GTK.ActivateCallback((widget, userData) => {
                    window = IntPtr.Zero;
                }),
                Marshal.StringToHGlobalAnsi("Close"),
                IntPtr.Zero,
                GTK.GConnectFlags.G_CONNECT_AFTER
            );

            // Bring window to the front
            GTK.gtk_window_present(window);
        }

        public static void Destroy() {
            throw new NotImplementedException();
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
        public static extern void gtk_main();

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gtk_window_new(GtkWindowType windowType);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_window_present(IntPtr window);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_window_set_keep_above(IntPtr window, bool setting);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_window_present_with_time(IntPtr window, int timestamp);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_window_set_default_size(IntPtr window, int width, int height);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_window_set_icon_from_file(IntPtr icon, string filename);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_container_add(IntPtr container, IntPtr widget);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_widget_show_all(IntPtr widget);

        // GTK types
        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gtk_menu_new();

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gtk_menu_item_new_with_label(string label);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_menu_shell_append(IntPtr menuShell, IntPtr child);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_menu_popup(IntPtr menu, IntPtr parentMenuShell, IntPtr parentMenuItem, MenuPositionCallback func, IntPtr data, uint button, uint activateTime);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_menu_item_set_submenu(IntPtr menuItem, IntPtr submenu);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gtk_separator_menu_item_new();

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_widget_show(IntPtr widget);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gtk_status_icon_new();

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_status_icon_set_from_file(IntPtr statusIcon, string filename);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_status_icon_set_tooltip_text(IntPtr statusIcon, string text);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_status_icon_set_visible(IntPtr statusIcon, bool visible);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gtk_status_icon_position_menu(IntPtr menu, out int x, out int y, out bool push_in, IntPtr userData);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool gtk_status_icon_get_geometry(IntPtr statusIcon, out IntPtr screen, out GdkRectangle area, out int orientation);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void g_signal_connect_data(IntPtr instance, string detailed_signal, ActivateCallback activateHandler, IntPtr activateData, IntPtr activateDestroyData, GConnectFlags activateConnectFlags);

        [DllImport(GtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void g_signal_connect_data(IntPtr instance, string detailed_signal, PopupMenuCallback popupMenuHandler, IntPtr popupMenuData, IntPtr popupMenuDestroyData, GConnectFlags popupMenuConnectFlags);

        // Callbacks
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void ActivateCallback(IntPtr widget, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void PopupMenuCallback(IntPtr statusIcon, uint button, uint activateTime, IntPtr userData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void MenuPositionCallback(IntPtr menu, out int x, out int y, out bool push_in, IntPtr userData);

        // Enums
        public enum GConnectFlags {
            G_CONNECT_AFTER = 1 << 0,
            G_CONNECT_SWAPPED = 1 << 1
        }

        public enum GtkWindowType {
            GTK_WINDOW_TOPLEVEL = 0,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GdkRectangle {
            public int x;
            public int y;
            public int width;
            public int height;
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
        public static extern void webkit_settings_set_disable_web_security(IntPtr settings, bool enable);

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void webkit_settings_set_allow_file_access_from_file_urls(IntPtr settings, bool enable);

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void webkit_settings_set_allow_universal_access_from_file_urls(IntPtr settings, bool enable);

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void webkit_settings_set_enable_developer_extras(IntPtr settings, bool enable);

        [DllImport(WebKitGtkLibrary, CallingConvention = CallingConvention.Cdecl)]
        public static extern void webkit_settings_set_user_agent(IntPtr settings, string userAgent);
    }
#endif
}