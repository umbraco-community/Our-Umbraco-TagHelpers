using Our.Umbraco.TagHelpers.Models;
using System.Threading.Tasks;

namespace Our.Umbraco.TagHelpers.Services
{
    public interface ISelfHostService
    {
        Task<SelfHostedFile> SelfHostFile(string url, string? subfolder = null, string? fileExtension = null);
    }
}