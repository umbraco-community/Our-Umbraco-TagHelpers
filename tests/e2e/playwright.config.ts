import { defineConfig } from '@playwright/test';

// Which Umbraco majors to test, e.g. UMBRACO_VERSIONS=17,19. Each gets its own site and port (50<N>).
const supported = [16, 17, 18, 19];
const versions = (process.env.UMBRACO_VERSIONS ?? supported.join(','))
  .split(',')
  .map((v) => Number(v.trim()))
  .filter((v) => {
    if (!supported.includes(v)) throw new Error(`Unsupported Umbraco version in UMBRACO_VERSIONS: ${v}`);
    return true;
  });

export default defineConfig({
  testDir: './specs',
  fullyParallel: true,
  retries: process.env.CI ? 1 : 0,
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: versions.map((v) => ({
    name: `v${v}`,
    use: { baseURL: `http://localhost:50${v}` },
    metadata: { umbracoMajor: v },
  })),
  webServer: versions.map((v) => ({
    command: `node scripts/start-site.mjs ${v}`,
    url: `http://localhost:50${v}/`,
    // Publish + unattended install + uSync import
    timeout: 600_000,
    reuseExistingServer: !process.env.CI,
    stdout: 'pipe' as const,
    stderr: 'pipe' as const,
  })),
});
