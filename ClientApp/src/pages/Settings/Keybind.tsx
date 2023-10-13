import KeybindSelector from "../../components/KeybindSelector";

interface Props {
  updateSettings: () => void;
  settings: KeybindSettings | undefined;
}

export const Keybind: React.FC<Props> = ({ settings, updateSettings }) => {
  console.log(settings)

  return (
    <div className="flex flex-col gap-2 font-medium text-base pb-7">
      <h1 className="font-semibold text-2xl">Keybinds</h1>
      <div className="flex flex-col gap-1">
        <KeybindSelector id="StartStopRecording" settings={settings} updateSettings={updateSettings} keybind={settings?.StartStopRecording}/>
      </div>
      <div className="flex flex-col gap-1">
        <KeybindSelector id="CreateBookmark" settings={settings} updateSettings={updateSettings} keybind={settings?.CreateBookmark}/>
      </div>
    </div>
  )
}

export default Keybind;