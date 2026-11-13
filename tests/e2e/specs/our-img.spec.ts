import { test, expect, section } from '../fixtures';

const mediaFile = '/media/wqmnqu3q/ashim-d-silva-weyamle9fdm-unsplash.jpg';

test('media image without alt uses the media name and original size', async ({ page }) => {
  await page.goto('/en/');
  const img = section(page, 'our-img').locator('img').nth(0);

  await expect(img).toHaveAttribute('alt', 'ashim-d-silva-WeYamle9fDM-unsplash.jpg');
  await expect(img).toHaveAttribute('width', '3072');
  await expect(img).toHaveAttribute('height', '2048');
  await expect(img).toHaveAttribute('src', new RegExp(`^${mediaFile}\\?.*width=3072`));
  await expect(img).toHaveAttribute('loading', 'lazy');
});

test('custom alt text, scaled height and above-the-fold loading', async ({ page }) => {
  await page.goto('/en/');
  const img = section(page, 'our-img').locator('img[alt="Custom Alt Text"]');

  await expect(img).toHaveAttribute('width', '200');
  await expect(img).toHaveAttribute('height', '133.33333333333331');
  await expect(img).toHaveAttribute('loading', 'eager');
  await expect(img).toHaveAttribute('fetchpriority', 'high');
});

test('media image renders responsive crop sources', async ({ page }) => {
  await page.goto('/en/');
  const picture = section(page, 'our-img').locator('picture').nth(0);

  await expect(picture.locator('source[media="(min-width: 768px)"]')).toHaveAttribute('width', '600');
  await expect(picture.locator('source[media="(min-width: 576px)"]')).toHaveAttribute('width', '400');
  await expect(picture.locator('source').first()).toHaveAttribute('srcset', new RegExp(`^${mediaFile}\\?cc=`));
});

test('file images keep root-relative paths', async ({ page }) => {
  await page.goto('/en/');
  const sample = section(page, 'our-img');
  const urls = [
    ...(await sample.locator('img[src*="cute-unicorn"]').evaluateAll((els) => els.map((e) => e.getAttribute('src')!))),
    ...(await sample.locator('source[srcset*="cute-unicorn"]').evaluateAll((els) => els.map((e) => e.getAttribute('srcset')!))),
  ];

  expect(urls.length).toBeGreaterThanOrEqual(3);
  for (const url of urls) expect(url).toMatch(/^\/samples\/cute-unicorn\.jpg\?width=\d+&height=\d+$/);
});

test('every rendered image URL resolves', async ({ page, request }) => {
  await page.goto('/en/');
  const sample = section(page, 'our-img');
  const urls = new Set([
    ...(await sample.locator('img').evaluateAll((els) => els.map((e) => e.getAttribute('src')!))),
    ...(await sample.locator('source').evaluateAll((els) => els.map((e) => e.getAttribute('srcset')!))),
  ]);

  for (const url of urls) {
    // Resolve relative to the page the browser is on, exactly as it would
    const response = await request.get(new URL(url, page.url()).toString());
    expect(response.status(), url).toBe(200);
  }
});
