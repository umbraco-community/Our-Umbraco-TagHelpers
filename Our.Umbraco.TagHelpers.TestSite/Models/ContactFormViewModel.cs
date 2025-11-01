using System.ComponentModel.DataAnnotations;

namespace Our.Umbraco.TagHelpers.TestSite.Models;

public class ContactFormViewModel 
{
    [Required]
    public string Name { get; set; }
    
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    
    [Required]
    [MinLength(5)]
    public string Message { get; set; }
}