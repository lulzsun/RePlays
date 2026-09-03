
interface Props {
  text: string;
}

export const HelpSymbol: React.FC<Props> = ({ text }) => {
  return (
    <>
      <div title={ text }><svg className="ml-1" viewBox="0 0 256 256" width="20px" height="20px" cursor="help">
        <g fill="#17b2b0" fillRule="nonzero" stroke="none" strokeWidth="1" strokeLinecap="butt" strokeLinejoin="miter" strokeMiterlimit="10" strokeDasharray="" strokeDashoffset="0" fontFamily="none" fontWeight="none" fontSize="none" textAnchor="none">
          <g transform="scale(2,2)">
            <path d="M64,6c-32,0 -58,26 -58,58c0,32 26,58 58,58c32,0 58,-26 58,-58c0,-32 -26,-58 -58,-58zM64,12c28.7,0 52,23.3 52,52c0,28.7 -23.3,52 -52,52c-28.7,0 -52,-23.3 -52,-52c0,-28.7 23.3,-52 52,-52zM64,30c-4.97056,0 -9,4.02944 -9,9c0,4.97056 4.02944,9 9,9c4.97056,0 9,-4.02944 9,-9c0,-4.97056 -4.02944,-9 -9,-9zM64,59c-5,0 -9,4 -9,9v24c0,5 4,9 9,9c5,0 9,-4 9,-9v-24c0,-5 -4,-9 -9,-9z"></path>
          </g>
        </g>
      </svg>
      </div>
    </>
  );
};

export default HelpSymbol;
