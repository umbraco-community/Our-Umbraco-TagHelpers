using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Our.Umbraco.TagHelpers.Utils
{
    /// <summary>
    /// Scopes the CSS in an inlined SVG's &lt;style&gt; blocks to that SVG, by prefixing every selector with a class
    /// that's added to the &lt;svg&gt;, so classes such as .st0 from two SVGs on one page don't restyle each other.
    /// </summary>
    internal static class SvgStyleScope
    {
        // At-rules whose block holds style rules, which are scoped. Others (@keyframes, @font-face, @page...) are kept as they are.
        private static readonly HashSet<string> RuleListAtRules = new(StringComparer.OrdinalIgnoreCase)
        {
            "media", "supports", "container", "layer", "document", "-moz-document",
        };

        // A selector for the <svg> itself: "svg", "svg .a", "svg:hover"...
        private static readonly Regex SvgTypeSelector = new(@"^svg(?=$|[\s.#:\[>+~])", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// A stable class for a file path or media key: "svg-" and 12 hex characters of its SHA-256 hash, so the output is
        /// the same on every render and different files get different classes.
        /// </summary>
        public static string ScopeClass(string key) =>
            "svg-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..12].ToLowerInvariant();

        /// <summary>
        /// Prefixes every selector in the CSS with the scope class, including inside @media, @supports, @container and
        /// @layer, and leaves other at-rules, comments, strings and a CDATA wrapper as they are.
        /// </summary>
        public static string ScopeCss(string css, string scopeClass)
        {
            if (string.IsNullOrWhiteSpace(css))
            {
                return css;
            }

            // Some editors wrap the CSS in <![CDATA[ ... ]]>
            const string cdataStart = "<![CDATA[", cdataEnd = "]]>";
            var start = css.IndexOf(cdataStart, StringComparison.Ordinal);
            var end = css.LastIndexOf(cdataEnd, StringComparison.Ordinal);
            if (start >= 0 && end > start)
            {
                var inner = css.Substring(start + cdataStart.Length, end - start - cdataStart.Length);
                return css[..(start + cdataStart.Length)] + ScopeCss(inner, scopeClass) + css[end..];
            }

            var scoped = new StringBuilder(css.Length + 64);
            ScopeRuleList(css, 0, css.Length, scopeClass, scoped);
            return scoped.ToString();
        }

        // Copies a list of rules (the stylesheet, or the inside of @media...), prefixing the selectors of style rules
        private static void ScopeRuleList(string css, int start, int end, string scopeClass, StringBuilder scoped)
        {
            var i = start;
            while (i < end)
            {
                if (char.IsWhiteSpace(css[i]))
                {
                    scoped.Append(css[i++]);
                    continue;
                }

                if (IsCommentStart(css, i, end))
                {
                    var afterComment = SkipComment(css, i, end);
                    scoped.Append(css, i, afterComment - i);
                    i = afterComment;
                    continue;
                }

                // The prelude: a selector list, or an at-rule's name and condition
                var preludeEnd = FindPreludeEnd(css, i, end);
                if (preludeEnd >= end)
                {
                    scoped.Append(css, i, end - i);
                    return;
                }

                var prelude = css[i..preludeEnd];
                if (css[preludeEnd] == ';')
                {
                    // A statement such as @import url(x.css);
                    scoped.Append(prelude).Append(';');
                    i = preludeEnd + 1;
                    continue;
                }

                var blockEnd = FindBlockEnd(css, preludeEnd, end);
                if (prelude.StartsWith('@'))
                {
                    if (RuleListAtRules.Contains(AtRuleName(prelude)))
                    {
                        scoped.Append(prelude).Append('{');
                        ScopeRuleList(css, preludeEnd + 1, blockEnd, scopeClass, scoped);
                        if (blockEnd < end)
                        {
                            scoped.Append('}');
                        }
                    }
                    else
                    {
                        scoped.Append(css, i, Math.Min(blockEnd + 1, end) - i);
                    }
                }
                else
                {
                    scoped.Append(PrefixSelectorList(prelude, scopeClass));
                    scoped.Append(css, preludeEnd, Math.Min(blockEnd + 1, end) - preludeEnd);
                }

                i = blockEnd + 1;
            }
        }

        private static string PrefixSelectorList(string prelude, string scopeClass)
        {
            var selectors = prelude.TrimEnd();
            var trailingWhitespace = prelude[selectors.Length..];
            return string.Join(", ", SplitSelectors(selectors).Select(selector => PrefixSelector(selector.Trim(), scopeClass))) + trailingWhitespace;
        }

        private static string PrefixSelector(string selector, string scopeClass)
        {
            if (selector.StartsWith(":root", StringComparison.OrdinalIgnoreCase))
            {
                return "." + scopeClass + selector[":root".Length..];
            }

            if (SvgTypeSelector.IsMatch(selector))
            {
                return selector[..3] + "." + scopeClass + selector[3..];
            }

            return "." + scopeClass + " " + selector;
        }

        // Splits on commas that aren't inside brackets or strings, so :is(.a, .b) stays one selector
        private static IEnumerable<string> SplitSelectors(string selectors)
        {
            var depth = 0;
            var last = 0;
            for (var i = 0; i < selectors.Length; i++)
            {
                switch (selectors[i])
                {
                    case '(' or '[':
                        depth++;
                        break;
                    case ')' or ']':
                        depth--;
                        break;
                    case '"' or '\'':
                        i = SkipString(selectors, i, selectors.Length) - 1;
                        break;
                    case ',' when depth == 0:
                        yield return selectors[last..i];
                        last = i + 1;
                        break;
                }
            }

            yield return selectors[last..];
        }

        private static string AtRuleName(string prelude) =>
            new(prelude.Skip(1).TakeWhile(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        // The index of the '{' or ';' that ends the prelude, ignoring those in strings, comments and brackets
        private static int FindPreludeEnd(string css, int start, int end)
        {
            var depth = 0;
            for (var i = start; i < end; i++)
            {
                if (IsCommentStart(css, i, end))
                {
                    i = SkipComment(css, i, end) - 1;
                    continue;
                }

                switch (css[i])
                {
                    case '"' or '\'':
                        i = SkipString(css, i, end) - 1;
                        break;
                    case '(' or '[':
                        depth++;
                        break;
                    case ')' or ']':
                        depth--;
                        break;
                    case '{' or ';' when depth <= 0:
                        return i;
                }
            }

            return end;
        }

        // The index of the '}' that closes the block opened at 'open', ignoring braces in strings and comments
        private static int FindBlockEnd(string css, int open, int end)
        {
            var depth = 0;
            for (var i = open; i < end; i++)
            {
                if (IsCommentStart(css, i, end))
                {
                    i = SkipComment(css, i, end) - 1;
                    continue;
                }

                switch (css[i])
                {
                    case '"' or '\'':
                        i = SkipString(css, i, end) - 1;
                        break;
                    case '{':
                        depth++;
                        break;
                    case '}' when --depth == 0:
                        return i;
                }
            }

            return end;
        }

        private static bool IsCommentStart(string css, int i, int end) => css[i] == '/' && i + 1 < end && css[i + 1] == '*';

        private static int SkipComment(string css, int start, int end)
        {
            var close = css.IndexOf("*/", start + 2, end - start - 2, StringComparison.Ordinal);
            return close < 0 ? end : close + 2;
        }

        // The index just after the string starting at 'start', allowing for escaped quotes
        private static int SkipString(string css, int start, int end)
        {
            var quote = css[start];
            for (var i = start + 1; i < end; i++)
            {
                if (css[i] == '\\')
                {
                    i++;
                }
                else if (css[i] == quote)
                {
                    return i + 1;
                }
            }

            return end;
        }
    }
}
