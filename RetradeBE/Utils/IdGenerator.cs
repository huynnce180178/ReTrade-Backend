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
    }
}
