module.exports = {
  purge: ['./src/**/*.{js,jsx,ts,tsx}', './public/index.html'],
  darkMode: false, // or 'media' or 'class'
  theme: {
    extend: {},
    cursor: {
      'col-resize': 'col-resize',
      'ew-resize': 'ew-resize',
      'grab': 'grab',
      'grabbing': 'grabbing',
      'pointer': 'pointer'
    },
    colors: {
      'primary': 'white',
      'secondary': '#4B5563',
      'ternary': 'white'
    }
  },
  variants: {
    extend: {
      cursor: ['active'],
    }
  },
  plugins: [],
}
