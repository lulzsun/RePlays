import './App.css';
import Player from './pages/Player';
import VideosPage from './pages/VideosPage';
import { useEffect, useState } from 'react';
import { BrowserRouter as Router, Route, Switch, Link } from 'react-router-dom';
import { postMessage, addEventListener, removeEventListener } from './helpers/messenger';

function App() {
  const [game, setGameSort] = useState("All Games");
  const [sortBy, setTypeSort] = useState("Latest");
  const [gameList, setGameList] = useState([]);
  const [clips, setClips] = useState<Video[]>([]);
  const [sessions, setSessions] = useState<Video[]>([]);
  const [clipTotal, setClipTotal] = useState(0);
  const [sessionTotal, setSessionTotal] = useState(0);

  function handleWebViewMessages(event: Event) {
    let eventData = (event as Webview2Event).data;
    let message = eventData.message;
    let data = JSON.parse(eventData.data);

    console.log(data);
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
      default:
        break;
    }
  }
  
  useEffect(() => {
    postMessage('RetrieveVideos', {game: 'All Games', sortBy: 'Latest'});
    
    addEventListener('message', handleWebViewMessages);
    return () => {
      removeEventListener('message', handleWebViewMessages);
    }
  }, []);

  return (
    <Router>
      <div className="relative min-h-screen lg:flex">
        <div className="sidebar bg-blue-800 text-blue-100 w-60 py-7 px-2 absolute inset-y-0 left-0 transform -translate-x-full lg:relative lg:translate-x-0 transition duration-200 ease-in-out">
          <div className="text-2xl font-extrabold text-white flex items-center space-x-2 px-4 pb-4">
            <svg className="w-10 h-10 pr-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 16 16" stroke="currentColor">
              <path d="M2 3a.5.5 0 0 0 .5.5h11a.5.5 0 0 0 0-1h-11A.5.5 0 0 0 2 3zm2-2a.5.5 0 0 0 .5.5h7a.5.5 0 0 0 0-1h-7A.5.5 0 0 0 4 1zm2.765 5.576A.5.5 0 0 0 6 7v5a.5.5 0 0 0 .765.424l4-2.5a.5.5 0 0 0 0-.848l-4-2.5z"/>
              <path d="M1.5 14.5A1.5 1.5 0 0 1 0 13V6a1.5 1.5 0 0 1 1.5-1.5h13A1.5 1.5 0 0 1 16 6v7a1.5 1.5 0 0 1-1.5 1.5h-13zm13-1a.5.5 0 0 0 .5-.5V6a.5.5 0 0 0-.5-.5h-13A.5.5 0 0 0 1 6v7a.5.5 0 0 0 .5.5h13z"/>
            </svg>
            RePlays
          </div>

          <Link to="/" className="flex items-center block py-2.5 px-4 rounded transition duration-200 hover:bg-blue-700 hover:text-white">
            <svg className="w-10 h-10 pr-4" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
              <path d="M0 1a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1H1a1 1 0 0 1-1-1V1zm4 0v6h8V1H4zm8 8H4v6h8V9zM1 1v2h2V1H1zm2 3H1v2h2V4zM1 7v2h2V7H1zm2 3H1v2h2v-2zm-2 3v2h2v-2H1zM15 1h-2v2h2V1zm-2 3v2h2V4h-2zm2 3h-2v2h2V7zm-2 3v2h2v-2h-2zm2 3h-2v2h2v-2z"/>
            </svg>
            Sessions
          </Link>
          <Link to="/clips" className="flex items-center block py-2.5 px-4 rounded transition duration-200 hover:bg-blue-700 hover:text-white">
            <svg className="w-10 h-10 pr-4" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
              <path d="M3.5 3.5c-.614-.884-.074-1.962.858-2.5L8 7.226 11.642 1c.932.538 1.472 1.616.858 2.5L8.81 8.61l1.556 2.661a2.5 2.5 0 1 1-.794.637L8 9.73l-1.572 2.177a2.5 2.5 0 1 1-.794-.637L7.19 8.61 3.5 3.5zm2.5 10a1.5 1.5 0 1 0-3 0 1.5 1.5 0 0 0 3 0zm7 0a1.5 1.5 0 1 0-3 0 1.5 1.5 0 0 0 3 0z"/>
            </svg>
            Clips
          </Link>
          <Link to="/uploads" className="flex items-center block py-2.5 px-4 rounded transition duration-200 hover:bg-blue-700 hover:text-white">
            <svg className="w-10 h-10 pr-4" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
              <path fillRule="evenodd" d="M7.646 5.146a.5.5 0 0 1 .708 0l2 2a.5.5 0 0 1-.708.708L8.5 6.707V10.5a.5.5 0 0 1-1 0V6.707L6.354 7.854a.5.5 0 1 1-.708-.708l2-2z"/>
              <path d="M4.406 3.342A5.53 5.53 0 0 1 8 2c2.69 0 4.923 2 5.166 4.579C14.758 6.804 16 8.137 16 9.773 16 11.569 14.502 13 12.687 13H3.781C1.708 13 0 11.366 0 9.318c0-1.763 1.266-3.223 2.942-3.593.143-.863.698-1.723 1.464-2.383zm.653.757c-.757.653-1.153 1.44-1.153 2.056v.448l-.445.049C2.064 6.805 1 7.952 1 9.318 1 10.785 2.23 12 3.781 12h8.906C13.98 12 15 10.988 15 9.773c0-1.216-1.02-2.228-2.313-2.228h-.5v-.5C12.188 4.825 10.328 3 8 3a4.53 4.53 0 0 0-2.941 1.1z"/>
            </svg>
            Uploads
          </Link>
          <Link to="/settings" className="flex items-center block py-2.5 px-4 rounded transition duration-200 hover:bg-blue-700 hover:text-white">
            <svg className="w-10 h-10 pr-4" xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16">
              <path d="M8 4.754a3.246 3.246 0 1 0 0 6.492 3.246 3.246 0 0 0 0-6.492zM5.754 8a2.246 2.246 0 1 1 4.492 0 2.246 2.246 0 0 1-4.492 0z"/>
              <path d="M9.796 1.343c-.527-1.79-3.065-1.79-3.592 0l-.094.319a.873.873 0 0 1-1.255.52l-.292-.16c-1.64-.892-3.433.902-2.54 2.541l.159.292a.873.873 0 0 1-.52 1.255l-.319.094c-1.79.527-1.79 3.065 0 3.592l.319.094a.873.873 0 0 1 .52 1.255l-.16.292c-.892 1.64.901 3.434 2.541 2.54l.292-.159a.873.873 0 0 1 1.255.52l.094.319c.527 1.79 3.065 1.79 3.592 0l.094-.319a.873.873 0 0 1 1.255-.52l.292.16c1.64.893 3.434-.902 2.54-2.541l-.159-.292a.873.873 0 0 1 .52-1.255l.319-.094c1.79-.527 1.79-3.065 0-3.592l-.319-.094a.873.873 0 0 1-.52-1.255l.16-.292c.893-1.64-.902-3.433-2.541-2.54l-.292.159a.873.873 0 0 1-1.255-.52l-.094-.319zm-2.633.283c.246-.835 1.428-.835 1.674 0l.094.319a1.873 1.873 0 0 0 2.693 1.115l.291-.16c.764-.415 1.6.42 1.184 1.185l-.159.292a1.873 1.873 0 0 0 1.116 2.692l.318.094c.835.246.835 1.428 0 1.674l-.319.094a1.873 1.873 0 0 0-1.115 2.693l.16.291c.415.764-.42 1.6-1.185 1.184l-.291-.159a1.873 1.873 0 0 0-2.693 1.116l-.094.318c-.246.835-1.428.835-1.674 0l-.094-.319a1.873 1.873 0 0 0-2.692-1.115l-.292.16c-.764.415-1.6-.42-1.184-1.185l.159-.291A1.873 1.873 0 0 0 1.945 8.93l-.319-.094c-.835-.246-.835-1.428 0-1.674l.319-.094A1.873 1.873 0 0 0 3.06 4.377l-.16-.292c-.415-.764.42-1.6 1.185-1.184l.292.159a1.873 1.873 0 0 0 2.692-1.115l.094-.319z"/>
            </svg>
            Settings
          </Link>
        </div>
        
        <div className="flex flex-col h-screen w-full text-2xl font-bold">
          <div className="flex-initial">
            <div className="bg-gray-800 text-gray-100 flex justify-between lg:hidden">
              <div className="block p-4 text-white font-bold">RePlays</div>
              <button className="mobile-menu-button p-4 focus:outline-none focus:bg-gray-700">
                <svg className="h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M4 6h16M4 12h16M4 18h16" />
                </svg>
              </button>
            </div>
          </div>

          <div className="flex-auto overflow-auto h-full p-7">
            <Switch>
              <Route exact path="/">         <VideosPage key={"Sessions"} videoType={"Sessions"} gameList={gameList} game={game} sortBy={sortBy} videos={sessions} size={sessionTotal}/></Route>
              <Route exact path="/clips">    <VideosPage key={"Clips"} videoType={"Clips"} gameList={gameList} game={game} sortBy={sortBy} videos={clips} size={clipTotal}/></Route>
              <Route exact path="/uploads">uploads</Route>
              <Route exact path="/settings">settings</Route>
              <Route exact path="/player/:game/:video"><Player/></Route>
            </Switch>
          </div>
        </div>
      </div>
    </Router>
  );
}

export default App;
