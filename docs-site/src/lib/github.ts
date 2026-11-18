import type { AstroIntegrationLogger } from 'astro';

/**
 * Fetches JSON from the GitHub REST API. Uses GITHUB_TOKEN when it's set (the docs workflow sets it,
 * so builds aren't rate limited). A rejected token is dropped and the request tried again without
 * it, as everything read here is public.
 */
export function createGitHubClient(logger: AstroIntegrationLogger) {
	const headers: Record<string, string> = {
		Accept: 'application/vnd.github+json',
		'X-GitHub-Api-Version': '2022-11-28',
		'User-Agent': 'our-umbraco-taghelpers-docs',
	};
	if (process.env.GITHUB_TOKEN) headers.Authorization = `Bearer ${process.env.GITHUB_TOKEN}`;

	async function get<T>(path: string): Promise<T> {
		const url = `https://api.github.com/${path}`;
		let res = await fetch(url, { headers });
		if (res.status === 401 && headers.Authorization) {
			logger.warn('GITHUB_TOKEN was rejected, so carrying on without it');
			delete headers.Authorization;
			res = await fetch(url, { headers });
		}
		if (!res.ok) throw new Error(`GitHub API responded ${res.status} ${res.statusText} for ${path}`);
		return (await res.json()) as T;
	}

	/** Every page of a list endpoint, 100 at a time (up to 1,000 items). */
	async function getAll<T>(path: string): Promise<T[]> {
		const items: T[] = [];
		const join = path.includes('?') ? '&' : '?';
		for (let page = 1; page <= 10; page++) {
			const batch = await get<T[]>(`${path}${join}per_page=100&page=${page}`);
			items.push(...batch);
			if (batch.length < 100) break;
		}
		return items;
	}

	return { get, getAll };
}
