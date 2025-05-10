namespace GreenMeadowsPortal.Extensions
{
    // Make sure the class is static
    public static class UserExtensions
    {
        // Make sure to use 'this' keyword for the extension method
        public static bool ShowEmail(this Models.ApplicationUser user)
        {
            // Implementation
            return true;
        }

        // Make sure to use 'this' keyword for the extension method
        public static bool ShowPhoneNumber(this Models.ApplicationUser user)
        {
            // Implementation
            return !string.IsNullOrEmpty(user.PhoneNumber);
        }
    }
}