import { forwardRef } from "react";

interface Props {
  id: number;
  time?: number;
  type?: BookmarkType;
}

export const Bookmark = forwardRef<HTMLDivElement, Props>(({ id, time = 0, type = BookmarkType.Manual }, ref) => {
  let className = "grid grid-flow-col absolute rounded-lg h-full bg-yellow-400";
  return (
    <div ref={ref} data-index={id} style={{ zIndex: 1, width: `5px`, left: `${time}%` }} className={className}
      onContextMenu={(e) => {
        e.preventDefault();
      }}>


      {type === BookmarkType.Manual ?
        <svg xmlns="http://www.w3.org/2000/svg" xmlnsXlink="http://www.w3.org/1999/xlink" style={{ marginLeft: "-33%", marginTop: "-18px" }} width="15px" height="30px" > <g id="surface1"> <path style={{ stroke: "none", fillRule: "nonzero", fill: "rgb(97.647059%,80%,7.843137%)", fillOpacity: 1 }} d="M 0.910156 0.796875 L 14.058594 0.796875 C 14.5625 0.796875 14.96875 1.160156 14.96875 1.605469 L 14.96875 14.691406 C 14.96875 15.136719 14.5625 15.5 14.058594 15.5 L 0.910156 15.5 C 0.40625 15.5 0 15.136719 0 14.691406 L 0 1.605469 C 0 1.160156 0.40625 0.796875 0.910156 0.796875 Z M 0.910156 0.796875 " /> <path style={{ stroke: "none", fillRule: "nonzero", fill: "rgb(97.647059%,80%,7.843137%)", fillOpacity: 1 }} d="M 7.320312 28.199219 C 7.4375 28.410156 7.554688 28.410156 7.671875 28.199219 L 14.914062 15.089844 C 15.027344 14.878906 14.96875 14.777344 14.738281 14.777344 L 0.21875 14.777344 C -0.015625 14.777344 -0.0742188 14.878906 0.0429688 15.089844 Z M 7.320312 28.199219 " /> <path style={{ stroke: "none", fillRule: "nonzero", fill: "rgb(0%,0%,0%)", fillOpacity: 1 }} d="M 11.007812 2.824219 L 3.992188 2.824219 C 3.21875 2.824219 2.597656 3.433594 2.597656 4.1875 L 2.589844 15.070312 L 7.5 13.027344 L 12.410156 15.070312 L 12.410156 4.1875 C 12.410156 3.433594 11.78125 2.824219 11.007812 2.824219 Z M 11.007812 2.824219 " /> </g> </svg>
        :
        <svg xmlns="http://www.w3.org/2000/svg" xmlnsXlink="http://www.w3.org/1999/xlink" style={{ marginLeft: "-33%", marginTop: "-18px" }} width="15px" height="30px" > <g id="surface1"> <path style={{ stroke: "none", fillRule: "nonzero", fill: "rgb(97.647059%,80%,7.843137%)", fillOpacity: 1 }} d="M 0.910156 0.796875 L 14.058594 0.796875 C 14.5625 0.796875 14.96875 1.160156 14.96875 1.605469 L 14.96875 14.691406 C 14.96875 15.136719 14.5625 15.5 14.058594 15.5 L 0.910156 15.5 C 0.40625 15.5 0 15.136719 0 14.691406 L 0 1.605469 C 0 1.160156 0.40625 0.796875 0.910156 0.796875 Z M 0.910156 0.796875 " /> <path style={{ stroke: "none", fillRule: "nonzero", fill: "rgb(97.647059%,80%,7.843137%)", fillOpacity: 1 }} d="M 7.320312 28.199219 C 7.4375 28.410156 7.554688 28.410156 7.671875 28.199219 L 14.914062 15.089844 C 15.027344 14.878906 14.96875 14.777344 14.738281 14.777344 L 0.21875 14.777344 C -0.015625 14.777344 -0.0742188 14.878906 0.0429688 15.089844 Z M 7.320312 28.199219 " /> <path style={{ stroke: "none", fillRule: "nonzero", fill: "rgb(0%,0%,0%)", fillOpacity: 1 }} d="M 4.203125 9.710938 L 6.554688 11.992188 L 5.613281 12.90625 L 6.558594 13.820312 L 5.617188 14.734375 L 3.96875 13.136719 L 2.082031 14.964844 L 1.140625 14.050781 L 3.023438 12.222656 L 1.375 10.625 L 2.316406 9.710938 L 3.261719 10.621094 Z M 1.503906 2.984375 L 3.867188 2.988281 L 11.738281 10.625 L 12.683594 9.710938 L 13.625 10.625 L 11.976562 12.222656 L 13.859375 14.050781 L 12.917969 14.964844 L 11.03125 13.136719 L 9.382812 14.734375 L 8.441406 13.820312 L 9.382812 12.90625 L 1.507812 5.265625 Z M 11.136719 2.984375 L 13.496094 2.988281 L 13.496094 5.265625 L 10.796875 7.882812 L 8.441406 5.597656 Z M 11.136719 2.984375 " /> </g> </svg>
      }      
    </div>
  )
})

export default Bookmark;