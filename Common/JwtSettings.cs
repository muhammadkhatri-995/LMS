namespace LMS.API.Common
{
    public class JwtSettings
    {
        public const String SectionName = "Jwt";

        public String Issuer { get; set; } = String.Empty;
        public String Audience { get; set; } = String.Empty;
        public String Key { get; set; } = String.Empty;
        public int ExpiryMinutes { get; set; }=60;
       


    }
}
