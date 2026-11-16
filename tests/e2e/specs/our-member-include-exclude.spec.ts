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

// A newly registered member is in no member groups, so the Staff/Admins include lines stay hidden
test('logged in member sees the logged-in variants', async ({ page }) => {
  await registerMember(page);
  await page.goto('/en/');
  await expect(sampleLines(page)).toHaveText([
    'Only ✅ logged in members will see this.',
    'Everyone except ❌ Staff members can see this.',
    'Everyone except ❌ Anonymous users will see this.',
  ]);
});

test('Staff member sees the Staff and logged-in variants', async ({ staffMemberPage: page }) => {
  await page.goto('/en/');
  await expect(sampleLines(page)).toHaveText([
    'Only members of ✅ Staff Member Group will see this.',
    'Only members of ✅ Staff OR ✅ Admins member group will see this.',
    'Only ✅ logged in members will see this.',
    'Everyone except ❌ Anonymous users will see this.',
  ]);
});
