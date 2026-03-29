using System.Text.RegularExpressions;

namespace IOCv2.Application.Features.Projects.Common
{
    public static class ProjectCodeGenerator
    {
        // "Tech Corp Ltd" → "TECHCO" (tối đa 6 ký tự, chỉ alphanumeric, uppercase)
        public static string EnterpriseSlug(string enterpriseName)
            => Slugify(enterpriseName, 6);

        // "Group Alpha" → "GROU" (tối đa 4 ký tự, chỉ alphanumeric, uppercase)
        public static string GroupSlug(string groupName)
            => Slugify(groupName, 4);

        public static string Generate(string enterpriseName, string groupName, int n)
        {
            var ent = EnterpriseSlug(enterpriseName);
            var grp = GroupSlug(groupName);
            return $"PRJ-{ent}_{grp}_{n}";
        }

        private static string Slugify(string input, int maxLength)
        {
            var clean = Regex.Replace(input ?? string.Empty, "[^a-zA-Z0-9]", "");
            var trimmed = clean.Length > maxLength ? clean[..maxLength] : clean;
            return trimmed.ToUpperInvariant();
        }
    }
}
