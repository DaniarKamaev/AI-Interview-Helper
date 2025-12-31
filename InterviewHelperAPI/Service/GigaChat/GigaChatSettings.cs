namespace InterviewHelperAPI.Service.GigaChat;

public class GigaChatSettings
{
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string AuthUrl { get; set; } = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
    public string ApiUrl { get; set; } = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";
    public int MaxTokens { get; set; } = 2000;
    public double Temperature { get; set; } = 0.7;
}