import { defineConfig } from 'vite'

export default defineConfig({
    build: {
        // Bundle maps for prod debugging (chain down to .fs only when the
        // input .fs.js already carries a Fable map; `elmish-land build`
        // itself passes no --sourceMaps, so full .fs chains are a dev-loop
        // property via elmish-land.json serverCommand + manual --sourceMaps).
        outDir: "dist",
        sourcemap: true
    },
    // LAN + Tailscale access: bind all interfaces and allow the machine's
    // host names (same setup as JetpacFR.Web; "pc" is the Tailscale name).
    server: {
        host: "0.0.0.0",
        allowedHosts: ["mestar-pc", "pc", ".ts.net"],
    },
})
