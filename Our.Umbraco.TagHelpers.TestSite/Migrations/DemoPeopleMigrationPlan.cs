using Umbraco.Cms.Core.Packaging;

namespace Our.Umbraco.TagHelpers.TestSite.Migrations;

/// <summary>
/// Creates the demo users and member that the user/member/edit-link samples need
/// (see <see cref="AddDemoPeople"/>). Package migration plans are discovered automatically
/// and run on startup, including straight after an unattended install.
/// This lives in the demo site only; it is not part of the Our.Umbraco.TagHelpers package.
/// </summary>
public class DemoPeopleMigrationPlan : PackageMigrationPlan
{
    public DemoPeopleMigrationPlan()
        : base("Our.Umbraco.TagHelpers.TestSite demo people")
    {
    }

    protected override void DefinePlan()
    {
        To<AddDemoPeople>(new Guid("6f0f8a3e-5b8c-4c39-9d0e-2a7f4b1c9e51"));
    }
}
