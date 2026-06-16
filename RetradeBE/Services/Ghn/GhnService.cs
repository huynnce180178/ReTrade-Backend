using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RetradeBE.Config;
using RetradeBE.Models.DTOs;

namespace RetradeBE.Services.Ghn
{
    public class GhnService : IGhnService
    {
        private readonly HttpClient _httpClient;
        private readonly GhnSettings _ghnSettings;

        public GhnService(HttpClient httpClient, IOptions<GhnSettings> ghnSettings)
        {
            _httpClient = httpClient;
            _ghnSettings = ghnSettings.Value;
        }

        public async Task<GhnCalculateFeeResponse> CalculateFeeAsync(GhnCalculateFeeRequest request)
        {
            var url = $"{_ghnSettings.BaseUrl}/shiip/public-api/v2/shipping-order/fee";

            var jsonRequest = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = content
            };

            requestMessage.Headers.Add("Token", _ghnSettings.Token);
            requestMessage.Headers.Add("ShopId", _ghnSettings.ShopId.ToString());

            var response = await _httpClient.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"GHN Error ({response.StatusCode}): {responseString}");
            }
            var result = JsonSerializer.Deserialize<GhnCalculateFeeResponse>(responseString);

            if (result == null)
            {
                throw new Exception("Failed to deserialize GHN response.");
            }

            return result;
        }
        public async Task<object> GetProvincesAsync()
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_ghnSettings.BaseUrl}/shiip/public-api/master-data/province");
            requestMessage.Headers.Add("Token", _ghnSettings.Token);
            var response = await _httpClient.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception(responseString);
            using var doc = JsonDocument.Parse(responseString);
            return JsonSerializer.Deserialize<object>(doc.RootElement.GetProperty("data").GetRawText());
        }

        public async Task<object> GetDistrictsAsync(int provinceId)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_ghnSettings.BaseUrl}/shiip/public-api/master-data/district?province_id={provinceId}");
            requestMessage.Headers.Add("Token", _ghnSettings.Token);
            var response = await _httpClient.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception(responseString);
            using var doc = JsonDocument.Parse(responseString);
            return JsonSerializer.Deserialize<object>(doc.RootElement.GetProperty("data").GetRawText());
        }

        public async Task<object> GetWardsAsync(int districtId)
        {
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_ghnSettings.BaseUrl}/shiip/public-api/master-data/ward?district_id={districtId}");
            requestMessage.Headers.Add("Token", _ghnSettings.Token);
            var response = await _httpClient.SendAsync(requestMessage);
            var responseString = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode) throw new Exception(responseString);
            using var doc = JsonDocument.Parse(responseString);
            return JsonSerializer.Deserialize<object>(doc.RootElement.GetProperty("data").GetRawText());
        }
    }
}
