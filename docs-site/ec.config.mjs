// @ts-check
// Expressive Code (code block) settings. They live here rather than in astro.config.mjs because the
// themes are class instances: Markdown and MDX pages must share one copy, or each gets its own stylesheet.
import { defineEcConfig } from '@astrojs/starlight/expressive-code';
import { codeThemes } from './src/lib/code-themes.mjs';

export default defineEcConfig({
	themes: codeThemes,
	// Razor views are written ```cshtml in this repo's docs, but Shiki calls the grammar razor.
	// Shiki has no Mustache grammar; Handlebars uses the same {{ }} syntax.
	shiki: { langAlias: { cshtml: 'razor', mustache: 'handlebars' } },
	styleOverrides: {
		borderRadius: '8px',
		borderColor: 'var(--hmd-rule)',
		codeFontFamily: 'var(--__sl-font-mono)',
		uiFontFamily: 'var(--__sl-font)',
		frames: {
			shadowColor: 'transparent',
			editorActiveTabIndicatorTopColor: 'var(--hmd-pink)',
			editorTabBarBackground: 'var(--hmd-ground)',
			editorTabBarBorderBottomColor: 'var(--hmd-rule)',
			terminalTitlebarBackground: 'var(--hmd-ground)',
			terminalTitlebarBorderBottomColor: 'var(--hmd-rule)',
			terminalTitlebarForeground: 'var(--hmd-ink-muted)',
			terminalTitlebarDotsForeground: 'var(--hmd-border-strong)',
			terminalTitlebarDotsOpacity: '1',
		},
	},
});
