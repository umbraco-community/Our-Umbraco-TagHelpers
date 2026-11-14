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
  // Per version: a setup project logs into the backoffice once and saves the session
  // (the demo disallows concurrent logins of the same user), then the version's tests run.
  projects: versions.flatMap((v) => [
    {
      name: `setup-v${v}`,
      testMatch: /.*\.setup\.ts/,
      use: { baseURL: `http://localhost:50${v}` },
      metadata: { umbracoMajor: v },
    },
    {
      name: `v${v}`,
      dependencies: [`setup-v${v}`],
      use: { baseURL: `http://localhost:50${v}` },
      metadata: { umbracoMajor: v },
    },
  ]),
  webServer: versions.map((v) => ({
    command: `node scripts/start-site.mjs ${v}`,
    url: `http://localhost:50${v}/`,
    // Publish + unattended install + uSync import
    timeout: 600_000,
    // Always start freshly published sites, so a site left running from an earlier run (serving old
    // views and data) can't be tested by mistake; if a port is busy, Playwright reports it.
    // Set E2E_REUSE_SITES=1 to reuse already-running sites while iterating on specs only.
    reuseExistingServer: !process.env.CI && process.env.E2E_REUSE_SITES === '1',
    stdout: 'pipe' as const,
    stderr: 'pipe' as const,
  })),
});
