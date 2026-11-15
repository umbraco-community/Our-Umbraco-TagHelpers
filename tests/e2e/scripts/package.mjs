// The Our.Umbraco.TagHelpers NuGet package used by `npm run test:package`.
// scripts/test-package.mjs packs it into tests/e2e/.data/package-feed/ and records its version there;
// scripts/start-site.mjs then publishes each test site against that package instead of the project.

import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { repoRoot } from './library.mjs';

export const packageFeed = join(repoRoot, 'tests', 'e2e', '.data', 'package-feed');
export const packageVersionFile = join(packageFeed, 'version.txt');

/** True when the tests run against the packed package (set by scripts/test-package.mjs). */
export function usePackage() {
  return process.env.E2E_TAGHELPERS_SOURCE === 'package';
}

/** Version of the package in the feed, or null when nothing has been packed. */
export function packedVersion() {
  return existsSync(packageVersionFile) ? readFileSync(packageVersionFile, 'utf8').trim() : null;
}
