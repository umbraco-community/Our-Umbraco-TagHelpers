import { test, expect, section } from '../fixtures';

test('rewrites third party assets to locally hosted copies', async ({ page }) => {
  await page.goto('/en/');
  const scripts = section(page, 'our-self-host').locator('script');

  await expect(scripts).toHaveCount(3);
  await expect(scripts.nth(0)).toHaveAttribute('src', '/assets/alpinejs@3.10.5/dist/cdn.min.js');
  await expect(scripts.nth(1)).toHaveAttribute('src', '/assets/libraries/alpinejs@3.10.5/dist/cdn.min.js');
  await expect(scripts.nth(2)).toHaveAttribute('src', '/assets/alpinejs.js');
  await expect(scripts.nth(0)).toHaveAttribute('data-original-src', 'https://unpkg.com/alpinejs@3.10.5/dist/cdn.min.js');
});

test('serves the downloaded file', async ({ page, request }) => {
  await page.goto('/en/');
  const src = await section(page, 'our-self-host').locator('script').first().getAttribute('src');

  const response = await request.get(src!);
  expect(response.status()).toBe(200);
  expect((await response.body()).length).toBeGreaterThan(1_000);
});

test('rewrites href to a locally hosted copy and serves it', async ({ page, request }) => {
  await page.goto('/en/');
  const link = section(page, 'our-self-host').locator('link[rel="stylesheet"]');

  await expect(link).toHaveAttribute('href', '/assets/normalize.css@8.0.1/normalize.css');
  await expect(link).toHaveAttribute('data-original-href', 'https://unpkg.com/normalize.css@8.0.1/normalize.css');

  const response = await request.get('/assets/normalize.css@8.0.1/normalize.css');
  expect(response.status()).toBe(200);
  expect(await response.text()).toContain('normalize.css');
});
