
import HotkeySelector from "../../components/HotkeySelector";

interface Props {
    updateSettings: () => void;
    settings: KeybindingsSettings | undefined;
    keybindings: Keybindings | undefined;
}

export const Keybindings: React.FC<Props> = ({ settings, keybindings, updateSettings }) => {

    return (
        <div className="flex flex-col gap-2 font-medium text-base pb-7">
            <h1 className="font-semibold text-2xl">Keybindings</h1>
            <div className="flex flex-col gap-1">
                Toggle Recording Keybind
                <HotkeySelector id="StartStopRecording" width="auto" keybind={keybindings?.StartStopRecording} />
            </div>
            <div className="flex flex-col gap-1">
                Bookmark Keybind
                <HotkeySelector id="CreateBookmark" width="auto" keybind={keybindings?.CreateBookmark} />
            </div>
        </div>
    )
}

export default Keybindings;