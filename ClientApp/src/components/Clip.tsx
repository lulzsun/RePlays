import { forwardRef } from "react";

interface Props {
  id: number;
  start?: number;
  duration?: number;
}

export const Clip = forwardRef<HTMLDivElement, Props>(({id, start=0, duration=5}, ref) => {
  return (
    <div ref={ref} data-index={id} style={{ zIndex: 1, width: `calc(100% / ${duration})`, left: `${start}%`}} className="grid grid-flow-col absolute bg-blue-400 border border-blue-500 rounded-lg h-full cursor-grab active:cursor-grabbing">
      <div className="h-full w-full">
        <div className="h-full w-2 -ml-0.5 cursor-col-resize"></div>
      </div>
      <div className="h-full w-full flex justify-end">
        <div className="h-full w-2 -mr-0.5 cursor-col-resize"></div>
      </div>
    </div>
  )
})

export default Clip;