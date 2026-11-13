import { test, expect, section } from '../fixtures';

// The sample shows one line in even minutes and the other in odd minutes (server UTC time),
// so exactly one of them must be rendered - never both, never neither.
test('renders exactly one of the even/odd minute elements', async ({ page }) => {
  await page.goto('/en/');
  const lines = section(page, 'our-if').locator('div').filter({ hasText: /This will only render during (even|odd) minutes/ });

  await expect(lines).toHaveCount(1);
});
