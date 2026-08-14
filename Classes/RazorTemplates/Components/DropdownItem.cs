namespace RePlays.Classes.RazorTemplates.Components {
    /// <summary>
    ///  One entry of a <c>Dropdown</c>. The component renders each item twice — once as an
    ///  &lt;option&gt; for the native control and once as a menu row — so the two faces of the
    ///  dropdown always agree.
    /// </summary>
    public class DropdownItem {
        public string value { get; set; }
        /// <summary> Literal text. Ignored when <see cref="i18nKey"/> is set. </summary>
        public string text { get; set; }
        /// <summary> Locale key, resolved client side by the 'i18n' htmx extension. </summary>
        public string i18nKey { get; set; }
        /// <summary> Optional heading to group this item under (renders as an optgroup / menu title). </summary>
        public string group { get; set; }

        /// <summary> An item whose text is shown as-is (device names, resolutions, encoders...). </summary>
        public static DropdownItem Literal(string value, string text, string group = null) {
            return new DropdownItem { value = value, text = text, group = group };
        }

        /// <summary> An item whose text comes from the locale file. </summary>
        public static DropdownItem Translated(string value, string i18nKey, string group = null) {
            return new DropdownItem { value = value, i18nKey = i18nKey, group = group };
        }
    }
}
