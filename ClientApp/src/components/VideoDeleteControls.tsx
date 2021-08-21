import React from 'react';
import Button from './Button';

interface Props {
  selectAll: () => void;
  unSelectAll: () => void;
  deleteSelected: () => void;
  length: number;
}

export const VideoDeleteControls: React.FC<Props> = ({selectAll, unSelectAll, deleteSelected, length=1}) => {
	return (
    <div className="pt-2 grid grid-flow-col gap-4">
      <span className="mt-0.5 inline-flex justify-center w-full px-4 py-2 text-sm font-medium leading-5 text-gray-700 transition duration-150 ease-in-out rounded-md hover:text-gray-500 active:bg-gray-50 active:text-gray-800">
        {`${length} selected video${(length > 1 ? 's' : '')}`}
      </span>
      <div className="inline-flex gap-4">
        <Button text={"Unselect All"} onClick={(e) => unSelectAll()}/>
        <Button text={"Select All"} onClick={(e) => selectAll()}/>
        <Button text={"Delete"} onClick={(e) => deleteSelected()}/>
      </div>
    </div>
	)
}

export default VideoDeleteControls;