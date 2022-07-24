using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json.Linq;

namespace HitbotSqlite.Commands;

[Group("wiki")]
public class WikiModule : BaseCommandModule
{
    private readonly HttpClient http;

    public WikiModule(HttpClient ht)
    {
        http = ht;
    }

    [Command("random")]
    public async Task RandomWikiArticleTask(CommandContext ctx)
    {
        var response = await http.GetAsync("https://en.wikipedia.org/api/rest_v1/page/random/summary");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        string? finalresponse = Convert.ToString(JObject.Parse(content)["content_urls"]?["desktop"]?["page"]);
        if (finalresponse is null)
        {
            await ctx.RespondAsync("this should never happen");
            return;
        }

        await ctx.RespondAsync(finalresponse);
    }

    [Command("search")]
    public async Task SearchWikipediaTask(CommandContext ctx, [RemainingText] string searchstring)
    {
        var response =
            await http.GetAsync(
                $"https://en.wikipedia.org/w/api.php?action=opensearch&search={searchstring}&limit=1&namespace=0&format=json");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        string wa = content.Split('[')[4].Substring(1);
        Console.WriteLine(wa);
        if (wa.IndexOf("\"") == -1)
        {
            await ctx.RespondAsync("not found");
            return;
        }

        wa = wa.Substring(0, wa.IndexOf("\""));
        await ctx.RespondAsync(wa);
    }
}

public class ApiStuffModule : BaseCommandModule
{
    private readonly HttpClient http;

    public ApiStuffModule(HttpClient ht)
    {
        http = ht;
    }

    [Command("dadjoke")]
    public async Task DadJokeCommand(CommandContext ctx)
    {
        using (var request = new HttpRequestMessage(HttpMethod.Get, "https://icanhazdadjoke.com/"))
        {
            request.Headers.Add("Accept", "text/plain");
            var response = await http.SendAsync(request);
            await ctx.RespondAsync(await response.Content.ReadAsStringAsync());
        }
    }

    [Command("qr")]
    [Description(
        "Attempts to generate and then embed a QR code based on the link you send in this command. Only lasts for 24 hours.")]
    public async Task QrCommand(CommandContext ctx, string url)
    {
        await ctx.RespondAsync($"https://qrtag.net/api/qr.png?url={url}");
    }

    [Command("fact")]
    [Description("Gets a random fact from the API.")]
    public async Task FactCommand(CommandContext ctx)
    {
        var response = await http.GetAsync("https://uselessfacts.jsph.pl/random.json?language=en");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        string? finalresponse = Convert.ToString(JObject.Parse(content)["text"]);
        if (finalresponse is null)
        {
            await ctx.RespondAsync("this should never happen");
            return;
        }

        await ctx.RespondAsync(finalresponse);
    }

    [Command("urlshorten")]
    [Description(
        "Shortens a given URL with the 1pt API. Optionally specify a custom short URL. Will give you back a random five-letter string if no custom name is specified, or if the name you specified is already taken.")]
    public async Task UrlShortenCommand(CommandContext ctx, string url, string shorturl = "hi")
    {
        var response = await http.GetAsync($"https://api.1pt.co/addURL?long={url}&short={shorturl}");
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        string? finalresponse = Convert.ToString(JObject.Parse(content)["short"]);
        if (finalresponse is null)
        {
            await ctx.RespondAsync("this should never happen");
            return;
        }

        await ctx.RespondAsync($"https://1pt.co/{finalresponse}");
    }
}