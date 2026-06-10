using System;

namespace RetradeBE.Models.DTOs
{
    public class MyServiceDto
    {
        public string ServiceId { get; set; } = string.Empty;
        public string UserSubId { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
