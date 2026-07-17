namespace WebChat.Helpers
{
    // Avatar display data is derived from the user, not stored. Same input
    // always gives the same initials/colour, so a user looks identical
    // everywhere without a migration or an extra column.
    public static class AvatarHelper
    {
        private static readonly string[] Palette =
        {
            "#E1306C", "#405DE6", "#1DA1F2", "#17BF63",
            "#F5A623", "#8E44AD", "#E74C3C", "#16A085",
        };

        public static string Initials(string? displayName)
        {
            if (string.IsNullOrWhiteSpace(displayName)) return "??";

            var parts = displayName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length == 0) return "??";
            if (parts.Length == 1)
            {
                var single = parts[0];
                return (single.Length == 1 ? single : single[..2]).ToUpperInvariant();
            }

            return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
        }

        public static string Color(string? id)
        {
            if (string.IsNullOrEmpty(id)) return Palette[0];

            var hash = 0;
            foreach (var c in id) hash = unchecked(hash * 31 + c);

            return Palette[Math.Abs(hash % Palette.Length)];
        }
    }
}
