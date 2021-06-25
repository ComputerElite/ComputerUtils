using System;
using System.Text.RegularExpressions;

namespace ComputerUtils.RegexTemplates
{
    public class RegexTemplates
    {
        public static bool IsIP(String input)
        {
            return Regex.IsMatch(input, "((2(5[0-5]|[0-4][0-9])|1?[0-9]?[0-9])\\.){3}(2(5[0-5]|[0-4][0-9])|1?[0-9]?[0-9])");
        }

        public static String GetIP(String input)
        {
            Match found = Regex.Match(input, "((2(5[0-5]|[0-4][0-9])|1?[0-9]?[0-9])\\.){3}(2(5[0-5]|[0-4][0-9])|1?[0-9]?[0-9])");
            if (!found.Success) return "";
            return found.Value;
        }

        public static bool IsDiscordInvite(String input)
        {
            return Regex.IsMatch(input, "(https?://)?(www.)?(discord.(gg|io|me|li)|discordapp.com/invite)/.+[a-zA-Z0-9]");
        }

        public static String GetDiscordInvite(String input)
        {
            Match found = Regex.Match(input, "(https ?://)?(www.)?(discord.(gg|io|me|li)|discordapp.com/invite)/.+[a-zA-Z0-9]");
            if (!found.Success) return "";
            return found.Value;
        }
    }
}