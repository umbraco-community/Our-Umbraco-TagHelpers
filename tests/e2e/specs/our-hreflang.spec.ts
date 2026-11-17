import { test, expect, type Page } from '@playwright/test';

// https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/122
// The layout's <head> has <our-hreflang />. The home page varies by culture and is published in en-US (the default
// language), da and es behind the path domains /en, /da and /es; the other pages are invariant.
async function hreflangLinks(page: Page) {
  return page.locator('head link[rel="alternate"][hreflang]').evaluateAll((links) =>
    links.map((l) => ({ hreflang: l.getAttribute('hreflang'), href: l.getAttribute('href') })),
  );
}

for (const path of ['/en/', '/da/']) {
  test(`the home page lists every culture it's published in (${path})`, async ({ page }) => {
    await page.goto(path);
    const origin = new URL(page.url()).origin;

    const links = await hreflangLinks(page);

    // One link per culture, in the order Umbraco lists the languages (which can differ between installs, and
    // doesn't matter to search engines), then x-default to the default language (en-US)
    expect(links.slice(0, -1)).toHaveLength(3);
    expect(links.slice(0, -1)).toEqual(expect.arrayContaining([
      { hreflang: 'en-US', href: `${origin}/en/` },
      { hreflang: 'da', href: `${origin}/da/` },
      { hreflang: 'es', href: `${origin}/es/` },
    ]));
    expect(links.at(-1)).toEqual({ hreflang: 'x-default', href: `${origin}/en/` });
  });
}

test('an invariant page has no hreflang links', async ({ page }) => {
  await page.goto('/en/page-a/');
  expect(await hreflangLinks(page)).toEqual([]);
});
