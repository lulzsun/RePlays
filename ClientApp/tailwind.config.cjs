/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{js,jsx,ts,tsx}', './index.html'],
  darkMode: 'class', // or 'media' or 'class'
  theme: {
    extend: {},
    cursor: {
      'col-resize': 'col-resize',
      'ew-resize': 'ew-resize',
      'grab': 'grab',
      'grabbing': 'grabbing',
      'pointer': 'pointer'
    },
  },
  variants: {
    extend: {
      cursor: ['active'],
    }
  },
  plugins: [],
}