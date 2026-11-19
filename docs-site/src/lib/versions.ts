import { getCollection } from 'astro:content';

/** Compares two versions such as 2.1.0 and 2.1.0-rc1. A prerelease sorts below its release. */
export function compareVersions(a: string, b: string): number {
	const [coreA, preA] = a.replace(/^v/, '').split('-', 2);
	const [coreB, preB] = b.replace(/^v/, '').split('-', 2);
	const partsA = coreA.split('.').map(Number);
	const partsB = coreB.split('.').map(Number);
	for (let i = 0; i < 3; i++) {
		const diff = (partsA[i] ?? 0) - (partsB[i] ?? 0);
		if (diff !== 0) return diff;
	}
	if (preA === preB) return 0;
	if (!preA) return 1;
	if (!preB) return -1;
	return preA.localeCompare(preB, 'en', { numeric: true });
}

let cache: Promise<ReleaseInfo> | undefined;

interface ReleaseInfo {
	/** Newest stable (non-prerelease) version published on GitHub, if any could be loaded. */
	latestStable: string | undefined;
	/** False when GitHub couldn't be reached, so nothing can be called unreleased. */
	known: boolean;
}

export function getReleaseInfo(): Promise<ReleaseInfo> {
	cache ??= (async () => {
		const releases = await getCollection('releases');
		const stable = releases
			.filter((r) => !r.data.prerelease)
			.map((r) => r.data.version)
			.sort(compareVersions);
		return { latestStable: stable.at(-1), known: releases.length > 0 };
	})();
	return cache;
}

/** True when `version` hasn't had a stable release yet (it's on v2/dev, or only in a release candidate). */
export async function isUnreleased(version: string): Promise<boolean> {
	const { latestStable, known } = await getReleaseInfo();
	if (!known || !latestStable) return false;
	return compareVersions(version, latestStable) > 0;
}

/**
 * True when `version` is in the newest minor line: either not released yet, or in the same
 * major.minor as the latest stable release. Pages added then get a "New" badge in the sidebar.
 */
export async function isNew(version: string): Promise<boolean> {
	const { latestStable, known } = await getReleaseInfo();
	if (!known || !latestStable) return false;
	if (await isUnreleased(version)) return true;
	const [major, minor] = latestStable.split('.');
	const [vMajor, vMinor] = version.split('.');
	return major === vMajor && minor === vMinor;
}
