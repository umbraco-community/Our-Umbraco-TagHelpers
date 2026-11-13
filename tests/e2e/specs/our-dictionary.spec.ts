import { test, expect, section } from '../fixtures';

test('renders the translation for the page culture', async ({ page }) => {
  await page.goto('/en/');
  await expect(section(page, 'our-dictionary').locator('h3')).toHaveText([
    'Home',
    'Test in DK - fellback to this as no EN value set',
    'I will render as no key exists',
  ]);
});

test('renders the Danish translation on the Danish site', async ({ page }) => {
  await page.goto('/da/');
  await expect(section(page, 'our-dictionary').locator('h3').first()).toHaveText('Hjem');
});

test('renders the Spanish translation on the Spanish site', async ({ page }) => {
  await page.goto('/es/');
  await expect(section(page, 'our-dictionary').locator('h3').first()).toHaveText('Home ES');
});
