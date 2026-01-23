# CLAUDE.md - AI Assistant Guide for Our.Umbraco.TagHelpers

This document provides comprehensive guidance for AI assistants working with the Our.Umbraco.TagHelpers codebase. It covers the project structure, development workflows, coding conventions, and best practices.

## Table of Contents

- [Project Overview](#project-overview)
- [Repository Structure](#repository-structure)
- [Development Environment](#development-environment)
- [Architecture & Design Patterns](#architecture--design-patterns)
- [Coding Conventions](#coding-conventions)
- [TagHelper Implementation Patterns](#taghelper-implementation-patterns)
- [Testing Strategy](#testing-strategy)
- [Build & Deployment](#build--deployment)
- [Configuration Management](#configuration-management)
- [AI Assistant Guidelines](#ai-assistant-guidelines)

---

## Project Overview

**Our.Umbraco.TagHelpers** is a community-driven library of ASP.NET Core TagHelpers designed specifically for Umbraco CMS. The project provides 18+ TagHelpers that simplify common tasks in Umbraco development, such as conditional rendering, internationalization, responsive images, caching, and more.

### Key Information

- **Maintainer**: Warren Buckley
- **Organization**: Umbraco Community
- **License**: Free
- **Target Frameworks**: .NET 5.0, .NET 6.0
- **Primary Dependencies**: Umbraco.Cms.Core (9.2.0+), ASP.NET Core Razor/MVC
- **Package Distribution**: NuGet (Our.Umbraco.TagHelpers)

### Project Goals

1. Provide Razor-friendly alternatives to traditional Umbraco HTML helpers
2. Improve code readability and reduce Razor code block nesting
3. Maintain backward compatibility with Umbraco 9.x+
4. Support performance optimizations (lazy loading, caching, responsive images)
5. Follow ASP.NET Core TagHelper conventions

---

## Repository Structure

```
Our-Umbraco-TagHelpers/
├── .github/
│   └── workflows/
│       ├── dotnet.yml                    # CI/CD pipeline
│       ├── codeql-analysis.yml           # Security scanning
│       └── release-nuget.yml             # NuGet release automation
│
├── Our.Umbraco.TagHelpers/               # Main library project
│   ├── [Root]                            # 18 TagHelper implementations
│   │   ├── IncludeIfTagHelper.cs         # our-if attribute
│   │   ├── DictionaryTagHelper.cs        # <our-dictionary>
│   │   ├── ImgTagHelper.cs               # <our-img> (most complex, 23KB)
│   │   ├── InlineSvgTagHelper.cs         # <our-svg>
│   │   ├── MacroTagHelper.cs             # <our-macro>
│   │   ├── LanguageSwitcherTagHelper.cs  # <our-lang-switcher>
│   │   ├── LinkTagHelper.cs              # <our-link>
│   │   ├── UmbracoCacheTagHelper.cs      # <our-cache>
│   │   ├── VersionTagHelper.cs           # <our-version>
│   │   ├── EditLinkTagHelper.cs          # <our-edit-link>
│   │   ├── OurFallbackTagHelper.cs       # <our-fallback>
│   │   ├── ActiveClassTagHelper.cs       # our-active-class
│   │   ├── MemberTagHelper.cs            # our-member-include/exclude
│   │   ├── UserTagHelper.cs              # our-user-include/exclude
│   │   ├── SelfHostTagHelper.cs          # our-self-host
│   │   └── SurfaceControllerFormTagHelper.cs  # form our-controller/our-action
│   │
│   ├── Classes/                          # Data models
│   │   └── OurImageSize.cs
│   │
│   ├── Composing/                        # Dependency injection setup
│   │   ├── PackageManifestComposer.cs
│   │   ├── CacheTagHelperComposer.cs
│   │   ├── InlineSvgTagHelperComposer.cs
│   │   ├── SelfHostServiceComposer.cs
│   │   └── BackofficeUserComposer.cs
│   │
│   ├── Configuration/                    # AppSettings binding
│   │   ├── OurUmbracoTagHelpersConfiguration.cs
│   │   ├── InlineSvgTagHelperConfiguration.cs
│   │   ├── ImgTagHelperConfiguration.cs
│   │   ├── MediaQuerySizes.cs
│   │   └── SelfHostTagHelperConfiguration.cs
│   │
│   ├── Enums/
│   │   ├── OurScreenSize.cs              # Small, Medium, Large, XL, XXL
│   │   └── ImagePlaceholderType.cs
│   │
│   ├── Extensions/
│   │   ├── ClaimsIdentityExtensions.cs
│   │   └── WebHostEnvironmentExtensions.cs
│   │
│   ├── Models/
│   │   └── SelfHostedFile.cs
│   │
│   ├── Notifications/                     # Umbraco event handlers
│   │   ├── CacheRefresherNotifications.cs
│   │   └── InlineSvgTagHelperNotifications.cs
│   │
│   ├── Services/                          # Business logic
│   │   ├── IBackofficeUserAccessor.cs / BackofficeUserAccessor.cs
│   │   ├── ISelfHostService.cs / SelfHostService.cs
│   │   └── IUmbracoTagHelperCacheKeys.cs / UmbracoTagHelperCacheKeys.cs
│   │
│   ├── Utils/
│   │   └── StringUtils.cs                 # Parsing helpers
│   │
│   ├── Our.Umbraco.TagHelpers.csproj
│   └── TagHelperLogo.png
│
├── Our.Umbraco.TagHelpers.Tests/         # Unit tests
│   ├── Helpers/
│   │   └── TestContextHelpers.cs
│   ├── IncludeIfTagHelperTests.cs
│   ├── InlineSvgTagHelperTests.cs
│   ├── LinkTagHelperTests.cs
│   ├── SelfHostServiceTests.cs
│   └── SelfHostTagHelperTests.cs
│
├── Our.Umbraco.TagHelpers.ExampleSite/   # Demo Umbraco site
│   ├── Views/
│   ├── wwwroot/
│   ├── Program.cs
│   ├── Startup.cs
│   └── appsettings.json
│
├── GitVersion.yml                         # Versioning configuration
├── umbraco-marketplace.json               # Marketplace metadata
├── README.md                              # User documentation
└── Our.Umbraco.TagHelpers.sln
```

---

## Development Environment

### Prerequisites

- **.NET SDK**: 5.0 and 6.0 (multi-target support)
- **IDE**: Visual Studio 2019+, JetBrains Rider, or VS Code
- **Git**: For version control
- **GitVersion**: Installed automatically during build
- **Optional**: Umbraco CMS 9.x+ for testing in ExampleSite

### Initial Setup

```bash
# Clone the repository
git clone https://github.com/umbraco-community/Our-Umbraco-TagHelpers.git
cd Our-Umbraco-TagHelpers

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
cd Our.Umbraco.TagHelpers.Tests
dotnet test
```

### Project Files

- **Solution**: `Our.Umbraco.TagHelpers.sln` (contains all 3 projects)
- **Main Library**: `Our.Umbraco.TagHelpers/Our.Umbraco.TagHelpers.csproj`
- **Tests**: `Our.Umbraco.TagHelpers.Tests/Our.Umbraco.TagHelpers.Tests.csproj`
- **Example Site**: `Our.Umbraco.TagHelpers.ExampleSite/Our.Umbraco.TagHelpers.ExampleSite.csproj`

---

## Architecture & Design Patterns

### 1. TagHelper Pattern

All TagHelpers inherit from `Microsoft.AspNetCore.Razor.TagHelpers.TagHelper`:

```csharp
[HtmlTargetElement("element-name", Attributes = "attribute-name")]
public class MyTagHelper : TagHelper
{
    [HtmlAttributeName("attribute-name")]
    public string? PropertyName { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Transformation logic
    }
}
```

### 2. Dependency Injection via Umbraco Composers

The project uses Umbraco's `IComposer` pattern for service registration:

```csharp
public class MyServiceComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IMyService, MyService>();
        builder.AddNotificationHandler<MyNotification, MyNotificationHandler>();
    }
}
```

**Service Lifetimes:**
- **Singleton**: Cache services, backoffice user accessor, self-host service
- **Scoped**: Umbraco services (injected via constructor)
- **Transient**: TagHelpers (created per view render)

### 3. Notification Handlers

Cache invalidation uses Umbraco's notification system:

```csharp
public class CacheTagRefresherNotifications :
    INotificationHandler<ContentCacheRefresherNotification>,
    INotificationHandler<MediaCacheRefresherNotification>,
    INotificationHandler<DictionaryCacheRefresherNotification>
{
    public void Handle(ContentCacheRefresherNotification notification)
    {
        _cacheKeys.IncrementKey();
    }
}
```

### 4. Configuration via Options Pattern

AppSettings are bound using `IOptions<T>`:

```csharp
public class MyTagHelper : TagHelper
{
    private readonly MyConfiguration _config;

    public MyTagHelper(IOptions<MyConfiguration> config)
    {
        _config = config.Value;
    }
}
```

---

## Coding Conventions

### General Conventions

1. **Namespace**: Always use `Our.Umbraco.TagHelpers` (and sub-namespaces for folders)
2. **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
3. **File Organization**: One class per file, file name matches class name
4. **Naming**:
   - PascalCase for classes, methods, properties
   - camelCase for private fields with `_` prefix
   - Descriptive names (avoid abbreviations)

### TagHelper Naming Conventions

- **Element TagHelpers**: `<our-element>` → `ElementTagHelper.cs`
- **Attribute TagHelpers**: `our-attribute` → `AttributeTagHelper.cs`
- HTML element names use lowercase with hyphens
- C# attribute properties use PascalCase

**Example:**
```csharp
// <our-dictionary key="welcome" fallback-lang="en-US">
[HtmlTargetElement("our-dictionary")]
public class DictionaryTagHelper : TagHelper
{
    [HtmlAttributeName("key")]
    public string? Key { get; set; }

    [HtmlAttributeName("fallback-lang")]
    public string? FallbackLang { get; set; }
}
```

### Code Style

1. **XML Documentation**: Required for public APIs
2. **Braces**: Always use braces for control structures (even single-line)
3. **Async/Await**: Use `ProcessAsync` for async operations
4. **Null Handling**: Use null-conditional operators (`?.`, `??`)
5. **String Interpolation**: Prefer over concatenation

**Example:**
```csharp
/// <summary>
/// Apply the attribute our-if to any HTML DOM element for it to be included in the page
/// as long as the value inside the attribute evaluates to true
/// </summary>
[HtmlTargetElement("*", Attributes = "our-if")]
public class IncludeIfTagHelper : TagHelper
{
    /// <summary>
    /// A boolean expression
    /// If it evaluates to true then it will be kept in the page
    /// </summary>
    [HtmlAttributeName("our-if")]
    public bool? Predicate { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Predicate.HasValue || !Predicate.Value)
        {
            output.SuppressOutput();
        }
    }
}
```

---

## TagHelper Implementation Patterns

### Pattern 1: Attribute-Based Conditional Rendering

Used for: `our-if`, `our-member-include`, `our-user-include`, `our-active-class`

```csharp
[HtmlTargetElement("*", Attributes = "our-if")]
public class IncludeIfTagHelper : TagHelper
{
    [HtmlAttributeName("our-if")]
    public bool? Predicate { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (!Predicate.HasValue || !Predicate.Value)
        {
            output.SuppressOutput();
        }
    }
}
```

### Pattern 2: Element-Based Content Transformation

Used for: `<our-dictionary>`, `<our-version>`, `<our-fallback>`

```csharp
[HtmlTargetElement("our-dictionary")]
public class DictionaryTagHelper : TagHelper
{
    [HtmlAttributeName("key")]
    public string? Key { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = null; // Remove wrapper element

        var translation = GetTranslation(Key);
        if (translation != null)
        {
            output.Content.SetContent(translation);
        }
        else
        {
            var defaultContent = await output.GetChildContentAsync();
            output.Content.SetContent(defaultContent.GetContent());
        }
    }
}
```

### Pattern 3: Wrapper Elements with Child Content

Used for: `<our-link>`, `<our-cache>`

```csharp
[HtmlTargetElement("our-link")]
public class LinkTagHelper : TagHelper
{
    [HtmlAttributeName("link")]
    public Link? Link { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "a";
        output.Attributes.SetAttribute("href", Link?.Url);

        var childContent = await output.GetChildContentAsync();
        if (childContent.IsEmptyOrWhiteSpace)
        {
            output.Content.SetContent(Link?.Name ?? "");
        }
    }
}
```

### Pattern 4: Complex HTML Generation

Used for: `<our-img>`, `<our-svg>`, `<our-lang-switcher>`

```csharp
[HtmlTargetElement("our-img")]
public class ImgTagHelper : TagHelper
{
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Determine if <img> or <picture> element
        if (HasMultipleBreakpoints())
        {
            output.TagName = "picture";

            // Generate <source> elements
            foreach (var breakpoint in breakpoints)
            {
                var source = $"<source srcset=\"{url}\" media=\"{query}\" />";
                output.Content.AppendHtml(source);
            }

            // Generate fallback <img>
            output.Content.AppendHtml($"<img src=\"{fallbackUrl}\" />");
        }
        else
        {
            output.TagName = "img";
            output.Attributes.SetAttribute("src", imageUrl);
        }
    }
}
```

### Pattern 5: Service Injection

Used for: All TagHelpers that require external data/services

```csharp
public class DictionaryTagHelper : TagHelper
{
    private readonly ILocalizationService _localizationService;
    private readonly IUmbracoContextAccessor _umbracoContextAccessor;

    public DictionaryTagHelper(
        ILocalizationService localizationService,
        IUmbracoContextAccessor umbracoContextAccessor)
    {
        _localizationService = localizationService;
        _umbracoContextAccessor = umbracoContextAccessor;
    }
}
```

### Common TagHelper Methods

- `output.SuppressOutput()` - Remove element entirely
- `output.TagName = null` - Remove wrapper, keep content
- `output.TagName = "div"` - Change element type
- `output.Attributes.SetAttribute("name", "value")` - Set attribute
- `output.Content.SetContent("text")` - Replace content
- `output.Content.AppendHtml("<div>...</div>")` - Add HTML content
- `await output.GetChildContentAsync()` - Get existing child content

---

## Testing Strategy

### Test Framework

- **NUnit 3.13.2**: Primary testing framework
- **Moq 4.18.2**: Mocking dependencies
- **TestCase Attributes**: Data-driven tests

### Test Organization

Tests are located in `Our.Umbraco.TagHelpers.Tests/` with one test class per TagHelper:

```
Our.Umbraco.TagHelpers.Tests/
├── IncludeIfTagHelperTests.cs
├── InlineSvgTagHelperTests.cs
├── LinkTagHelperTests.cs
├── SelfHostServiceTests.cs
└── Helpers/
    └── TestContextHelpers.cs
```

### Test Patterns

#### 1. Data-Driven Tests with TestCase

```csharp
[TestCase(true, "original-child-content", "original-child-content")]
[TestCase(false, "original-child-content", "")]
public async Task Given_Predicate_Return_Contents_Or_Empty(
    bool predicate,
    string childContent,
    string expected)
{
    // Arrange
    var context = TestContextHelpers.CreateTagHelperContext();
    var output = TestContextHelpers.CreateTagHelperOutput("div", childContent);
    var tagHelper = new IncludeIfTagHelper { Predicate = predicate };

    // Act
    tagHelper.Process(context, output);

    // Assert
    Assert.AreEqual(expected, output.Content.GetContent());
}
```

#### 2. Testing with Mocks

```csharp
[Test]
public void GetLocalFile_ReturnsCorrectPath()
{
    // Arrange
    var mockEnv = new Mock<IWebHostEnvironment>();
    mockEnv.Setup(e => e.WebRootPath).Returns("/wwwroot");

    var service = new SelfHostService(mockEnv.Object);

    // Act
    var result = service.GetLocalFile("https://example.com/script.js");

    // Assert
    Assert.IsNotNull(result);
    Assert.That(result.LocalPath, Does.Contain("/wwwroot"));
}
```

#### 3. Test Helpers

The `TestContextHelpers` class provides factory methods:

```csharp
public static class TestContextHelpers
{
    public static TagHelperContext CreateTagHelperContext()
    {
        return new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString());
    }

    public static TagHelperOutput CreateTagHelperOutput(
        string tagName,
        string childContent)
    {
        return new TagHelperOutput(
            tagName,
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
            {
                var tagHelperContent = new DefaultTagHelperContent();
                tagHelperContent.SetContent(childContent);
                return Task.FromResult<TagHelperContent>(tagHelperContent);
            });
    }
}
```

### Testing Checklist

When adding a new TagHelper, ensure tests cover:

- [ ] Default behavior with no attributes
- [ ] All attribute combinations
- [ ] Null/empty value handling
- [ ] Edge cases (empty strings, special characters)
- [ ] Output suppression scenarios
- [ ] Child content preservation/modification
- [ ] Service dependency behavior (with mocks)

### Visual Testing with Example Site

The ExampleSite project provides a full Umbraco installation for visual testing of TagHelpers.

#### Running the Example Site

```bash
# Navigate to the example site
cd Our.Umbraco.TagHelpers.ExampleSite

# Restore dependencies
dotnet restore

# Run the site
dotnet run

# Or use watch mode for auto-reload during development
dotnet watch run
```

The site will start at `https://localhost:44331` (or similar). On first run:

1. Complete the Umbraco installer wizard
2. Create an admin user
3. The database is SQLite (stored in `umbraco/Data/Umbraco.sqlite.db`)

#### Adding TagHelper Test Pages

To test a new TagHelper visually:

1. Create a Document Type in Umbraco backoffice
2. Add a template with your TagHelper implementation
3. Create content using the Document Type
4. Preview the page to verify rendering

**Example Template:**
```cshtml
@inherits Umbraco.Cms.Web.Common.Views.UmbracoViewPage
@{
    Layout = null;
}

<!DOCTYPE html>
<html>
<head>
    <title>TagHelper Test</title>
</head>
<body>
    <!-- Test your TagHelper here -->
    <our-dictionary key="welcome">Default Welcome</our-dictionary>

    <our-if="DateTime.Now.Hour >= 9">
        <p>Office hours!</p>
    </our-if>

    <our-img media-item="Model.Image" width="400" />
</body>
</html>
```

#### Automated Browser Testing (Optional)

For automated visual regression testing or E2E tests:

**Using Playwright:**

```bash
# Install Playwright (from solution root)
dotnet add Our.Umbraco.TagHelpers.Tests package Microsoft.Playwright
dotnet build
pwsh bin/Debug/net6.0/playwright.ps1 install

# Create E2E test
```

```csharp
// Example Playwright test
[Test]
public async Task OurDictionary_RendersTranslation()
{
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync();
    var page = await browser.NewPageAsync();

    await page.GotoAsync("https://localhost:44331/test-page");

    var content = await page.TextContentAsync("h1");
    Assert.That(content, Is.EqualTo("Translated Welcome"));
}
```

**Benefits of Visual Testing:**
- Verify HTML output matches expectations
- Test responsive image breakpoints
- Validate lazy loading behavior
- Check JavaScript integration (language switcher, self-host)
- Test in real Umbraco context with actual content

---

## Build & Deployment

### Build Process

The project uses **GitVersion** for automatic semantic versioning based on git history.

```bash
# Local build
dotnet build

# Create NuGet package
dotnet pack --output ./build.out/
```

### CI/CD Pipeline (.github/workflows/dotnet.yml)

**Triggers:**
- Push to `main` branch
- Pull requests targeting `main`

**Build Steps:**
1. Install GitVersion 5.x
2. Checkout code with full history (`fetch-depth: 0`)
3. Setup .NET 5.0 and 6.0 SDKs
4. Restore dependencies (`dotnet restore`)
5. Execute GitVersion to determine version
6. Build solution (`dotnet build --no-restore`)
7. Run tests (`dotnet test`)
8. Pack NuGet package (`dotnet pack --output ../build.out/`)
9. Upload artifact to GitHub Actions

**Output:**
- NuGet package: `Our.Umbraco.TagHelpers.{version}.nupkg`
- Symbol package: `Our.Umbraco.TagHelpers.{version}.snupkg` (SourceLink enabled)

### GitVersion Configuration

File: `GitVersion.yml`

```yaml
assembly-versioning-scheme: MajorMinorPatch
mode: ContinuousDeployment
branches:
  main:
    mode: ContinuousDeployment
    tag: ''
  pull-request:
    mode: ContinuousDeployment
```

**Versioning Strategy:**
- Main branch: Stable versions (e.g., `1.2.3`)
- Pull requests: Pre-release versions (e.g., `1.2.3-PullRequest0001`)
- Tags trigger official releases

### Package Configuration

Defined in `Our.Umbraco.TagHelpers.csproj`:

```xml
<PropertyGroup>
  <Company>Umbraco Community</Company>
  <Authors>Warren Buckley</Authors>
  <PackageProjectUrl>https://github.com/umbraco-community/Our-Umbraco-TagHelpers</PackageProjectUrl>
  <PackageIcon>TagHelperLogo.png</PackageIcon>
  <PackageTags>Umbraco;TagHelper;TagHelpers;UmbracoCMS;umbraco-marketplace</PackageTags>
  <PackageReadmeFile>README.md</PackageReadmeFile>
</PropertyGroup>
```

**SourceLink Configuration:**
```xml
<PropertyGroup>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
```

Enables debugging into the library from consuming projects.

---

## Configuration Management

### AppSettings Schema

Users can configure TagHelper behavior globally via `appsettings.json`:

```json
{
  "Our.Umbraco.TagHelpers": {
    "OurSVG": {
      "EnsureViewBox": false,
      "Cache": false,
      "CacheMinutes": 180
    },
    "OurImg": {
      "MobileFirst": true,
      "MediaQueries": {
        "Small": 576,
        "Medium": 768,
        "Large": 992,
        "ExtraLarge": 1200,
        "ExtraExtraLarge": 1400
      },
      "UseNativeLazyLoading": true,
      "LazyLoadCssClass": "lazyload",
      "LazyLoadPlaceholder": "SVG",
      "LazyLoadPlaceholderLowQualityImageQuality": 5,
      "ApplyAspectRatio": false,
      "AlternativeTextMediaTypePropertyAlias": "alternativeText"
    },
    "OurSelfHost": {
      "RootFolder": "/assets"
    }
  }
}
```

### Configuration Classes

Located in `Our.Umbraco.TagHelpers/Configuration/`:

- `OurUmbracoTagHelpersConfiguration.cs` - Root configuration class
- `InlineSvgTagHelperConfiguration.cs` - SVG TagHelper settings
- `ImgTagHelperConfiguration.cs` - Image TagHelper settings
- `MediaQuerySizes.cs` - Breakpoint definitions
- `SelfHostTagHelperConfiguration.cs` - Self-hosting settings

### Binding Configuration

Configuration is registered in Composer classes:

```csharp
public class OurUmbracoTagHelpersConfigurationComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.Configure<OurUmbracoTagHelpersConfiguration>(
            builder.Config.GetSection("Our.Umbraco.TagHelpers"));
    }
}
```

---

## AI Assistant Guidelines

### When Making Changes

1. **Read Before Writing**
   - Always read existing code before modifying
   - Understand the TagHelper's purpose and current behavior
   - Check for dependencies and injection patterns

2. **Follow Existing Patterns**
   - Use the same coding style as surrounding code
   - Match existing TagHelper patterns (see [TagHelper Implementation Patterns](#taghelper-implementation-patterns))
   - Maintain consistency with attribute naming conventions

3. **Add Tests**
   - Create/update tests for any new TagHelper
   - Follow existing test patterns (see [Testing Strategy](#testing-strategy))
   - Use `TestCase` attributes for data-driven tests
   - Mock dependencies using Moq

4. **Update Documentation**
   - Add XML documentation for public APIs
   - Update README.md with new TagHelper examples
   - Include usage examples in comments

5. **Dependency Injection**
   - Add new services to appropriate Composer class
   - Use constructor injection for dependencies
   - Choose appropriate service lifetime (Singleton/Scoped/Transient)

### Common Tasks

#### Adding a New TagHelper

```csharp
// 1. Create TagHelper class in Our.Umbraco.TagHelpers/
[HtmlTargetElement("our-newfeature")]
public class NewFeatureTagHelper : TagHelper
{
    // 2. Add dependencies via constructor
    private readonly IMyService _myService;

    public NewFeatureTagHelper(IMyService myService)
    {
        _myService = myService;
    }

    // 3. Add properties for HTML attributes
    [HtmlAttributeName("my-attribute")]
    public string? MyAttribute { get; set; }

    // 4. Implement Process or ProcessAsync
    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        // Implementation
    }
}

// 5. Register dependencies in Composing/ folder if needed
public class NewFeatureComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddSingleton<IMyService, MyService>();
    }
}

// 6. Add tests in Our.Umbraco.TagHelpers.Tests/
public class NewFeatureTagHelperTests
{
    [Test]
    public void MyTest()
    {
        // Test implementation
    }
}

// 7. Update README.md with usage example
```

#### Adding Configuration

```csharp
// 1. Create configuration class in Configuration/
public class NewFeatureConfiguration
{
    public bool EnableFeature { get; set; } = true;
    public int CacheMinutes { get; set; } = 60;
}

// 2. Add to root configuration class
public class OurUmbracoTagHelpersConfiguration
{
    public NewFeatureConfiguration NewFeature { get; set; } = new();
}

// 3. Inject into TagHelper
public class NewFeatureTagHelper : TagHelper
{
    private readonly NewFeatureConfiguration _config;

    public NewFeatureTagHelper(IOptions<OurUmbracoTagHelpersConfiguration> options)
    {
        _config = options.Value.NewFeature;
    }
}

// 4. Document in README.md appsettings section
```

#### Adding a Notification Handler

```csharp
// 1. Create handler class in Notifications/
public class MyNotificationHandler : INotificationHandler<ContentPublishedNotification>
{
    public void Handle(ContentPublishedNotification notification)
    {
        // Handle event
    }
}

// 2. Register in Composer
public class MyComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
    {
        builder.AddNotificationHandler<ContentPublishedNotification, MyNotificationHandler>();
    }
}
```

### File Organization Checklist

When adding new code, place it in the appropriate folder:

- **TagHelpers**: Root of `Our.Umbraco.TagHelpers/`
- **Services**: `Services/` folder with interface + implementation
- **Configuration**: `Configuration/` folder
- **Models/DTOs**: `Models/` or `Classes/` folder
- **Enums**: `Enums/` folder
- **Extensions**: `Extensions/` folder
- **Utilities**: `Utils/` folder
- **DI Setup**: `Composing/` folder
- **Event Handlers**: `Notifications/` folder
- **Tests**: `Our.Umbraco.TagHelpers.Tests/`

### Code Quality Checklist

Before submitting changes:

- [ ] Code compiles without warnings
- [ ] All existing tests pass
- [ ] New tests added for new functionality
- [ ] XML documentation added for public APIs
- [ ] Nullable reference types handled correctly
- [ ] No hardcoded magic values (use constants/configuration)
- [ ] Error handling appropriate for use case
- [ ] Performance considerations addressed (caching, lazy loading)
- [ ] README.md updated with new features
- [ ] Configuration examples provided (if applicable)

### Breaking Changes

This is a community library used by many Umbraco sites. Avoid breaking changes:

- Don't change public API signatures
- Don't rename TagHelper attributes
- Don't change default behavior without configuration option
- Don't remove TagHelpers or configuration options
- Use `[Obsolete]` attribute for deprecations

### Performance Considerations

- Use caching for expensive operations (see `<our-cache>`, `<our-svg>`)
- Avoid synchronous I/O operations in `Process()`/`ProcessAsync()`
- Use lazy loading patterns for images/assets
- Consider memory usage with large HTML generation
- Cache compiled templates/expressions where possible

### Security Considerations

- Always HTML-encode user-supplied content
- Validate URLs before rendering `href`/`src` attributes
- Be cautious with `output.Content.SetHtmlContent()` - only use with trusted content
- Use `output.Content.SetContent()` for user content (auto-escapes)
- Check permissions for backoffice/member-only features

### Debugging Tips

1. **Local Testing**: Use the ExampleSite project
2. **Breakpoints**: Set in TagHelper `Process()`/`ProcessAsync()` methods
3. **Output Inspection**: Check `output.Content.GetContent()` in debugger
4. **SourceLink**: Consumers can debug into the library if using symbols

### Common Pitfalls

1. **Forgetting `output.TagName = null`**: Causes extra wrapper element
2. **Not handling null values**: Use `?.` and `??` operators
3. **Synchronous I/O**: Use `ProcessAsync` for file/network operations
4. **Forgetting to register services**: Add to Composer classes
5. **Incorrect attribute naming**: Use lowercase-with-hyphens for HTML
6. **Not testing edge cases**: Null, empty string, special characters

---

## Additional Resources

- **Umbraco Documentation**: https://our.umbraco.com/Documentation/
- **ASP.NET Core TagHelpers**: https://docs.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/
- **GitHub Repository**: https://github.com/umbraco-community/Our-Umbraco-TagHelpers
- **NuGet Package**: https://www.nuget.org/packages/Our.Umbraco.TagHelpers
- **24 Days Article (Self-Host)**: https://24days.in/umbraco-cms/2022/static-assets-taghelper/
- **YouTube Tutorial**: https://www.youtube.com/watch?v=3fkDs0NwIE8

---

## Questions or Issues?

If you have questions about the codebase or need clarification on any conventions:

1. Review the comprehensive README.md for user documentation
2. Examine existing TagHelper implementations as examples
3. Check test files for usage patterns
4. Refer to this CLAUDE.md for development guidelines
5. Review git commit history for context on changes

**Last Updated**: 2026-01-23
**Document Version**: 1.0
**Maintained By**: Umbraco Community
