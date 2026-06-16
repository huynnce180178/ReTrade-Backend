using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RetradeBE.Models.DTOs
{
    public class GhnCalculateFeeRequest
    {
        [JsonPropertyName("service_type_id")]
        public int ServiceTypeId { get; set; } = 5;

        [JsonPropertyName("from_district_id")]
        public int? FromDistrictId { get; set; }

        [JsonPropertyName("from_ward_code")]
        public string? FromWardCode { get; set; }

        [JsonPropertyName("to_district_id")]
        public int ToDistrictId { get; set; }

        [JsonPropertyName("to_ward_code")]
        public string? ToWardCode { get; set; }

        [JsonPropertyName("length")]
        public int? Length { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("weight")]
        public int Weight { get; set; }

        [JsonPropertyName("insurance_value")]
        public int InsuranceValue { get; set; }

        [JsonPropertyName("coupon")]
        public string? Coupon { get; set; }

        [JsonPropertyName("items")]
        public List<GhnItem> Items { get; set; } = new List<GhnItem>();
    }

    public class GhnItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = null!;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("length")]
        public int Length { get; set; }

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }

        [JsonPropertyName("weight")]
        public int Weight { get; set; }
    }

    public class GhnCalculateFeeResponse
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = null!;

        [JsonPropertyName("data")]
        public GhnFeeData? Data { get; set; }
    }

    public class GhnFeeData
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("service_fee")]
        public int ServiceFee { get; set; }
    }

    public class CalculateFeeRequestDto
    {
        public string ProductId { get; set; } = null!;
        public string AddressId { get; set; } = null!;
    }

    public class CalculateFeeResponseDto
    {
        public decimal ShippingFee { get; set; }
    }

    public class CheckoutRequestDto
    {
        public string ProductId { get; set; } = null!;
        public string AddressId { get; set; } = null!;
        public int Quantity { get; set; } = 1;
        public string? PaymentMethod { get; set; }
    }
}
