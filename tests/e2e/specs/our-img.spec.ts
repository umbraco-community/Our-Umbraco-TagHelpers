import { test, expect, section, type Page } from '../fixtures';

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

// The test sites sign image requests (an HMAC key, as Umbraco 17+ generates on install), so the
// resize URLs must carry Umbraco's hmac or ImageSharp answers 400
test('file images keep root-relative paths and are signed', async ({ page }) => {
  await page.goto('/en/');
  const sample = section(page, 'our-img');
  const urls = [
    ...(await sample.locator('img[src*="cute-unicorn"]').evaluateAll((els) => els.map((e) => e.getAttribute('src')!))),
    ...(await sample.locator('source[srcset*="cute-unicorn"]').evaluateAll((els) => els.map((e) => e.getAttribute('srcset')!))),
  ];

  expect(urls.length).toBeGreaterThanOrEqual(3);
  for (const url of urls) expect(url).toMatch(/^\/samples\/cute-unicorn\.jpg\?width=\d+&height=\d+&hmac=[0-9a-f]+$/);
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

const classAndStyleImg = (page: Page) => section(page, 'our-img').locator('img[src^="/samples/cute-unicorn.jpg?width=300"]');

test('class is applied to the img', async ({ page }) => {
  await page.goto('/en/');
  const img = classAndStyleImg(page);

  await expect(img).toHaveClass(/demo-img demo-img--rounded/);
  await expect(img).toHaveAttribute('width', '300');
  await expect(img).toHaveAttribute('height', '150');
});

// `style` is bound to ImgTagHelper.ImgStyle, so Razor removes it and the tag helper must write it back:
// https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/107
test('style is applied to the img', async ({ page }) => {
  await page.goto('/en/');
  await expect(classAndStyleImg(page)).toHaveAttribute('style', /border: 2px solid green/);
});

test('large breakpoints render sources with the large media queries', async ({ page }) => {
  await page.goto('/en/');
  const picture = section(page, 'our-img').locator('picture').last();

  const large = picture.locator('source[media="(min-width: 992px)"]');
  await expect(large).toHaveAttribute('width', '500');
  await expect(large).toHaveAttribute('height', '250');

  const extraLarge = picture.locator('source[media="(min-width: 1200px)"]');
  await expect(extraLarge).toHaveAttribute('width', '600');
  await expect(extraLarge).toHaveAttribute('srcset', new RegExp(`^${mediaFile}\\?cc=`));

  const extraExtraLarge = picture.locator('source[media="(min-width: 1400px)"]');
  await expect(extraExtraLarge).toHaveAttribute('width', '800');
  await expect(extraExtraLarge).toHaveAttribute('srcset', new RegExp(`^${mediaFile}\\?cc=`));
});

// https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/51
const fallbackDemo = (page: Page, name: string) => section(page, 'our-img').locator(`[data-demo="${name}"] img`);

test('fallback-img is shown when the src file is missing', async ({ page }) => {
  await page.goto('/en/');
  const img = fallbackDemo(page, 'fallback-missing-src');

  await expect(img).toHaveAttribute('src', /^\/samples\/cute-unicorn\.jpg\?width=150&height=100&hmac=/);
  await expect(img).toHaveAttribute('alt', 'Missing file');
});

test('fallback-img is shown when no media item was picked', async ({ page }) => {
  await page.goto('/en/');
  await expect(fallbackDemo(page, 'fallback-no-media')).toHaveAttribute('src', /^\/samples\/cute-unicorn\.jpg\?width=150&height=100&hmac=/);
});

test('fallback-img is not used when the src file exists', async ({ page }) => {
  await page.goto('/en/');
  await expect(fallbackDemo(page, 'fallback-not-needed')).toHaveAttribute('src', /^\/samples\/cute-unicorn\.jpg\?width=150&height=100&hmac=/);
  await expect(section(page, 'our-img').locator('img[src*="this-fallback-is-not-used"]')).toHaveCount(0);
});

test('a fallback image loads', async ({ page }) => {
  await page.goto('/en/');
  const img = fallbackDemo(page, 'fallback-missing-src');
  await img.scrollIntoViewIfNeeded();
  await expect.poll(() => img.evaluate((el: HTMLImageElement) => el.complete && el.naturalWidth)).toBeGreaterThan(0);
});
