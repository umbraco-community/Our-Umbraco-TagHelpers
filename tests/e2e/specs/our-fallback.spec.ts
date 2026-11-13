import { test, expect, section } from '../fixtures';

test('uses the culture variant when it exists, otherwise the tag content', async ({ page }) => {
  await page.goto('/en/');
  await expect(section(page, 'our-fallback').locator('h3')).toHaveText([
    'DA Header',
    'I do NOT have a da-DK culture variant of this property',
  ]);
});
