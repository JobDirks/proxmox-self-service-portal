using System;
using System.Text.RegularExpressions;
using VmPortal.Application.Security;

namespace VmPortal.Infrastructure.Security
{
    internal partial class InputSanitizer : IInputSanitizer
    {
        [GeneratedRegex(@"^[a-zA-Z0-9\-_\.]{1,50}$")]
        private static partial Regex VmNameRegex();

        [GeneratedRegex(@"^[a-zA-Z0-9\-_]{1,30}$")]
        private static partial Regex NodeNameRegex();

        [GeneratedRegex(@"[<>""'&]")]
        private static partial Regex HtmlCharsRegex();

        public string SanitizeVmName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string trimmed = input.Trim();
            return VmNameRegex().IsMatch(trimmed) ? trimmed : string.Empty;
        }

        public string SanitizeNodeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string trimmed = input.Trim();
            return NodeNameRegex().IsMatch(trimmed) ? trimmed : string.Empty;
        }

        public bool IsValidVmId(int vmId)
        {
            return vmId > 0 && vmId <= 999999; // Proxmox VMID range
        }

        public string SanitizeForDisplay(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // HTML encode dangerous characters
            return HtmlCharsRegex().Replace(input, match => match.Value switch
            {
                "<" => "&lt;",
                ">" => "&gt;",
                "\"" => "&quot;",
                "'" => "&#x27;",
                "&" => "&amp;",
                _ => match.Value
            });
        }

        public string SanitizeForLog(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Remove control characters and limit length for logging
            string cleaned = Regex.Replace(input, @"[\x00-\x1F\x7F]", "");
            return cleaned.Length > 200 ? cleaned[..200] + "..." : cleaned;
        }
    }
}
