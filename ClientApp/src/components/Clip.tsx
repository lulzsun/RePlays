interface Props {
  start?: number;
  duration?: number;
}

export const Clip: React.FC<Props> = ({start=0, duration=5}: Props) => {
  return (
    <button style={{ zIndex: 1, width: `calc(100% / ${duration})`, left: `${start}%`}} className="absolute bg-blue-400 border border-blue-500 rounded-lg h-full cursor-grab active:cursor-grabbing">

    </button>
  )
}

export default Clip;