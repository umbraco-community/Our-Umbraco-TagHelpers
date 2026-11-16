import { test, expect, section } from '../fixtures';

test('adds the encoded ufprt route field for the surface controller', async ({ page }) => {
  await page.goto('/en/');
  const form = section(page, 'our-form').locator('form#testContactForm');

  await expect(form).toHaveAttribute('method', 'post');
  const ufprt = form.locator('input[type="hidden"][name="ufprt"]');
  await expect(ufprt).toHaveCount(1);
  expect(await ufprt.getAttribute('value')).toMatch(/\S{20,}/);
});
