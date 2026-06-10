namespace RetradeBE.Models.DTOs;

public class ServiceSubscriptionDto
{
    public string ServiceId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string TargetRole { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int DurationDays { get; set; }

    public string BenefitsDescription { get; set; } = string.Empty;
}
