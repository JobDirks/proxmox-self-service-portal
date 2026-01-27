using System;
using System.Text.RegularExpressions;
using VmPortal.Application.Security;

namespace VmPortal.Infrastructure.Security
{
    internal partial class InputSanitizer : IInputSanitizer
    {
        // Allow letters, numbers, spaces, '-', '_', '.' for VM names (length 1–100)
        [GeneratedRegex(@"^[\p{L}\p{N}\s\-_\.]{1,100}$")]
        private static partial Regex VmNameRegex();

        // Allow typical hostname characters: letters, numbers, '-', '.', '_' (length 1–100)
        [GeneratedRegex(@"^[a-zA-Z0-9\-_\.]{1,100}$")]
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
            return vmId > 0 && vmId <= 999999;
        }

        public string SanitizeForDisplay(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

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

            string cleaned = Regex.Replace(input, @"[\x00-\x1F\x7F]", "");
            return cleaned.Length > 200 ? cleaned[..200] + "..." : cleaned;
        }
    }
}
