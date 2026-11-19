import { test, expect, section, type Page } from '../fixtures';

// https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/14
const demo = (page: Page, name: string) => section(page, 'our-bytes').locator(`[data-demo="${name}"]`);

test('shows the file size of a media item', async ({ page }) => {
  await page.goto('/en/');
  // The image media item's umbracoBytes is 1152302
  await expect(demo(page, 'media-item')).toHaveText('1.1 MB');
});

test('shows a value in the largest unit, with the decimals asked for', async ({ page }) => {
  await page.goto('/en/');
  await expect(demo(page, 'value')).toHaveText('1.5 KB');
  await expect(demo(page, 'decimals')).toHaveText('1.18 MB');
});

test('formats the number for the page culture', async ({ page }) => {
  await page.goto('/da/');
  await expect(demo(page, 'value')).toHaveText('1,5 KB');
  await expect(demo(page, 'media-item')).toHaveText('1,1 MB');
});

test('shows nothing without a value, and never the our-bytes element itself', async ({ page }) => {
  await page.goto('/en/');
  await expect(demo(page, 'empty')).toHaveText('');
  await expect(section(page, 'our-bytes').locator('our-bytes')).toHaveCount(0);
});
