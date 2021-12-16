import { postMessage } from '../helpers/messenger';

interface Props {
  id?: string;
  path?: string;
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void;
}

export const DirectoryBrowser: React.FC<Props> = ({id="", path="C:/FakePath", onClick}) => {
	return (
    <button className={`relative inline-flex justify-center w-96 h-full px-4 py-2 text-sm font-medium leading-5 text-gray-600 transition duration-150 ease-in-out bg-gray-100 border border-gray-300 rounded-md hover:text-gray-700 focus:outline-none focus:border-blue-300 focus:shadow-outline-blue active:bg-gray-50 active:text-gray-800`}
      type="button" onClick={(e) => {
        if(onClick) onClick(e); 
        postMessage("SelectFolder", id);
      }}>
        <div className="flex flex-row w-full">
          <span className="w-11/12 overflow-ellipsis overflow-hidden">{path}</span>
          <svg className="w-1/12 h-5 ml-2 -mr-1" viewBox="0 0 20 20" fill="currentColor">
            <path fillRule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clipRule="evenodd"/>
          </svg>
        </div>
    </button>
	)
}

export default DirectoryBrowser;