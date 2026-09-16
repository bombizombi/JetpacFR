import { defineConfig } from 'vite'

export default defineConfig({
    build: {
        outDir: "dist"
    },
    // LAN + Tailscale access: bind all interfaces and allow the machine's
    // host names (same setup as JetpacFR.Web; "pc" is the Tailscale name).
    server: {
        host: "0.0.0.0",
        allowedHosts: ["mestar-pc", "pc", ".ts.net"],
    },
})
