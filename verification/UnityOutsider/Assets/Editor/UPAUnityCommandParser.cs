using System.Text.RegularExpressions;

namespace UPA.Commands
{
    public static class UnityCommandParser
    {
        public static bool TryParse(string intent, out string objectName)
        {
            var match = Regex.Match((intent ?? "").Trim(),
                @"\A(?:Create|Make|Add) a GameObject named (?<name>[A-Za-z][A-Za-z0-9_]{0,63}) with a Rigidbody in the scene\.?\z",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            objectName = match.Success ? match.Groups["name"].Value : "";
            return match.Success;
        }
    }
}
