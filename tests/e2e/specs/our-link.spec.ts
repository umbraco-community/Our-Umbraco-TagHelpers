import { test, expect, section } from '../fixtures';

test('wraps child content in the picked link', async ({ page }) => {
  await page.goto('/en/');
  const link = section(page, 'our-link').locator('a', { has: page.getByRole('heading', { name: 'Hi There' }) });

  await expect(link).toHaveAttribute('href', 'https://google.com');
  await expect(link).toHaveAttribute('target', '_blank');
});

test('uses the link name when there is no child content', async ({ page }) => {
  await page.goto('/en/');
  const link = section(page, 'our-link').getByRole('link', { name: 'Something about Google', exact: true });

  await expect(link).toHaveAttribute('href', 'https://google.com');
});

test('falls back to the given element when the link is empty', async ({ page }) => {
  await page.goto('/en/');
  const sample = section(page, 'our-link');

  await expect(sample.locator('div > span')).toHaveText('Some text tha will fallback to wrapping div with a span inside');
  await expect(sample.locator(':scope > span')).toHaveText('Some text tha will fallback to span');
});
