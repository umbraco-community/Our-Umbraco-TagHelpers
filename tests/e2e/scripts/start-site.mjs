// Publishes the TestSite for one Umbraco major into an isolated folder and runs it.
// Used by playwright.config.ts as a webServer command:  node scripts/start-site.mjs <16|17|18|19>
//
// Every run starts from a clean tests/e2e/.data/v<N>/ so the site does a fresh unattended
// install and uSync first-boot import (no stale database, indexes, logs or downloaded assets).

import { spawn, spawnSync } from 'node:child_process';
import { cpSync, existsSync, mkdirSync, openSync, rmSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { setTimeout as sleep } from 'node:timers/promises';

const dotnetMajorFor = { 16: 9, 17: 10, 18: 10, 19: 11 };

const version = Number(process.argv[2]);
const dotnetMajor = dotnetMajorFor[version];
if (!dotnetMajor) {
  console.error(`Usage: node scripts/start-site.mjs <16|17|18|19> (got '${process.argv[2] ?? ''}')`);
  process.exit(1);
}

const e2eDir = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const repoRoot = resolve(e2eDir, '..', '..');
const siteProject = join(repoRoot, 'Our.Umbraco.TagHelpers.TestSite');
const dataDir = join(e2eDir, '.data', `v${version}`);
const appDir = join(dataDir, 'app');
const port = 5000 + version;

// Fail fast (instead of hanging until the webServer timeout) when the runtime is missing.
const runtimes = spawnSync('dotnet', ['--list-runtimes'], { encoding: 'utf8' });
if (runtimes.status !== 0 || !new RegExp(`^Microsoft\\.AspNetCore\\.App ${dotnetMajor}\\.`, 'm').test(runtimes.stdout)) {
  console.error(`Umbraco ${version} needs the .NET ${dotnetMajor} runtime (Microsoft.AspNetCore.App ${dotnetMajor}.x), which is not installed.`);
  process.exit(1);
}

rmSync(dataDir, { recursive: true, force: true });
mkdirSync(dataDir, { recursive: true });

// All versions build the same library project; publish one at a time so they never
// write to its obj/bin folders concurrently.
const lockDir = join(e2eDir, '.data', '.publish.lock');
async function withPublishLock(fn) {
  for (;;) {
    try {
      mkdirSync(lockDir);
      break;
    } catch {
      // A lock left behind by an interrupted run would otherwise block every future run
      try {
        if (Date.now() - statSync(lockDir).mtimeMs > 15 * 60_000) rmSync(lockDir, { recursive: true, force: true });
      } catch {}
      await sleep(500);
    }
  }
  try {
    return fn();
  } finally {
    rmSync(lockDir, { recursive: true, force: true });
  }
}

console.log(`[v${version}] publishing TestSite for Umbraco ${version}...`);
const publish = await withPublishLock(() =>
  spawnSync(
    'dotnet',
    ['publish', siteProject, '-c', 'Release', `-p:UmbracoVersion=${version}`, '-o', appDir, '-nologo', '-v', 'q', '-clp:ErrorsOnly;NoSummary'],
    { stdio: 'inherit' },
  ),
);
if (publish.status !== 0) {
  console.error(`[v${version}] dotnet publish failed`);
  process.exit(publish.status ?? 1);
}

// Umbraco excludes wwwroot/media from publish (so deploys don't overwrite uploads);
// the demo's media items need the files.
const mediaSource = join(siteProject, 'wwwroot', 'media');
if (existsSync(mediaSource)) {
  cpSync(mediaSource, join(appDir, 'wwwroot', 'media'), { recursive: true });
}

const logFile = join(dataDir, 'site.log');
console.log(`[v${version}] starting on http://localhost:${port} (log: ${logFile})`);
const log = openSync(logFile, 'a');
const site = spawn('dotnet', ['Our.Umbraco.TagHelpers.TestSite.dll'], {
  cwd: appDir,
  stdio: ['ignore', log, log],
  env: {
    ...process.env,
    ASPNETCORE_ENVIRONMENT: 'Development',
    ASPNETCORE_URLS: `http://localhost:${port}`,
    Umbraco__CMS__Global__UseHttps: 'false',
  },
});

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => site.kill(signal));
}
site.on('exit', (code, signal) => {
  console.log(`[v${version}] site exited (${signal ?? code})`);
  process.exit(code ?? 0);
});
