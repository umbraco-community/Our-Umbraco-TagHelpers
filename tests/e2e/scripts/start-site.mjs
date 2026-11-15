// Publishes the TestSite for one Umbraco major into an isolated folder and runs it.
// Used by playwright.config.ts as a webServer command:  node scripts/start-site.mjs <16|17|18|19>
//
// Every run starts from a clean tests/e2e/.data/v<N>/ so the site does a fresh unattended
// install and uSync first-boot import (no stale database, indexes, logs or downloaded assets).

import { spawn, spawnSync } from 'node:child_process';
import { cpSync, existsSync, mkdirSync, openSync, readFileSync, rmSync } from 'node:fs';
import { join } from 'node:path';
import { configuration, libraryBuildProblem, repoRoot } from './library.mjs';
import { packageFeed, packedVersion, usePackage } from './package.mjs';

const dotnetMajorFor = { 16: 9, 17: 10, 18: 10, 19: 11 };

const version = Number(process.argv[2]);
const dotnetMajor = dotnetMajorFor[version];
if (!dotnetMajor) {
  console.error(`Usage: node scripts/start-site.mjs <16|17|18|19> (got '${process.argv[2] ?? ''}')`);
  process.exit(1);
}

const e2eDir = join(repoRoot, 'tests', 'e2e');
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

// Either the Our.Umbraco.TagHelpers project (npm test) or the packed .nupkg (npm run test:package)
const packageVersion = usePackage() ? packedVersion() : null;
const tagHelpersArgs = [];
if (usePackage()) {
  if (!packageVersion) {
    console.error(`Cannot start Umbraco ${version}: no package in ${packageFeed}. Run the tests with \`npm run test:package\`.`);
    process.exit(1);
  }
  tagHelpersArgs.push('-p:TagHelpersSource=Package', `-p:TagHelpersPackageVersion=${packageVersion}`, `-p:TagHelpersPackageSource=${packageFeed}`);
} else {
  // The sites publish in parallel, so they must not build the shared library project themselves
  // (concurrent builds of one project write the same obj/bin files). `npm test` builds it once first.
  // Refuse to run against a missing or stale build rather than silently testing old code.
  const problem = libraryBuildProblem(`net${dotnetMajor}.0`);
  if (problem) {
    console.error(`Cannot start Umbraco ${version}: ${problem}. Run the tests with \`npm test\` (or run \`npm run build:library\` first).`);
    process.exit(1);
  }
}

// Retries: on Windows a site that has only just stopped can still hold its database file open (EBUSY)
rmSync(dataDir, { recursive: true, force: true, maxRetries: 10, retryDelay: 500 });
mkdirSync(dataDir, { recursive: true });

console.log(`[v${version}] publishing TestSite for Umbraco ${version} with ${packageVersion ? `Our.Umbraco.TagHelpers ${packageVersion} (package)` : 'the Our.Umbraco.TagHelpers project'}...`);
// --no-dependencies: restore and build only the TestSite, using the library build from above
const publish = spawnSync(
  'dotnet',
  ['publish', siteProject, '--no-dependencies', '-c', configuration, `-p:UmbracoVersion=${version}`, ...tagHelpersArgs, '-o', appDir, '-nologo', '-v', 'q', '-clp:ErrorsOnly;NoSummary'],
  { stdio: 'inherit' },
);
if (publish.status !== 0) {
  console.error(`[v${version}] dotnet publish failed`);
  process.exit(publish.status ?? 1);
}

if (packageVersion) {
  // Prove the site really runs the package: its deps.json must load the tag helpers from it...
  const deps = JSON.parse(readFileSync(join(appDir, 'Our.Umbraco.TagHelpers.TestSite.deps.json'), 'utf8'));
  const library = deps.libraries?.[`Our.Umbraco.TagHelpers/${packageVersion}`];
  if (library?.type !== 'package') {
    console.error(`[v${version}] the published site does not use the Our.Umbraco.TagHelpers ${packageVersion} package`);
    process.exit(1);
  }
  // ...and the package's buildTransitive targets must register its appsettings JSON schema with Umbraco
  const items = spawnSync(
    'dotnet',
    ['msbuild', siteProject, `-p:Configuration=${configuration}`, `-p:UmbracoVersion=${version}`, ...tagHelpersArgs, '-getItem:UmbracoJsonSchemaFiles', '-nologo'],
    { encoding: 'utf8' },
  );
  const schemaFiles = items.status === 0 ? JSON.parse(items.stdout).Items.UmbracoJsonSchemaFiles.map((item) => item.Identity) : [];
  if (!schemaFiles.some((file) => /appsettings-schema\.our\.umbraco\.taghelpers\.json$/.test(file))) {
    console.error(`[v${version}] the package's appsettings schema was not registered (UmbracoJsonSchemaFiles: ${schemaFiles.join(', ') || `${items.stdout}${items.stderr}`.trim()})`);
    process.exit(1);
  }
  console.log(`[v${version}] verified: tag helpers loaded from the package, appsettings schema registered`);
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
    // Umbraco 17+ generates an imaging HMAC key on install but only uses it after a restart, so every
    // test site gets one from the first boot: image URLs must then be signed, as on a real site.
    // A fixed key, only ever used by these local test sites.
    Umbraco__CMS__Imaging__HMACSecretKey: 'PVUrY5IXvtiuu3bYjmpHXmG4tWGAghESQJEXMeC+rk4D+iy9G1MgbTyzykPEQfkg5HJw5bpqQ68D49JGD+iCoQ==',
  },
});

for (const signal of ['SIGINT', 'SIGTERM']) {
  process.on(signal, () => site.kill(signal));
}
site.on('exit', (code, signal) => {
  console.log(`[v${version}] site exited (${signal ?? code})`);
  process.exit(code ?? 0);
});
