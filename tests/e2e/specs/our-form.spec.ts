import { test, expect, section } from '../fixtures';

test('adds the encoded ufprt route field for the surface controller', async ({ page }) => {
  await page.goto('/en/');
  const form = section(page, 'our-form').locator('form#testContactForm');

  await expect(form).toHaveAttribute('method', 'post');
  const ufprt = form.locator('input[type="hidden"][name="ufprt"]');
  await expect(ufprt).toHaveCount(1);
  expect(await ufprt.getAttribute('value')).toMatch(/\S{20,}/);
});

test('invalid input shows the validation messages', async ({ page }) => {
  await page.goto('/en/');
  const sample = section(page, 'our-form');

  await sample.locator('form#testContactForm input[type="submit"]').click();

  await expect(sample.locator('span.field-validation-error')).toHaveCount(3);
  for (const message of await sample.locator('span.field-validation-error').all()) {
    await expect(message).not.toBeEmpty();
  }
  await expect(sample.locator('.form-success')).toHaveCount(0);
});

test('valid input reaches the surface controller and redirects back', async ({ page }) => {
  await page.goto('/en/');
  const sample = section(page, 'our-form');
  const form = sample.locator('form#testContactForm');

  await form.locator('input[name="Name"]').fill('E2E Tester');
  await form.locator('input[name="Email"]').fill('e2e@example.com');
  await form.locator('textarea[name="Message"]').fill('Hello from Playwright');
  await form.locator('input[type="submit"]').click();

  await expect(page).toHaveURL(/\/en\/$/);
  await expect(sample.locator('.form-success')).toHaveText('Thanks E2E Tester, your message was sent.');
});

test('umb-area routes to the surface controller in that area', async ({ page }) => {
  await page.goto('/en/');
  const sample = section(page, 'our-form');
  const form = sample.locator('form#testAreaForm');

  await form.locator('input[name="name"]').fill('E2E Area');
  await form.locator('button[type="submit"]').click();

  await expect(sample.locator('.area-form-success')).toHaveText('Area form received E2E Area');
});

// https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/93
test('our-controller with the Controller suffix reaches the surface controller', async ({ page }) => {
  await page.goto('/en/');
  const sample = section(page, 'our-form');
  const form = sample.locator('form#testSuffixForm');

  await form.locator('input[name="Name"]').fill('E2E Suffix');
  await form.locator('input[name="Email"]').fill('suffix@example.com');
  await form.locator('textarea[name="Message"]').fill('Posted with ContactFormController');
  await form.locator('input[type="submit"]').click();

  await expect(page).toHaveURL(/\/en\/$/);
  await expect(sample.locator('.form-success')).toHaveText('Thanks E2E Suffix, your message was sent.');
});

// https://github.com/umbraco-community/Our-Umbraco-TagHelpers/security/code-scanning/1
// The demo surface controllers validate the anti-forgery token that ASP.NET's form tag helper adds to each form,
// so a cross-site POST, which can't read the token, is rejected.
for (const [formId, fields] of [
  ['testContactForm', { Name: 'Forged', Email: 'forged@example.com', Message: 'Cross-site post' }],
  ['testSuffixForm', { Name: 'Forged', Email: 'forged@example.com', Message: 'Cross-site post' }],
  ['testAreaForm', { name: 'Forged' }],
] as const) {
  test(`a post to #${formId} without the anti-forgery token is rejected`, async ({ page }) => {
    await page.goto('/en/');
    const form = section(page, 'our-form').locator(`form#${formId}`);

    // The real form carries the token
    await expect(form.locator('input[type="hidden"][name="__RequestVerificationToken"]')).toHaveCount(1);

    // The same post, with the route field (ufprt) but without the token
    const ufprt = await form.locator('input[name="ufprt"]').getAttribute('value');
    const response = await page.request.post('/en/', { form: { ...fields, ufprt: ufprt! }, maxRedirects: 0 });

    expect(response.status()).toBe(400);
  });
}
