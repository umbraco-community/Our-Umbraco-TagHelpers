import { test, expect, section } from '../fixtures';

test('inlines both the file and the media item SVG', async ({ page }) => {
  await page.goto('/en/');
  const sample = section(page, 'our-svg');

  // One from src="/samples/partying-face.svg", one from the Logo media item
  await expect(sample.locator('svg')).toHaveCount(2);
  await expect(sample.locator('img')).toHaveCount(0);
});

test('ensure-viewbox adds a viewBox to each inlined SVG', async ({ page }) => {
  await page.goto('/en/');
  const svgs = section(page, 'our-svg').locator('div > svg');

  await expect(svgs).toHaveCount(2);
  for (const svg of await svgs.all()) {
    expect(await svg.evaluate((el) => el.getAttribute('viewBox') ?? el.getAttribute('viewbox'))).toMatch(/^0 0 \d+(\.\d+)? \d+(\.\d+)?$/);
  }
});
