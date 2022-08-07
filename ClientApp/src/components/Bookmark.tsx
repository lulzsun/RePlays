import { forwardRef } from "react";

interface Props {
    id: number;
    time?: number;
}

export const Bookmark = forwardRef<HTMLDivElement, Props>(({ id, time = 0 }, ref) => {
    return (
        <div ref={ref} data-index={id} style={{ zIndex: 1, width: `6px`, left: `${time}%` }} className="grid grid-flow-col absolute bg-yellow-400 rounded-lg h-full"
            onContextMenu={(e) => {
                e.preventDefault();
            }}>
        </div>
    )
})

export default Bookmark;