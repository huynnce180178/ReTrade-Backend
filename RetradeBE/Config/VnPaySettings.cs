namespace RetradeBE.Config;

public class VnPaySettings
{
    public string TmnCode { get; set; } = string.Empty;

    public string HashSecret { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiUrl { get; set; } = string.Empty;

    public string CallbackUrl { get; set; } = string.Empty;

    public string IpnUrl { get; set; } = string.Empty;

    public string FrontendReturnUrl { get; set; } = string.Empty;

    public string FrontendWalletReturnUrl { get; set; } = string.Empty;

    public string FrontendUpgradeReturnUrl { get; set; } = string.Empty;

    public string Version { get; set; } = "2.1.0";

    public string Command { get; set; } = "pay";

    public string CurrencyCode { get; set; } = "VND";

    public string Locale { get; set; } = "vn";
}
