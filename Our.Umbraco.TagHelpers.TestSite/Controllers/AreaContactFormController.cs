using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Website.Controllers;

namespace Our.Umbraco.TagHelpers.TestSite.Controllers;

/// <summary>
/// A surface controller in the "TagHelpersDemo" area ([PluginController] puts it there, as packages do),
/// so a form can only reach it when the area is given: &lt;form our-controller="AreaContactForm" umb-area="TagHelpersDemo" ...&gt;
/// </summary>
[PluginController(AreaName)]
public class AreaContactFormController : SurfaceController
{
    public const string AreaName = "TagHelpersDemo";

    public AreaContactFormController(
        IUmbracoContextAccessor umbracoContextAccessor,
        IUmbracoDatabaseFactory databaseFactory,
        ServiceContext services,
        AppCaches appCaches,
        IProfilingLogger profilingLogger,
        IPublishedUrlProvider publishedUrlProvider)
        : base(umbracoContextAccessor, databaseFactory, services, appCaches, profilingLogger, publishedUrlProvider)
    {
    }

    // SurfaceController already has [AutoValidateAntiforgeryToken]; this says it here too, for anyone copying the
    // demo and for code scanning, which doesn't see the inherited attribute. The form tag helper adds the token.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Submit(string name)
    {
        TempData["AreaFormSubmitted"] = name;
        return RedirectToCurrentUmbracoPage();
    }
}
