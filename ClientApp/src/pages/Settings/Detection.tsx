import { useTranslation } from "react-i18next";

import Button from "../../components/Button";
import { postMessage } from "../../helpers/messenger";

interface Props {
  updateSettings: () => void;
  settings: DetectionSettings | undefined;
}

export const Detection: React.FC<Props> = ({ settings, updateSettings }) => {
  const { t } = useTranslation();

  return (
    <div className="flex flex-col gap-2 font-medium text-base pb-7">
      <h1 className="font-semibold text-2xl">{t("settingsDetectionItem01")}</h1>
      <span className="font-normal text-sm pb-2">
        {t("settingsDetectionItem02")}
      </span>
      <span className="text-gray-700 dark:text-gray-400">
        {t("settingsDetectionItem03")}
      </span>
      <span className="font-normal text-sm">
        {t("settingsDetectionItem04")}
      </span>
      <Button
        text={t("settingsDetectionItem05")}
        width={"auto"}
        onClick={(e) => {
          postMessage("AddProgram", "whitelist");
        }}
      />
      <div className="flex flex-row gap-1">
        <div className="flex flex-col gap-2">
          {settings !== undefined &&
            settings.whitelist.map((item) => {
              return (
                <Button
                  text={item.gameExe.replace(/^.*[\\\/]/, "")}
                  width={"auto"}
                  onClick={(e) => {
                    postMessage("RemoveProgram", {
                      list: "whitelist",
                      exe: item.gameExe,
                    });
                  }}
                />
              );
            })}
        </div>
        <div className="flex flex-col gap-2">
          {settings !== undefined &&
            settings.whitelist.map((item) => {
              return (
                <input
                  className={`inline-flex align-middle justify-center w-full h-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out bg-white border border-gray-300 rounded-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
                  type="text"
                  defaultValue={item.gameName}
                  onBlur={(e) => {
                    if (settings !== undefined) item.gameName = e.target.value;
                    updateSettings();
                  }}
                />
              );
            })}
        </div>
      </div>
      <span className="text-gray-700 dark:text-gray-400">
        {t("settingsDetectionItem06")}
      </span>
      <span className="font-normal text-sm">
        {t("settingsDetectionItem07")}
      </span>
      <div className="flex flex-col gap-2">
        {settings !== undefined &&
          settings.blacklist.map((item) => {
            return (
              <Button
                text={item.replace(/^.*[\\\/]/, "")}
                width={"auto"}
                onClick={(e) => {
                  postMessage("RemoveProgram", {
                    list: "blacklist",
                    exe: item,
                  });
                }}
              />
            );
          })}
        <Button
          text={t("settingsDetectionItem05")}
          width={"auto"}
          onClick={(e) => {
            postMessage("AddProgram", "blacklist");
          }}
        />
      </div>
    </div>
  );
};

export default Detection;
