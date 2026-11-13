import { test, loginToBackoffice, backofficeAuthFile } from '../fixtures';

// Runs once per Umbraco version before that version's tests (see playwright.config.ts).
test('log into the backoffice', async ({ page, umbracoMajor }) => {
  await loginToBackoffice(page);
  await page.context().storageState({ path: backofficeAuthFile(umbracoMajor) });
});
