using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;

namespace Our.Umbraco.TagHelpers.TestSite.Migrations;

/// <summary>
/// Creates the demo people used by the user/member samples and the cross-version tests:
/// <list type="bullet">
/// <item>a user in the built-in Editor group,</item>
/// <item>a "Demo No Content" user group (Media section only) and a user in it,</item>
/// <item>a "Staff" member group and a member in it.</item>
/// </list>
/// Logins come from <see cref="DemoPeopleOptions"/>; without them nothing is created.
/// Anything that already exists is skipped, and failures are logged rather than thrown,
/// because a throwing package migration would stop the demo site from booting.
/// </summary>
public class AddDemoPeople : AsyncMigrationBase
{
    /// <summary>The built-in Editors user group (Umbraco has no constant for it).</summary>
    public const string EditorGroupAlias = "editor";
    public const string NoContentGroupAlias = "demoNoContent";
    public const string StaffMemberGroupName = "Staff";

    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AddDemoPeople> _logger;

    // Set per run from a DI scope: some of these services (IMemberEditingService needs the scoped
    // IMemberManager) can't be resolved from the root provider that constructs migrations.
    private IUserService _userService = null!;
    private IUserGroupService _userGroupService = null!;
    private IMemberService _memberService = null!;
    private IMemberGroupService _memberGroupService = null!;
    private IMemberTypeService _memberTypeService = null!;
    private IMemberEditingService _memberEditingService = null!;
    private IShortStringHelper _shortStringHelper = null!;

    public AddDemoPeople(
        IMigrationContext context,
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<AddDemoPeople> logger)
        : base(context)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task MigrateAsync()
    {
        var options = _configuration.GetSection(DemoPeopleOptions.SectionName).Get<DemoPeopleOptions>();
        if (options?.IsComplete != true)
        {
            _logger.LogWarning("Demo people: no complete {Section} configuration, so no demo users or members were created", DemoPeopleOptions.SectionName);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var services = scope.ServiceProvider;
            _userService = services.GetRequiredService<IUserService>();
            _userGroupService = services.GetRequiredService<IUserGroupService>();
            _memberService = services.GetRequiredService<IMemberService>();
            _memberGroupService = services.GetRequiredService<IMemberGroupService>();
            _memberTypeService = services.GetRequiredService<IMemberTypeService>();
            _memberEditingService = services.GetRequiredService<IMemberEditingService>();
            _shortStringHelper = services.GetRequiredService<IShortStringHelper>();

            var editorGroup = await _userGroupService.GetAsync(EditorGroupAlias);
            var noContentGroup = await EnsureNoContentUserGroupAsync();

            await EnsureUserAsync("Demo Editor", options.EditorEmail!, editorGroup, options.Password!);
            await EnsureUserAsync("Demo Media Only", options.NoContentUserEmail!, noContentGroup, options.Password!);

            var staff = await EnsureMemberGroupAsync(StaffMemberGroupName);
            await EnsureMemberAsync("Demo Staff", options.StaffMemberEmail!, staff, options.Password!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Demo people: creating the demo users and members failed");
        }
    }

    private async Task<IUserGroup?> EnsureNoContentUserGroupAsync()
    {
        var existing = await _userGroupService.GetAsync(NoContentGroupAlias);
        if (existing is not null)
        {
            _logger.LogInformation("Demo people: user group {Alias} already exists", NoContentGroupAlias);
            return existing;
        }

        var group = new UserGroup(_shortStringHelper)
        {
            Alias = NoContentGroupAlias,
            Name = "Demo No Content",
            Icon = "icon-picture",
            HasAccessToAllLanguages = true,
        };
        group.AddAllowedSection(Constants.Applications.Media);

        var result = await _userGroupService.CreateAsync(group, Constants.Security.SuperUserKey);
        if (!result.Success)
        {
            _logger.LogWarning("Demo people: could not create user group {Alias}: {Status}", NoContentGroupAlias, result.Status);
            return null;
        }

        _logger.LogInformation("Demo people: created user group {Alias}", NoContentGroupAlias);
        return result.Result;
    }

    private async Task EnsureUserAsync(string name, string email, IUserGroup? group, string password)
    {
        if (_userService.GetByEmail(email) is not null)
        {
            _logger.LogInformation("Demo people: user {Name} already exists", name);
            return;
        }

        if (group is null)
        {
            _logger.LogWarning("Demo people: skipped user {Name} because its user group is missing", name);
            return;
        }

        var created = await _userService.CreateAsync(
            Constants.Security.SuperUserKey,
            new UserCreateModel
            {
                Email = email,
                UserName = email,
                Name = name,
                UserGroupKeys = new HashSet<Guid> { group.Key },
            },
            approveUser: true);

        if (!created.Success || created.Result.CreatedUser is null)
        {
            _logger.LogWarning("Demo people: could not create user {Name}: {Status}", name, created.Status);
            return;
        }

        var passwordResult = await _userService.ChangePasswordAsync(
            Constants.Security.SuperUserKey,
            new ChangeUserPasswordModel { UserKey = created.Result.CreatedUser.Key, NewPassword = password });

        if (!passwordResult.Success)
        {
            _logger.LogWarning("Demo people: created user {Name} but could not set its password: {Status}", name, passwordResult.Status);
            return;
        }

        _logger.LogInformation("Demo people: created user {Name} in {Group}", name, group.Alias);
    }

    private async Task<IMemberGroup?> EnsureMemberGroupAsync(string name)
    {
        var existing = await _memberGroupService.GetByNameAsync(name);
        if (existing is not null)
        {
            _logger.LogInformation("Demo people: member group {Name} already exists", name);
            return existing;
        }

        var result = await _memberGroupService.CreateAsync(new MemberGroup { Name = name });
        if (!result.Success)
        {
            _logger.LogWarning("Demo people: could not create member group {Name}: {Status}", name, result.Status);
            return null;
        }

        _logger.LogInformation("Demo people: created member group {Name}", name);
        return result.Result;
    }

    private async Task EnsureMemberAsync(string name, string email, IMemberGroup? group, string password)
    {
        if (_memberService.GetByEmail(email) is not null)
        {
            _logger.LogInformation("Demo people: member {Name} already exists", name);
            return;
        }

        var memberType = _memberTypeService.Get(Constants.Conventions.MemberTypes.DefaultAlias);
        var superUser = await _userService.GetAsync(Constants.Security.SuperUserKey);
        if (group is null || memberType is null || superUser is null)
        {
            _logger.LogWarning("Demo people: skipped member {Name} because its member group, the default member type or the admin user is missing", name);
            return;
        }

        var result = await _memberEditingService.CreateAsync(
            new MemberCreateModel
            {
                Email = email,
                Username = email,
                Password = password,
                ContentTypeKey = memberType.Key,
                IsApproved = true,
                Roles = new[] { group.Key },
                Variants = new[] { new VariantModel { Name = name } },
            },
            superUser);

        if (!result.Success)
        {
            _logger.LogWarning("Demo people: could not create member {Name}: {Status}", name, result.Status);
            return;
        }

        _logger.LogInformation("Demo people: created member {Name} in {Group}", name, group.Name);
    }
}
