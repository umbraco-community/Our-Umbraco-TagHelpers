import { test, expect, section, type Page } from '../fixtures';

const visibleLines = (page: Page) => section(page, 'our-user-include-exclude').locator('div');

test('anonymous visitor sees only the anonymous variants', async ({ page }) => {
  await page.goto('/en/');
  await expect(visibleLines(page)).toHaveText([
    'Only ✅ Anonymous users will see this.',
    'Everyone except ❌ Editor users can see this.',
    'Everyone except ❌ Admin or ❌ Editor users can see this.',
    'Everyone except ❌ logged in users will see this.',
  ]);
});

test('backoffice admin sees the admin and logged-in variants', async ({ backofficePage: page }) => {
  await page.goto('/en/');
  await expect(visibleLines(page)).toHaveText([
    'Only users in the ✅ Admin group will see this.',
    'Only users in the ✅ Admin or ✅ Editor user group will see this.',
    'Only ✅ logged in users will see this.',
    'Everyone except ❌ Editor users can see this.',
    'Everyone except ❌ Admin or ❌ Editor users can see this.',
    'Everyone except ❌ Anonymous users will see this.',
  ]);
});
