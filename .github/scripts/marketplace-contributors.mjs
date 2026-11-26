// Writes the repository's contributors into umbraco-marketplace.json (AuthorDetails.Contributors), for the
// "Co-authors" on the Umbraco Marketplace listing. The Marketplace can read them from GitHub itself
// (SyncContributorsFromRepository), but then there's no way to leave anyone out.
//
// Run from the repository root: node .github/scripts/marketplace-contributors.mjs
// Set GITHUB_TOKEN (or GH_TOKEN) to avoid GitHub's rate limit for anonymous requests.
// .github/workflows/marketplace.yml runs it on every push to v2/dev and weekly, and opens a pull request
// when the list changes.

import { readFileSync, writeFileSync } from 'node:fs';

const repo = 'umbraco-community/Our-Umbraco-TagHelpers';
const file = 'umbraco-marketplace.json';

// Left out, as lowercase GitHub usernames:
// - the package author, who the listing already shows above the co-authors,
// - AI coding agents that commit with an ordinary user account (the docs site's contributors loader,
//   docs-site/src/loaders/github-contributors.ts, leaves out the same ones).
// Bots ([bot] accounts) are always left out.
const excluded = new Set(['warrenbuckley', 'claude']);

const token = process.env.GITHUB_TOKEN || process.env.GH_TOKEN;
const headers = {
	Accept: 'application/vnd.github+json',
	'User-Agent': 'our-umbraco-taghelpers-marketplace-contributors',
	...(token ? { Authorization: `Bearer ${token}` } : {}),
};

async function get(url) {
	const response = await fetch(url, { headers });
	if (!response.ok) throw new Error(`GET ${url}: ${response.status} ${await response.text()}`);
	return response;
}

// Every page of contributors
const logins = [];
for (let url = `https://api.github.com/repos/${repo}/contributors?per_page=100`; url; ) {
	const response = await get(url);
	for (const c of await response.json()) {
		if (c.type === 'User' && !c.login.endsWith('[bot]') && !excluded.has(c.login.toLowerCase())) logins.push(c.login);
	}
	url = /<([^>]+)>;\s*rel="next"/.exec(response.headers.get('link') ?? '')?.[1];
}

// Display names from each profile, falling back to the username, as the Marketplace does
const contributors = await Promise.all(
	logins.map(async (login) => {
		const user = await (await get(`https://api.github.com/users/${login}`)).json();
		return { Name: (user.name?.trim() || login).slice(0, 100), Url: user.html_url };
	}),
);
contributors.sort((a, b) => a.Name.localeCompare(b.Name, 'en', { sensitivity: 'base' }));

const listing = JSON.parse(readFileSync(file, 'utf8'));
listing.AuthorDetails.SyncContributorsFromRepository = false;
listing.AuthorDetails.Contributors = contributors;
writeFileSync(file, JSON.stringify(listing, null, 4) + '\n');

console.log(`${contributors.length} contributors: ${contributors.map((c) => c.Name).join(', ')}`);
