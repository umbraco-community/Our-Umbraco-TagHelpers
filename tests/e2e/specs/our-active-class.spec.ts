import { test, expect, section } from '../fixtures';

test('renders the navigation for every child of the root', async ({ page }) => {
  await page.goto('/en/page-a/');
  const anchors = section(page, 'our-active-class').locator('a.nav-link');

  await expect(anchors.filter({ hasText: 'Page A' }).first()).toHaveAttribute('href', '/en/page-a/');
  await expect(anchors.filter({ hasText: 'Page B' }).first()).toHaveAttribute('href', '/en/page-b/');
  await expect(anchors.filter({ hasText: 'Page C' }).first()).toHaveAttribute('href', '/en/page-c/');
});

// Every page of the demo site sits under a culture domain (/en, /es, /da): https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/97
test('marks the current page as active', async ({ page }) => {
  await page.goto('/en/page-a/');
  const sample = section(page, 'our-active-class');

  await expect(sample.locator('a.nav-link--active')).toHaveText('Page A');
  await expect(sample.locator('li.selected > a').first()).toHaveText('Page A');
});

test('marks an ancestor as active on a child page', async ({ page }) => {
  await page.goto('/en/page-a/fallback-demo/');
  const sample = section(page, 'our-active-class');

  await expect(sample.locator('a.nav-link--active')).toHaveText('Page A');
  await expect(sample.locator('li.selected > a')).toHaveText(['Page A', 'Fallback Demo']);
});

test('marks the active page in another culture', async ({ page }) => {
  // Follow the Danish navigation rather than assume its URL segments
  await page.goto('/da/');
  const href = await section(page, 'our-active-class').locator('a.nav-link').first().getAttribute('href');
  expect(href).toMatch(/^\/da\//);

  await page.goto(href!);
  const active = section(page, 'our-active-class').locator('a.nav-link--active');
  await expect(active).toHaveCount(1);
  await expect(active).toHaveAttribute('href', href!);
});
