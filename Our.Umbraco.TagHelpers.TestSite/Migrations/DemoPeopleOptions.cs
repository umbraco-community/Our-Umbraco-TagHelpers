namespace Our.Umbraco.TagHelpers.TestSite.Migrations;

/// <summary>
/// Logins for the demo people, read from <c>TestSite:DemoPeople</c> (appsettings.Development.json).
/// </summary>
public class DemoPeopleOptions
{
    public const string SectionName = "TestSite:DemoPeople";

    public string? EditorEmail { get; set; }

    public string? NoContentUserEmail { get; set; }

    public string? StaffMemberEmail { get; set; }

    public string? Password { get; set; }

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(EditorEmail)
        && !string.IsNullOrWhiteSpace(NoContentUserEmail)
        && !string.IsNullOrWhiteSpace(StaffMemberEmail)
        && !string.IsNullOrWhiteSpace(Password);
}
