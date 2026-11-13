import { test, expect, section } from '../fixtures';

test('renders the navigation for every child of the root', async ({ page }) => {
  await page.goto('/en/page-a/');
  const anchors = section(page, 'our-active-class').locator('a.nav-link');

  await expect(anchors.filter({ hasText: 'Page A' }).first()).toHaveAttribute('href', '/en/page-a/');
  await expect(anchors.filter({ hasText: 'Page B' }).first()).toHaveAttribute('href', '/en/page-b/');
  await expect(anchors.filter({ hasText: 'Page C' }).first()).toHaveAttribute('href', '/en/page-c/');
});

// Known limitation, documented in the sample ("does not work with domain cultures mapped to it"):
// every page of the demo site sits under a culture domain (/en, /es, /da), so nothing is marked active.
// Same on Umbraco 16-19. Tracked in https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/97 — remove fixme when fixed.
test.fixme('marks the current page as active', async ({ page }) => {
  await page.goto('/en/page-a/');
  const sample = section(page, 'our-active-class');

  await expect(sample.locator('a.nav-link--active')).toHaveText('Page A');
  await expect(sample.locator('li.selected > a').first()).toHaveText('Page A');
});
