import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';
import path from 'node:path';

export default defineConfig({
    plugins: [vue()],
	  define: {
        'process.env.NODE_ENV': '"production"'
    },
    build: {
        outDir: path.resolve(__dirname, '../../wwwroot/vue/video-editor'),
        emptyOutDir: true,
        lib: {
            entry: path.resolve(__dirname, 'src/main.ts'),
            formats: ['es'],
            fileName: () => 'index.js'
        },
        rollupOptions: {
            output: {
                assetFileNames: assetInfo => {
                    if (assetInfo.name?.endsWith('.css')) {
                        return 'index.css';
                    }
                    return 'assets/[name]-[hash][extname]';
                }
            }
        }
    }
});