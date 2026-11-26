# Our.Umbraco.TagHelpers documentation site

The source of https://umbraco-community.github.io/Our-Umbraco-TagHelpers/, built with [Astro Starlight](https://starlight.astro.build).

```bash
npm ci
npm run dev      # http://localhost:4321/Our-Umbraco-TagHelpers/
npm run build    # into dist/
npm run check    # type check
```

Pages are in `src/content/docs/`. How to add a tag helper page, mark the version it arrived in, and how the release notes work: see `src/content/docs/contributing/writing-docs.mdx` (the "Writing the docs" page on the site).

The site deploys from `v2/dev` with `.github/workflows/docs.yml`. Pull requests that change it get a preview on Cloudflare, linked on the pull request, from `.github/workflows/docs-preview.yml` (one-time setup at the top of that file).
