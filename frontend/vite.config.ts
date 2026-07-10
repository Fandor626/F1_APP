/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  test: {
    environment: 'jsdom',
    // jsdom's default document origin is opaque ("about:blank"), under which
    // window.localStorage throws/returns undefined — a real URL gives it a
    // proper origin so localStorage-backed hooks (useLocalStorage, streak,
    // fan card) work under test the same way they do in a real browser tab.
    environmentOptions: { jsdom: { url: 'http://localhost:5173' } },
    setupFiles: ['./src/shared/test/setup.ts'],
    globals: true,
  },
})
