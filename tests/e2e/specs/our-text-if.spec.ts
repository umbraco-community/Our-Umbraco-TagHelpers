import { test, expect, section } from '../fixtures';

// https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/54
const demo = (page: import('@playwright/test').Page, name: string) => section(page, 'our-text-if').locator(`[data-demo="${name}"]`);

test('fills the element with the text', async ({ page }) => {
  await page.goto('/en/');
  await expect(demo(page, 'with-text')).toHaveText('EN Header');
});

test('fills the element with the text of the page culture', async ({ page }) => {
  await page.goto('/da/');
  await expect(demo(page, 'with-text')).toHaveText('DA Header');
});

test('leaves the element out when there is no text', async ({ page }) => {
  await page.goto('/en/');
  await expect(demo(page, 'empty')).toHaveCount(0);
  await expect(section(page, 'our-text-if').locator('h3')).toHaveCount(1); // only the "with-text" heading
});

test('shows markup in the text as text', async ({ page }) => {
  await page.goto('/en/');
  const markup = demo(page, 'markup');

  await expect(markup).toHaveText('<em>Shown as text, not emphasised</em>');
  await expect(markup.locator('em')).toHaveCount(0);
});
