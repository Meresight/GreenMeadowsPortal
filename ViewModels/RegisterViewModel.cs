namespace GreenMeadowsPortal.ViewModels;
using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
      

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty; // Fix: Initialize with default value

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty; // Fix: Initialize with default value

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty; // Fix: Initialize with default value

    [Required]
    public string FirstName { get; set; } = string.Empty; // Fix: Initialize with default value

    [Required]
    public string LastName { get; set; } = string.Empty; // Fix: Initialize with default value

    public string Address { get; set; } = string.Empty; // Fix: Initialize with default value

    [Required]
    public string Role { get; set; } = string.Empty; // Fix: Initialize with default value
}
