namespace VmPortal.Domain.Vms
{
    public static class ProxmoxTagHelper
    {
        // Example prefix; adjust to your real one or remove if you don't use it
        public const String TemplateTagPrefix = "tmpl-";

        // Example system tags automatically applied by the system
        private static readonly String[] SystemTags =
        {
            "vmportal-managed",
            "vmportal-template"
        };

        // Allowed characters: letters, numbers, _ + . - :
        private const String TagPattern = "^[A-Za-z0-9_+\\.\\-:]+$";

        public static Boolean IsValidTemplateTag(String? tag)
        {
            if (String.IsNullOrWhiteSpace(tag))
            {
                return true; // optional
            }

            String trimmed = tag.Trim();

            if (trimmed.Length == 0)
            {
                return true;
            }

            System.Text.RegularExpressions.Regex regex =
                new System.Text.RegularExpressions.Regex(TagPattern);

            return regex.IsMatch(trimmed);
        }

        public static String NormalizeTemplateTag(String tag)
        {
            String trimmed = tag.Trim();

            // Proxmox tags are often treated case-insensitively; choose your convention
            String lower = trimmed.ToLowerInvariant();

            return lower;
        }

        public static Boolean IsSystemTag(String tag)
        {
            String normalized = NormalizeTemplateTag(tag);

            foreach (String systemTag in SystemTags)
            {
                if (normalized == systemTag)
                {
                    return true;
                }
            }

            return false;
        }

        public static String ApplyTemplatePrefixIfMissing(String tag)
        {
            if (String.IsNullOrWhiteSpace(tag))
            {
                return tag;
            }

            String normalized = NormalizeTemplateTag(tag);

            if (normalized.StartsWith(TemplateTagPrefix, StringComparison.Ordinal))
            {
                return normalized;
            }

            return TemplateTagPrefix + normalized;
        }
    }
}
