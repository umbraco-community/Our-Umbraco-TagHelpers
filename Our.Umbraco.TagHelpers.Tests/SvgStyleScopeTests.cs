#nullable enable
using NUnit.Framework;
using NUnit.Framework.Legacy;
using Our.Umbraco.TagHelpers.Utils;

namespace Our.Umbraco.TagHelpers.Tests
{
    // https://github.com/umbraco-community/Our-Umbraco-TagHelpers/issues/114
    // Prefixing the selectors in an SVG's <style> with its scope class, so inlined SVGs don't restyle each other
    public class SvgStyleScopeTests
    {
        private static string Scope(string css) => SvgStyleScope.ScopeCss(css, "svg-abc");

        [TestCase(".st0{fill:#3544b1}", ".svg-abc .st0{fill:#3544b1}")]
        [TestCase(".st0 { fill: red; } .st1 { fill: blue; }", ".svg-abc .st0 { fill: red; } .svg-abc .st1 { fill: blue; }")]
        [TestCase(".a, .b > path {fill:red}", ".svg-abc .a, .svg-abc .b > path {fill:red}")]
        [TestCase("path:is(.a, .b) {fill:red}", ".svg-abc path:is(.a, .b) {fill:red}")]
        [TestCase("* {fill:red}", ".svg-abc * {fill:red}")]
        public void Selectors_ArePrefixed(string css, string expected)
        {
            ClassicAssert.AreEqual(expected, Scope(css));
        }

        [TestCase("svg {fill:red}", "svg.svg-abc {fill:red}")]
        [TestCase("svg .a {fill:red}", "svg.svg-abc .a {fill:red}")]
        [TestCase("svg:hover .a {fill:red}", "svg.svg-abc:hover .a {fill:red}")]
        [TestCase(":root {--c:red}", ".svg-abc {--c:red}")]
        public void SelectorsForTheSvgItself_TargetTheScopedSvg(string css, string expected)
        {
            ClassicAssert.AreEqual(expected, Scope(css));
        }

        [Test]
        public void RulesInsideMediaAndSupports_AreScoped()
        {
            ClassicAssert.AreEqual(
                "@media (min-width: 10px) { .svg-abc .a{fill:red} .svg-abc .b{fill:blue} } @supports (display:grid) { .svg-abc .c{fill:green} }",
                Scope("@media (min-width: 10px) { .a{fill:red} .b{fill:blue} } @supports (display:grid) { .c{fill:green} }"));
        }

        // The Umbraco Community site's single regex turns "to" into ".svg-… to" here
        [TestCase("@keyframes spin { from { transform: rotate(0) } to { transform: rotate(360deg) } }")]
        [TestCase("@font-face { font-family: x; src: url(x.woff2); }")]
        [TestCase("@import url(x.css);")]
        public void OtherAtRules_AreLeftAlone(string css)
        {
            ClassicAssert.AreEqual(css, Scope(css));
        }

        [Test]
        public void KeyframesBetweenRules_DoNotStopTheRulesAfterThemBeingScoped()
        {
            ClassicAssert.AreEqual(
                ".svg-abc .a{animation:spin 1s} @keyframes spin { from {opacity:0} to {opacity:1} } .svg-abc .b{fill:red}",
                Scope(".a{animation:spin 1s} @keyframes spin { from {opacity:0} to {opacity:1} } .b{fill:red}"));
        }

        [Test]
        public void CommentsAndStrings_AreKept()
        {
            ClassicAssert.AreEqual(
                "/* { brand } */ .svg-abc .a::after{content:\"}{\"} /* end */",
                Scope("/* { brand } */ .a::after{content:\"}{\"} /* end */"));
        }

        [Test]
        public void CData_IsKept()
        {
            ClassicAssert.AreEqual("<![CDATA[ .svg-abc .st0{fill:red} ]]>", Scope("<![CDATA[ .st0{fill:red} ]]>"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void EmptyCss_IsUnchanged(string css)
        {
            ClassicAssert.AreEqual(css, Scope(css));
        }

        [Test]
        public void ScopeClass_IsStable_AndDiffersBetweenFiles()
        {
            var logo = SvgStyleScope.ScopeClass("/assets/logo.svg");

            StringAssert.IsMatch("^svg-[0-9a-f]{12}$", logo);
            ClassicAssert.AreEqual(logo, SvgStyleScope.ScopeClass("/assets/logo.svg"));
            ClassicAssert.AreNotEqual(logo, SvgStyleScope.ScopeClass("/assets/footer-logo.svg"));
        }
    }
}
