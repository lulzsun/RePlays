import Logo from './logo.svg';
import Player from './pages/Player';
import VideosPage from './pages/VideosPage';
import { createContext, useEffect, useState } from 'react';
import { HashRouter as Router, Route, Switch, Link } from 'react-router-dom';
import { postMessage, addEventListener, removeEventListener } from './helpers/messenger';
import ContextMenu from './components/ContextMenu';
import { useRef } from 'react';
import Settings from './pages/Settings';
import Modal from './components/Modal';
import Toast from './components/Toast';
import {BookmarkType} from './index';

export const ContextMenuContext = createContext<ContextMenuOptions | null>(null);
export const ModalContext = createContext<ModalOptions | null>(null);

function App() {
  const sideBarEle = useRef<HTMLDivElement | null>(null);

  const [contextMenuItems, setContextMenuItems] = useState<ContextMenuItem[]>();
  const [contextMenuPosition, setContextMenuPosition] = useState<ContextMenuPosition>({x: -100, y: -100});
  const contextMenuFocusEle = useRef<HTMLInputElement | null>(null);

  const [modalData, setModalData] = useState({});
  const [modalOpen, setModalOpen] = useState(false);
  const [modalConfirm, setModalConfirm] = useState(() => () => {});

  const [toastList, setToastList] = useState<ModalData[]>([]);
  //const recentLinksEle = useRef<HTMLSelectElement | null>(null);
  const [recentLinksMenuOpen, setRecentLinksMenuOpen] = useState(false);

  const [game, setGameSort] = useState("All Games");
  const [sortBy, setTypeSort] = useState("Latest");
  const [gameList, setGameList] = useState([]);
  // @ts-ignore
  const [clips, setClips] = useState<Video[]>(null);
  // @ts-ignore
  const [sessions, setSessions] = useState<Video[]>(null);
  const [clipTotal, setClipTotal] = useState(0);
  const [sessionTotal, setSessionTotal] = useState(0);
  const [userSettings, setUserSettings] = useState<UserSettings>();

  function handleWebViewMessages(event: Event) {
    let eventData = (event as Webview2Event).data;
    let message = eventData.message;
    let data = JSON.parse(eventData.data);

    if(data && data.title !== "Downloading") 
      console.log(message, data);

    switch (message) {
      case 'RetrieveVideos':
        setGameSort(data.game);
        setTypeSort(data.sortBy);
        setGameList(data.games);

        setClips(data.clips);
        setSessions(data.sessions);

        setClipTotal(data.clipsSize);
        setSessionTotal(data.sessionsSize);
        break;
      case 'DisplayModal':
        setModalData(data);
        if(data.title === "Downloading") {
          setModalConfirm(() => () => {});
        } else setModalConfirm(() => () => {});
        setModalOpen(true);
        break;
      case 'DisplayToast':
        setToastList(arr => {
          var tl = [...arr];
          var existingIndex = tl.findIndex(toast => toast.id === data.id);

          if(existingIndex === -1) {
            tl.push(data);
          }
          else {
            tl[existingIndex] = data;
          }
          return tl;
        });
        break;
      case 'DestroyToast':
        setToastList(arr => {
          var tl = [...arr];
          var existingIndex = tl.findIndex(toast => toast.id === data.id);

          if(existingIndex !== -1) tl.splice(existingIndex, 2);
          return tl;
        });
        break;
      case 'SetBookmarks':
        console.log(data);
        let videoMetadata = JSON.parse(localStorage.getItem("videoMetadataBookmarks")!);

        let bookmarks: { id: number, type: BookmarkType, time: number }[] = [];
        const map = [BookmarkType.Manual, BookmarkType.Kill];

        data.bookmarks.forEach(function (bookmark: any) {
            let timeToSet = bookmark.time / (data.elapsed) * 100;
            bookmarks.push({ id: Date.now(), type: map[bookmark.type], time: timeToSet });
        }); 

        videoMetadata[data.videoname] = { bookmarks };

        localStorage.setItem("videoMetadataBookmarks", JSON.stringify(videoMetadata));
        break;
      case 'UserSettings':
        setUserSettings(data);
        localStorage.setItem("availableRateControls", data.captureSettings.rateControlCache); 
        break;
      default:
        break;
    }
  }


  function handleMouseDown(e: MouseEvent) {
    if(recentLinksMenuOpen) {
      postMessage('HideRecentLinks');
      setRecentLinksMenuOpen(false);
    }
  }
  
  useEffect(() => {
    if(document.querySelector('initialized') === null) {
        if (localStorage.getItem("videoMetadata") === null) localStorage.setItem("videoMetadata", '{}');
        if (localStorage.getItem("videoMetadataBookmarks") === null) localStorage.setItem("videoMetadataBookmarks", '{}');

      postMessage('Initialize');
      postMessage('RetrieveVideos', {game: 'All Games', sortBy: 'Latest'});
      
      addEventListener('message', handleWebViewMessages);
      document.body.appendChild(document.createElement('initialized'));
    }
    document.addEventListener('mousedown', handleMouseDown);
    return () => {
      if(document.querySelector('initialized') === null) {
        removeEventListener('message', handleWebViewMessages);
      }
      document.removeEventListener('mousedown', handleMouseDown);
    }
  }, [recentLinksMenuOpen]);

  return (
    <Router>
      <ModalContext.Provider value={{setData: (data) => {setModalData(data)}, setOpen: (open) => {setModalOpen(open)}, isOpen: modalOpen, setConfirm: (confirm) => {setModalConfirm(() => confirm)}}}>
      <ContextMenuContext.Provider value={{setItems: (items) => {setContextMenuItems(items)}, setPosition: (position) => {
          setTimeout(() => {
            contextMenuFocusEle.current!.focus(); 
            setContextMenuPosition(position);
          }, 1);
        }}}>
        <div className={(window.matchMedia("(prefers-color-scheme: dark)").matches && (userSettings == null && window.matchMedia("(prefers-color-scheme: dark)").matches ? "System" : userSettings?.generalSettings.theme) === "System" ? "Dark" : userSettings?.generalSettings.theme)}>
          <Modal modalData={modalData} open={modalOpen} setOpen={setModalOpen} onConfirm={modalConfirm}/>
          <div className="bg-white dark:bg-gray-800 relative min-h-screen lg:flex">
            <div className="absolute inline-block text-left dropdown" style={{zIndex: 9999}}>
              <input tabIndex={-1} ref={contextMenuFocusEle} className="w-0"/>
              <div className="absolute -top-1/3 opacity-0 invisible dropdown-menu transition-all duration-300 transform scale-95">
                <ContextMenu items={contextMenuItems} position={contextMenuPosition}/>
              </div>
            </div>

            <div ref={sideBarEle} style={{zIndex: 999}} className="flex flex-col h-screen justify-between bg-gray-900 text-blue-100 w-auto py-2 px-2 absolute inset-y-0 left-0 transform -translate-x-full lg:relative lg:translate-x-0 transition duration-200 ease-in-out">
              {/* Header & Side Buttons Container */}
              <div className="w-full">
                <div className="text-2xl font-semibold text-white flex items-center space-x-2 px-4 pb-4 pt-3">
                  {/* <svg className="w-10 h-10 pr-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 16 16" stroke="currentColor">
                    <path d="M2 3a.5.5 0 0 0 .5.5h11a.5.5 0 0 0 0-1h-11A.5.5 0 0 0 2 3zm2-2a.5.5 0 0 0 .5.5h7a.5.5 0 0 0 0-1h-7A.5.5 0 0 0 4 1zm2.765 5.576A.5.5 0 0 0 6 7v5a.5.5 0 0 0 .765.424l4-2.5a.5.5 0 0 0 0-.848l-4-2.5z"/>
                    <path d="M1.5 14.5A1.5 1.5 0 0 1 0 13V6a1.5 1.5 0 0 1 1.5-1.5h13A1.5 1.5 0 0 1 16 6v7a1.5 1.5 0 0 1-1.5 1.5h-13zm13-1a.5.5 0 0 0 .5-.5V6a.5.5 0 0 0-.5-.5h-13A.5.5 0 0 0 1 6v7a.5.5 0 0 0 .5.5h13z"/>
                  </svg> */}
                  <img src={Logo} className="w-12 h-12 pr-4" alt="logo" />
                  RePlays
                </div>

                <Link to="/" className="flex items-center block py-2.5 px-4 rounded transition duration-200 hover:bg-gray-700 hover:text-white"
                onClick={() => {
                  sideBarEle.current!.classList.toggle("-translate-x-full");
                }}>
                  <svg className="w-10 h-10 pr-4" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M0 1a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H1a1 1 0 0 1-1-1V1zm4 0v6h8V1H4zm8 8H4v6h8V9zM1 1v2h2V1H1zm2 3H1v2h2V4zM1 7v2h2V7H1zm2 3H1v2h2v-2zm-2 3v2h2v-2H1zM15 1h-2v2h2V1zm-2 3v2h2V4h-2zm2 3h-2v2h2V7zm-2 3v2h2v-2h-2zm2 3h-2v2h2v-2z"/>
                  </svg>
                  Sessions
                </Link>
                <Link to="/clips" className="flex items-center block py-2.5 px-4 rounded transition duration-200 hover:bg-gray-700 hover:text-white"
                onClick={() => {
                  sideBarEle.current!.classList.toggle("-translate-x-full");
                }}>
                  <svg className="w-10 h-10 pr-4" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M3.5 3.5c-.614-.884-.074-1.962.858-2.5L8 7.226 11.642 1c.932.538 1.472 1.616.858 2.5L8.81 8.61l1.556 2.661a2.5 2.5 0 1 1-.794.637L8 9.73l-1.572 2.177a2.5 2.5 0 1 1-.794-.637L7.19 8.61 3.5 3.5zm2.5 10a1.5 1.5 0 1 0-3 0 1.5 1.5 0 0 0 3 0zm7 0a1.5 1.5 0 1 0-3 0 1.5 1.5 0 0 0 3 0z"/>
                  </svg>
                  Clips
                </Link>
              </div>
              {/* Toast Container */}
              <div className="mt-auto flex relative w-full space-2 px-2">
                <div className="absolute inset-x-0 bottom-0 flex flex-col space-y-2">
                  {toastList && toastList.map((toast) => {
                    return <Toast key={toast.id} toastData={toast}/>
                  })}
                </div>
              </div>
              {/* Footer Container */}
              <div className="flex flex-row items-center justify-center w-full">
                <div title="Hide/Show Notifications" className="p-2 px-4 flex items-center rounded transition duration-200 hover:bg-gray-700 hover:text-white cursor-pointer">
                  <svg className="w-6 h-6" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M8 16a2 2 0 0 0 2-2H6a2 2 0 0 0 2 2zm.995-14.901a1 1 0 1 0-1.99 0A5.002 5.002 0 0 0 3 6c0 1.098-.5 6-2 7h14c-1.5-1-2-5.902-2-7 0-2.42-1.72-4.44-4.005-4.901z"/>
                  </svg>
                </div>
                <div title="Recent Links" className="p-2 px-4 flex items-center rounded transition duration-200 hover:bg-gray-700 hover:text-white cursor-pointer"
                onClick={(e) => {
                  postMessage("ShowRecentLinks"); setRecentLinksMenuOpen(true);
                }}>
                  <svg className="w-6 h-6" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M12.643 15C13.979 15 15 13.845 15 12.5V5H1v7.5C1 13.845 2.021 15 3.357 15h9.286zM5.5 7h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1 0-1zM.8 1a.8.8 0 0 0-.8.8V3a.8.8 0 0 0 .8.8h14.4A.8.8 0 0 0 16 3V1.8a.8.8 0 0 0-.8-.8H.8z"/>
                  </svg>
                </div>
                {/* <div className="group relative">
                  <svg className="w-6 h-6 absolute left-0 right-0 top-0 bottom-0 m-auto group-hover:text-white pointer-events-none" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                    <path d="M12.643 15C13.979 15 15 13.845 15 12.5V5H1v7.5C1 13.845 2.021 15 3.357 15h9.286zM5.5 7h5a.5.5 0 0 1 0 1h-5a.5.5 0 0 1 0-1zM.8 1a.8.8 0 0 0-.8.8V3a.8.8 0 0 0 .8.8h14.4A.8.8 0 0 0 16 3V1.8a.8.8 0 0 0-.8-.8H.8z"/>
                  </svg>
                  <select className={`w-14 h-10 flex items-center rounded transition duration-200 bg-transparent text-transparent group-hover:bg-gray-700 focus:outline-none cursor-pointer`}
                  name="recentLinksDdm" id="recentLinksDdm" defaultValue="-1" 
                  onChange={(e) => {
                    navigator.clipboard.writeText(e.target.value);
                    e.target.value = "-1";
                  }}>
                    <option disabled value="-1">Recent Links | Click to copy to clipboard</option>
                    {userSettings && userSettings?.uploadSettings && userSettings?.uploadSettings.recentLinks.map((link, i) => {
                      return <option key={i} value={`${link.split("] ")[1]}`} onClick={(e) => {console.log("hi")}}>{link}</option>
                    })}
                  </select>
                </div> */}
                <Link title="Settings" to="/settings/General" className="p-2 px-4 flex items-center rounded transition duration-200 hover:bg-gray-700 hover:text-white cursor-pointer"
                onClick={() => {
                  sideBarEle.current!.classList.toggle("-translate-x-full");
                }}>
                  <svg className="w-6 h-6" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
                  <path d="M9.405 1.05c-.413-1.4-2.397-1.4-2.81 0l-.1.34a1.464 1.464 0 0 1-2.105.872l-.31-.17c-1.283-.698-2.686.705-1.987 1.987l.169.311c.446.82.023 1.841-.872 2.105l-.34.1c-1.4.413-1.4 2.397 0 2.81l.34.1a1.464 1.464 0 0 1 .872 2.105l-.17.31c-.698 1.283.705 2.686 1.987 1.987l.311-.169a1.464 1.464 0 0 1 2.105.872l.1.34c.413 1.4 2.397 1.4 2.81 0l.1-.34a1.464 1.464 0 0 1 2.105-.872l.31.17c1.283.698 2.686-.705 1.987-1.987l-.169-.311a1.464 1.464 0 0 1 .872-2.105l.34-.1c1.4-.413 1.4-2.397 0-2.81l-.34-.1a1.464 1.464 0 0 1-.872-2.105l.17-.31c.698-1.283-.705-2.686-1.987-1.987l-.311.169a1.464 1.464 0 0 1-2.105-.872l-.1-.34zM8 10.93a2.929 2.929 0 1 1 0-5.86 2.929 2.929 0 0 1 0 5.858z"/>
                  </svg>
                </Link>
              </div>
            </div>
            
            <div className="flex flex-col h-screen w-full text-2xl font-semibold text-gray-900 dark:text-white">
              <div className="flex-initial">
                <div className="bg-gray-900 text-gray-100 flex justify-between lg:hidden">
                  <div className="block p-4 text-white font-semibold">RePlays</div>
                    <button className="p-4 focus:outline-none focus:bg-gray-700" 
                    onClick={() => {
                      sideBarEle.current!.classList.toggle("-translate-x-full");
                    }}>
                    <svg className="h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 12h16M4 18h16" />
                    </svg>
                  </button>
                </div>
              </div>

              <div className="flex-auto overflow-hidden h-full p-7 text-gray-900 dark:text-white">
                <Switch>
                  <Route exact path="/">         <VideosPage key={"Sessions"} videoType={"Sessions"} gameList={gameList} game={game} sortBy={sortBy} videos={sessions} size={sessionTotal}/></Route>
                  <Route exact path="/clips">    <VideosPage key={"Clips"} videoType={"Clips"} gameList={gameList} game={game} sortBy={sortBy} videos={clips} size={clipTotal}/></Route>
                  {/* <Route exact path="/uploads">  <VideosPage key={"Uploads"} videoType={"Uploads"} gameList={gameList} game={game} sortBy={sortBy} videos={clips} size={clipTotal}/></Route> */}
                  <Route exact path="/settings/:page"> <Settings userSettings={userSettings} setUserSettings={setUserSettings}/></Route>
                  <Route exact path="/player/:game/:video/:videoType"><Player videos={sessions != null ? sessions.concat(clips) : []}/></Route>
                </Switch>
              </div>
            </div>
          </div>
        </div>
      </ContextMenuContext.Provider>
      </ModalContext.Provider>
    </Router>
  );
}

export default App;
