import { test, expect, section, registerMember, type Page } from '../fixtures';

// Only the include/exclude sample lines: the section also contains the login/register forms
const sampleLines = (page: Page) => section(page, 'our-member-include-exclude').locator(':scope > div, h4 ~ div').filter({ hasText: /(Only|Everyone except)/ });

test('anonymous visitor sees only the anonymous variants', async ({ page }) => {
  await page.goto('/en/');
  await expect(sampleLines(page)).toHaveText([
    'Only ✅ Anonymous members will see this.',
    'Everyone except ❌ Staff members can see this.',
    'Everyone except ❌ logged in users will see this.',
  ]);
});

// The demo has no member groups, so the Staff/Admins include lines stay hidden for any member
test('logged in member sees the logged-in variants', async ({ page }) => {
  await registerMember(page);
  await page.goto('/en/');
  await expect(sampleLines(page)).toHaveText([
    'Only ✅ logged in members will see this.',
    'Everyone except ❌ Staff members can see this.',
    'Everyone except ❌ Anonymous users will see this.',
  ]);
});
