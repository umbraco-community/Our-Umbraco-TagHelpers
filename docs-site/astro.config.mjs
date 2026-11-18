// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';

const repo = 'https://github.com/umbraco-community/Our-Umbraco-TagHelpers';

// Served from GitHub Pages at https://umbraco-community.github.io/Our-Umbraco-TagHelpers/
// To move to a custom domain later, set `site` to it and remove `base`.
export default defineConfig({
	site: 'https://umbraco-community.github.io',
	base: '/Our-Umbraco-TagHelpers',
	trailingSlash: 'always',
	// Contributors' GitHub avatars are downloaded and resized at build time
	image: { remotePatterns: [{ protocol: 'https', hostname: 'avatars.githubusercontent.com' }] },
	integrations: [
		starlight({
			title: 'Our Umbraco TagHelpers',
			description:
				'A community package of ASP.NET Core Razor tag helpers for Umbraco CMS 16, 17, 18 and 19.',
			favicon: '/favicon.png',
			social: [{ icon: 'github', label: 'GitHub', href: repo }],
			editLink: { baseUrl: `${repo}/edit/v2/dev/docs-site/` },
			customCss: [
				'@fontsource-variable/literata/opsz.css',
				'@fontsource-variable/source-sans-3/wght.css',
				'@fontsource-variable/jetbrains-mono/wght.css',
				'./src/styles/brand.css',
			],
			components: {
				SiteTitle: './src/components/SiteTitle.astro',
				PageTitle: './src/components/PageTitle.astro',
				ThemeProvider: './src/components/ThemeProvider.astro',
				ThemeSelect: './src/components/ThemeSelect.astro',
			},
			routeMiddleware: './src/route-middleware.ts',
			// Code block themes and styles are in ec.config.mjs
			sidebar: [
				{ label: 'Getting started', items: [{ autogenerate: { directory: 'getting-started' } }] },
				{ label: 'Tag helpers', items: [{ autogenerate: { directory: 'tag-helpers' } }] },
				{ label: 'Release notes', slug: 'release-notes' },
				{ label: 'Contributors', slug: 'contributors' },
				{ label: 'Contributing', items: [{ autogenerate: { directory: 'contributing' } }] },
			],
		}),
	],
});
