import { test, loginToBackoffice, loginMember, authFile, demoPeople, type Identity, type Page } from '../fixtures';

// Runs once per Umbraco version before that version's tests (see playwright.config.ts).
// Each login is saved as a session the specs reuse through fixtures (backofficePage, editorPage, ...).
// The demo people are created by the TestSite on first boot (Migrations/AddDemoPeople.cs).

// The first logins after boot wait for the backoffice to warm up (see loginToBackoffice)
test.describe.configure({ timeout: 90_000 });

async function saveSession(page: Page, umbracoMajor: number, identity: Identity) {
  await page.context().storageState({ path: authFile(umbracoMajor, identity) });
}

test('log into the backoffice', async ({ page, umbracoMajor }) => {
  await loginToBackoffice(page);
  await saveSession(page, umbracoMajor, 'backoffice');
});

test('log in as the demo Editor user', async ({ page, umbracoMajor }) => {
  await loginToBackoffice(page, demoPeople.editor);
  await saveSession(page, umbracoMajor, 'editor');
});

test('log in as the demo user without content access', async ({ page, umbracoMajor }) => {
  await loginToBackoffice(page, demoPeople.noContentUser);
  await saveSession(page, umbracoMajor, 'no-content-user');
});

test('log in as the demo Staff member', async ({ page, umbracoMajor }) => {
  await loginMember(page, demoPeople.staffMember);
  await saveSession(page, umbracoMajor, 'staff-member');
});
