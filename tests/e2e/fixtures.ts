import { test as base, expect, type Locator, type Page } from '@playwright/test';

export { expect, type Page };

/** `test` with the Umbraco major of the current project (v16..v19) available as a fixture. */
export const test = base.extend<{ umbracoMajor: number }>({
  umbracoMajor: async ({}, use, testInfo) => {
    await use(Number(testInfo.project.metadata.umbracoMajor));
  },
});

/** A tag helper sample on the demo site, wrapped in `<section data-sample="name">` (see Views/Home.cshtml). */
export function section(page: Page, name: string): Locator {
  return page.locator(`section[data-sample="${name}"]`);
}

/** Logs into the backoffice with the TestSite's unattended install user (overridable via env). */
export async function loginToBackoffice(page: Page): Promise<void> {
  const email = process.env.E2E_BACKOFFICE_EMAIL ?? 'warren@hackmakedo.com';
  const password = process.env.E2E_BACKOFFICE_PASSWORD ?? 'password1234';

  await page.goto('/umbraco');
  // The login page is a web component; CSS locators pierce its shadow DOM.
  const emailInput = page.locator('input[type="email"], input[name="username"]').first();
  await emailInput.waitFor({ timeout: 60_000 });
  await emailInput.fill(email);
  await page.locator('input[type="password"]').first().fill(password);
  await page.getByRole('button', { name: 'Login' }).click();
  await page.waitForURL('**/umbraco/section/content**', { timeout: 60_000 });
}

/** Registers (and is thereby logged in as) a brand new member using the demo register form. */
export async function registerMember(page: Page): Promise<{ name: string; email: string }> {
  const id = `${Date.now()}-${Math.floor(Math.random() * 1e6)}`;
  const name = `E2E Member ${id}`;
  const email = `e2e-${id}@example.com`;
  const password = `E2e-${id}-Passw0rd!`;

  await page.goto('/en/');
  const form = page.locator('form', { has: page.getByRole('button', { name: 'Register' }) });
  await form.locator('input[name$="Name"]').first().fill(name);
  await form.locator('input[autocomplete="username"]').fill(email);
  await form.locator('input[autocomplete="new-password"]').nth(0).fill(password);
  await form.locator('input[autocomplete="new-password"]').nth(1).fill(password);
  await form.getByRole('button', { name: 'Register' }).click();
  await expect(page.getByText('Welcome back')).toBeVisible();

  return { name, email };
}
