import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { viteSingleFile } from 'vite-plugin-singlefile';
import svgr from 'vite-plugin-svgr';

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react(), viteSingleFile(), svgr()],
  build: {
    outDir: 'build',
    assetsInlineLimit: 10000000000,
  },
});
