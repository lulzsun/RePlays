import { useTranslation } from "react-i18next";

import React, { useContext } from "react";
import { Link } from "react-router-dom";
import { formatBytes } from "../helpers/utils";
import { postMessage } from "../helpers/messenger";
import UploadModal from "./UploadModal";
import { ModalContext } from "../Contexts";
import { CompressModal } from "./CompressModal";

interface Props {
  game?: string;
  size?: number;
  duration?: number;
  kills?: number;
  assists?: number;
  deaths?: number;
  champion?: string;
  win?: boolean;
  date?: string;
  video?: string;
  videoType?: string;
  folder?: string;
  thumb?: string;
  checked?: boolean;
  onChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
}

export const Card: React.FC<Props> = ({
  date = Date.now().toString(),
  game = "Game Unknown",
  thumb = "video_thumbnail_placeholder.png",
  size = 0,
  duration = 0,
  kills,
  assists,
  deaths,
  champion,
  win,
  video = "",
  videoType = "",
  folder = "",
  checked,
  onChange,
}) => {
  const { t } = useTranslation();

  const modalCtx = useContext(ModalContext);
  const resultText = win === undefined ? undefined : win ? "Win" : "Loss";
  const resultColor =
    win === undefined ? undefined : win ? "text-green-500" : "text-red-500";

  function handleUpload() {
    console.log(`${game} ${video} ${videoType} to upload`);
    var thumb = `${folder}/${game}/.thumbs/${video}`;
    thumb =
      thumb.substr(0, thumb.lastIndexOf(".")) + ".webp" || thumb + ".webp";

    modalCtx?.setData({
      title: "Upload",
      context: <UploadModal video={video} game={game} thumb={thumb} />,
      cancel: true,
    });
    modalCtx?.setOpen(true);
  }

  function handleCompress() {
    console.log(`${game} ${video} ${videoType} to compress`);
    var thumb = `${folder}/${game}/.thumbs/${video}`;
    thumb =
      thumb.substr(0, thumb.lastIndexOf(".")) + ".webp" || thumb + ".webp";

    modalCtx?.setData({
      title: "Compress",
      context: <CompressModal video={video} game={game} thumb={thumb} />,
      cancel: true,
    });
    modalCtx?.setOpen(true);
  }

  return (
    <div
      className={
        "relative w-full block h-full group rounded-lg border " +
        (checked ? "border-2 border-blue-500" : "border border-gray-500")
      }
    >
      <div className="absolute z-50 w-full flex justify-between">
        <div className={"m-2 group-hover:opacity-100 " + (checked ? "opacity-100" : "opacity-0")}>
          <input tabIndex={-1} type="checkbox" className="h-4 w-4" checked={(checked === undefined || checked === false ? false : true)} onChange={(e) => {onChange(e);}}/>
        </div>
        <div className="m-1 mr-2 dropdown" style={{zIndex: 50}}>
          <button tabIndex={-1} type="button" aria-haspopup="true" aria-expanded="true" aria-controls="headlessui-menu-items-117" className="opacity-0 group-hover:opacity-100">
            <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" className="h-6 w-6 text-white" viewBox="0 0 16 16">
              <path d="M3 9.5a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm5 0a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3zm5 0a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3z"/>
            </svg>
          </button>
          <div className="opacity-0 invisible dropdown-menu transition-all duration-300 transform origin-top-right -translate-y-2 scale-95">
            <div
              className="absolute right-0 w-auto mt-2 origin-top-right bg-white border border-gray-500 divide-y divide-gray-100 rounded-md shadow-lg outline-none"
              aria-labelledby="headlessui-menu-button-1"
              id="headlessui-menu-items-117"
              role="menu"
            >
              <div
                className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-right whitespace-nowrap"
                onClick={() => {
                  handleCompress();
                }}
              >
                {t("componentsCardItem01")}
              </div>
              <div
                className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-right whitespace-nowrap"
                onClick={() => {
                  postMessage("ShowInFolder", { filePath: `${game}/${video}` });
                }}
              >
                {t("componentsCardItem02")}
              </div>
              <div
                className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-right whitespace-nowrap"
                onClick={() => {
                  postMessage("Delete", { filePaths: [`${game}/${video}`] });
                }}
              >
                {t("componentsCardItem03")}
              </div>
              {videoType === "Clips" && (
                <div
                  className="cursor-pointer text-gray-700 flex justify-between w-full px-4 py-2 text-sm leading-5 text-right whitespace-nowrap"
                  onClick={() => {
                    handleUpload();
                  }}
                >
                  {t("componentsCardItem04")}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
      <Link className="rounded-lg" to={`/player/${game}/${video}/${videoType}`} onClick={() => {console.log(folder)}}>
        <div className="relative text-white w-full rounded-t-lg object-cover overflow-hidden items-center">
          <span
            className="absolute z-40 bottom-1 right-1 py-0.5 px-2 rounded-full text-xs font-normal"
            style={{ backgroundColor: `rgba(0, 0, 0, 0.5)` }}
          >
            {duration > 3600
              ? new Date(duration * 1000)
                  .toISOString()
                  .substr(11, 8)
                  .replace(/^0+/, "")
              : new Date(duration * 1000).toISOString().substring(14, 19)}
          </span>
          {game === "League of Legends" &&
            videoType === "Sessions" &&
            champion &&
            champion !== "TFTChampion" && (
              <span className="absolute z-40 bottom-1 left-1 text-xs font-normal flex items-center">
                <img
                  className="border border-black -mr-4 z-40 rounded-full"
                  src={`https://ddragon.leagueoflegends.com/cdn/13.13.1/img/champion/${champion}.png`}
                  style={{ width: "20px" }}
                  alt={champion}
                />
                <p
                  className="py-0.5 pl-5 rounded-full p-2"
                  style={{ backgroundColor: `rgba(0, 0, 0, 0.5)` }}
                >
                  {`${kills}/${deaths}/${assists}`}
                  {resultText && " - "}
                  {resultText && (
                    <span className={`font-bold ${resultColor}`}>
                      {resultText}
                    </span>
                  )}
                </p>
              </span>
            )}
          <div className="absolute z-30 w-full h-full bg-black opacity-0 group-hover:opacity-50" />
          <img
            className="absolute z-20 w-full"
            alt=""
            src={`${folder}/${game}/.thumbs/${thumb}`}
          />
          <img
            className="relative z-10 w-full"
            alt=""
            src={"video_thumbnail_placeholder.png"}
          />
        </div>
        <div className="bg-white dark:bg-gray-900 text-gray-800 dark:text-white w-full rounded-b-lg p-4 text-xs font-medium">
          {game}
          <p className="text-gray-700 dark:text-gray-400 font-light">
            {new Date(date).toLocaleDateString()} |{" "}
            {new Date(date).toLocaleTimeString([], {
              hour: "2-digit",
              minute: "2-digit",
            })}{" "}
            | {formatBytes(size)}
          </p>
        </div>
      </Link>
    </div>
  );
};

export default Card;
