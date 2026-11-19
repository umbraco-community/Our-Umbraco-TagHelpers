// Syntax colours for code blocks, matching the site's palette: keywords and tags in violet blue,
// strings in green, comments muted, and attribute names and constants in a warm brown, so tag helper
// attributes such as our-if stand out. Every colour reads at 6:1 or better on the code background.
import { ExpressiveCodeTheme } from '@astrojs/starlight/expressive-code';

/** @param {{ name: string, type: 'dark' | 'light', c: Record<string, string> }} t */
function siteTheme({ name, type, c }) {
	return new ExpressiveCodeTheme({
		name,
		type,
		colors: {
			'editor.background': c.panel,
			'editor.foreground': c.ink,
			'editorGroupHeader.tabsBackground': c.ground,
			'tab.activeBackground': c.panel,
			'tab.activeForeground': c.ink,
			'tab.inactiveForeground': c.muted,
			'terminal.background': c.panel,
			'terminal.foreground': c.ink,
			'editor.selectionBackground': c.selection,
		},
		tokenColors: [
			{ scope: ['comment', 'punctuation.definition.comment'], settings: { foreground: c.muted, fontStyle: 'italic' } },
			{ scope: ['keyword', 'storage', 'storage.type', 'storage.modifier', 'keyword.control', 'keyword.operator.new'], settings: { foreground: c.keyword } },
			{ scope: ['string', 'string.quoted', 'punctuation.definition.string'], settings: { foreground: c.string } },
			{ scope: ['constant', 'constant.numeric', 'constant.language', 'support.constant', 'constant.character'], settings: { foreground: c.warm } },
			{ scope: ['entity.name.tag', 'meta.tag', 'punctuation.definition.tag'], settings: { foreground: c.keyword } },
			// Tag helper attributes such as our-if are what these docs are about, so they get the warm colour
			{ scope: ['entity.other.attribute-name'], settings: { foreground: c.warm } },
			{ scope: ['support.type.property-name', 'meta.object-literal.key'], settings: { foreground: c.warm } },
			{ scope: ['entity.name.type', 'entity.name.class', 'support.class', 'support.type'], settings: { foreground: c.ink } },
			{ scope: ['entity.name.function', 'support.function'], settings: { foreground: c.ink } },
			{ scope: ['keyword.operator', 'punctuation'], settings: { foreground: c.ink } },
			{ scope: ['markup.inserted'], settings: { foreground: c.string } },
			{ scope: ['markup.deleted'], settings: { foreground: c.warm } },
		],
	});
}

export const codeThemes = [
	siteTheme({
		name: 'th-dark',
		type: 'dark',
		c: {
			ground: '#1a1917',
			panel: '#23211e',
			ink: '#ece8e1',
			muted: '#b3ada3',
			keyword: '#9aa5ff',
			string: '#8fcf9a',
			warm: '#e8a77a',
			selection: '#33302b',
		},
	}),
	siteTheme({
		name: 'th-light',
		type: 'light',
		c: {
			ground: '#faf8f3',
			panel: '#ffffff',
			ink: '#1d1d1b',
			muted: '#57534e',
			keyword: '#3544b1',
			string: '#2f6b3a',
			warm: '#8a4510',
			selection: '#e4dfd4',
		},
	}),
];
