using System;

namespace RetradeBE.Utils
{
    public static class IdGenerator
    {
        private static readonly Random _random = new Random();

        public static string GenerateId(string prefix)
        {
            string datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            int randomPart = _random.Next(100000, 1000000);
            return $"{prefix}_{datePart}_{randomPart}";
        }

        public static string GenerateOrderId(int sequence)
        {
            string datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            int randomPart = _random.Next(100000, 1000000);
            return $"ord_{datePart}_{sequence:D6}_{randomPart}";
        }

        public static string GenerateTransactionId(string paymentMethod)
        {
            string prefix = paymentMethod.ToLower();
            if (prefix.Length > 5) prefix = prefix.Substring(0, 5); // shorten if necessary
            string timePart = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            int randomPart = _random.Next(100000, 1000000);
            return $"{prefix}_{timePart}_{randomPart}";
        }

        public static string CleanNameForId(string name, int maxLength = 6)
        {
            if (string.IsNullOrWhiteSpace(name)) return "name";
            
            // Remove diacritics / accents
            string cleaned = name.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            foreach (char c in cleaned)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    if (char.IsLetterOrDigit(c))
                    {
                        sb.Append(char.ToLower(c));
                    }
                    else if (c == ' ' || c == '-' || c == '_')
                    {
                        sb.Append('_');
                    }
                }
            }
            string result = sb.ToString().Replace("__", "_").Trim('_');
            if (result.Length > maxLength) result = result.Substring(0, maxLength);
            return string.IsNullOrEmpty(result) ? "val" : result;
        }
    }
}
