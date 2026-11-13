import { test, expect, section, type Page } from '../fixtures';

test('uses the culture variant when it exists, otherwise the tag content', async ({ page }) => {
  await page.goto('/en/');
  await expect(section(page, 'our-fallback').locator('h3')).toHaveText([
    'DA Header',
    'I do NOT have a da-DK culture variant of this property',
  ]);
});

// Fallback Demo (/en/page-a/fallback-demo/) is a child of Page A with an empty header
const demo = (page: Page, name: string) => section(page, 'our-fallback').locator(`[data-demo="${name}"]`);

test('ancestors mode uses the nearest ancestor value', async ({ page }) => {
  await page.goto('/en/page-a/fallback-demo/');
  await expect(demo(page, 'ancestors')).toHaveText('Page A');
});

test("uses the page's own value when it has one", async ({ page }) => {
  await page.goto('/en/page-a/');
  await expect(demo(page, 'ancestors')).toHaveText('Page A');
  await expect(demo(page, 'content')).toHaveText('Page A');
});

test('falls back to the tag content when there is no value', async ({ page }) => {
  await page.goto('/en/page-a/fallback-demo/');
  await expect(demo(page, 'content')).toHaveText('This page has no header, so the tag content is shown');
});
