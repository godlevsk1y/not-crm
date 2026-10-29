import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./tests",
  fullyParallel: true,
  use: { baseURL: "http://localhost:3112" },
  webServer: {
    command: "npm run dev -- --port 3112",
    url: "http://localhost:3112",
    env: { NEXT_PUBLIC_API_URL: "http://localhost:3112/api/" },
    timeout: 120_000,
  },
});
