// Builds Our.Umbraco.TagHelpers (all target frameworks) once, before the test sites are published.
// Runs automatically as the `pretest` step of `npm test`; also available as `npm run build:library`.

import { spawnSync } from 'node:child_process';
import { configuration, libraryProject } from './library.mjs';

console.log('Building Our.Umbraco.TagHelpers...');
const build = spawnSync(
  'dotnet',
  ['build', libraryProject, '-c', configuration, '-nologo', '-v', 'q', '-clp:ErrorsOnly;NoSummary'],
  { stdio: 'inherit' },
);
process.exit(build.status ?? 1);
