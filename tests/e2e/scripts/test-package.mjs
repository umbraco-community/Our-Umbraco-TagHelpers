// `npm run test:package`: packs Our.Umbraco.TagHelpers exactly as build.ps1 does, then runs the Playwright
// suite with every test site installing that .nupkg instead of referencing the project.
// Arguments are passed on to Playwright, e.g.  npm run test:package -- specs/our-img.spec.ts
//
// The package gets a unique prerelease version (e.g. 2.1.0-e2e.20261115093000): NuGet caches packages by
// version, so reusing 2.1.0 would test an older build (or, once published, the nuget.org one). The contents
// are otherwise identical to what build.ps1 packs.

import { spawnSync } from 'node:child_process';
import { mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { join } from 'node:path';
import { configuration, libraryProject } from './library.mjs';
import { packageFeed, packageVersionFile } from './package.mjs';

function run(command, args, options = {}) {
  const result = spawnSync(command, args, { stdio: 'inherit', ...options });
  if (result.status !== 0) process.exit(result.status ?? 1);
  return result;
}

const baseVersion = spawnSync('dotnet', ['msbuild', libraryProject, '-getProperty:Version', '-nologo'], { encoding: 'utf8' }).stdout.trim();
if (!/^\d+\.\d+\.\d+/.test(baseVersion)) {
  console.error(`Could not read <Version> from ${libraryProject} (got '${baseVersion}')`);
  process.exit(1);
}
const stamp = new Date().toISOString().replace(/\D/g, '').slice(0, 14);
const version = `${baseVersion.split('-')[0]}-e2e.${stamp}`;

rmSync(packageFeed, { recursive: true, force: true, maxRetries: 10, retryDelay: 500 });
mkdirSync(packageFeed, { recursive: true });

console.log(`Packing Our.Umbraco.TagHelpers ${version} into ${packageFeed}...`);
run('dotnet', ['pack', libraryProject, '-c', configuration, '-o', packageFeed, `-p:Version=${version}`, '-nologo', '-v', 'q', '-clp:ErrorsOnly;NoSummary']);
writeFileSync(packageVersionFile, version);

// Run Playwright's CLI with this Node (no shell, so this works the same on Windows, macOS and Linux)
const playwrightCli = join(createRequire(import.meta.url).resolve('@playwright/test/package.json'), '..', 'cli.js');
const tests = spawnSync(process.execPath, [playwrightCli, 'test', ...process.argv.slice(2)], {
  stdio: 'inherit',
  env: { ...process.env, E2E_TAGHELPERS_SOURCE: 'package' },
});
process.exit(tests.status ?? 1);
