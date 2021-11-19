import { useEffect, useRef } from "react";

interface Props {
  toastData: ModalData;
  onClick?: () => any;
}

export const Toast: React.FC<Props> = ({toastData, onClick}) => {
  var circle = useRef<SVGCircleElement>(null);

  useEffect(() => {
    if(circle === null || circle.current === null) return;
    var radius = circle.current.r.baseVal.value;
    var circumference = radius * 2 * Math.PI;

    circle.current.style.strokeDasharray = `${circumference} ${circumference}`;
    circle.current.style.strokeDashoffset = `${circumference}`;
    const offset = circumference - (toastData.progress! / toastData!.progressMax!) * circumference;
    circle.current.style.strokeDashoffset = offset + "";
  }, [toastData]);

	return (
    <div className="flex w-full overflow-hidden bg-white rounded-lg shadow-md dark:bg-gray-800">
      <div className="p-1.5 flex items-center justify-center bg-blue-500">
        <svg className="progress-ring" width="30" height="30">
          <circle ref={circle} className="progress-ring__circle" stroke="white" strokeWidth="2" fill="transparent" r="12" cx="15" cy="15"/>
        </svg>
      </div>
      <div className="p-1.5">
        <span className="text-sm font-semibold text-blue-500 dark:text-blue-400">
          {toastData.title}</span>
        <p className="h-4 w-28 text-xs text-gray-600 dark:text-gray-200 truncate">
          {toastData.context}</p>
      </div>
    </div>
	)
}

export default Toast;