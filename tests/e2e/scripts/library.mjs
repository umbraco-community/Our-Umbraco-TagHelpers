// The Our.Umbraco.TagHelpers library that every test site references.
// It is built once (scripts/build-library.mjs, run by `npm test`) before the sites are published,
// because the sites publish in parallel and must not build the same project at the same time.

import { existsSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

export const repoRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..', '..', '..');
export const libraryProject = join(repoRoot, 'Our.Umbraco.TagHelpers');
export const configuration = 'Release';

export function libraryDll(targetFramework) {
  return join(libraryProject, 'bin', configuration, targetFramework, 'Our.Umbraco.TagHelpers.dll');
}

/**
 * Newest modification time of the files that are compiled into the DLL (bin/obj excluded).
 * Only .cs/.csproj count: other files (logo, buildTransitive) don't make the build rewrite the DLL.
 */
function newestSourceMtime(dir = libraryProject) {
  let newest = 0;
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (entry.name === 'bin' || entry.name === 'obj') continue;
    const path = join(dir, entry.name);
    if (entry.isDirectory()) newest = Math.max(newest, newestSourceMtime(path));
    else if (/\.(cs|csproj)$/.test(entry.name)) newest = Math.max(newest, statSync(path).mtimeMs);
  }
  return newest;
}

/** Why the library build for this TFM can't be used, or null when it is present and up to date. */
export function libraryBuildProblem(targetFramework) {
  const dll = libraryDll(targetFramework);
  if (!existsSync(dll)) return `the library has not been built for ${targetFramework} (${dll} is missing)`;
  if (statSync(dll).mtimeMs < newestSourceMtime()) return 'the library build is older than its source files';
  return null;
}
