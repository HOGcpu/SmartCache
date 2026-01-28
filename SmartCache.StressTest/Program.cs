using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

class Program
{
    static readonly HttpClient client = new HttpClient();
    static readonly string[] domains =
    {
        "gmail.com","yahoo.com","outlook.com","example.com","test.com",
        "nomail.com","mail.com","hotmail.com","protonmail.com","abc.com"
    };
    static readonly Random rnd = new Random();

    static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false)
            .AddCommandLine(args)
            .Build();

        string apiUrl = config["StressTest:ApiUrl"];

        // Security headers
        var apiKey = config["Security:ApiKey"];
        var bearer = config["Security:BearerToken"];

        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer);

        const int emailCount = 100;
        const int requestsPerEmail = 10;

        Console.WriteLine("Waiting 10 seconds for API to start...");
        await Task.Delay(10000);

        do 
        {
            Console.WriteLine($"Starting stress test against {apiUrl}");

            var emails = GenerateEmails(emailCount);

            // POST
            var postMetrics = await RunRequests(
                emails, requestsPerEmail,
                email => SendRequest(apiUrl + email, isPost: true)
            );
            Console.WriteLine($"POST Success: {postMetrics.success}, Fail: {postMetrics.fail}");
            Console.WriteLine($"POST RPS: {postMetrics.rps:F2}");

            // GET
            var getMetrics = await RunRequests(
                emails, requestsPerEmail,
                email => SendRequest(apiUrl + email, isPost: false)
            );
            Console.WriteLine($"GET Success: {getMetrics.success}, Fail: {getMetrics.fail}");
            Console.WriteLine($"GET RPS: {getMetrics.rps:F2}");

            Console.WriteLine("Stress test completed!");
            Console.WriteLine("Press 'R' to run stress test again or any other key to exit");
        } 
        while (Console.ReadKey(true).Key == ConsoleKey.R);
    }

    static async Task<(int success, int fail, double rps)> RunRequests(string[] emails, int requestsPerEmail, Func<string, Task<bool>> requestFunc)
    {
        int success = 0, fail = 0;
        var sw = Stopwatch.StartNew();
        var tasks = new List<Task>();

        foreach (var email in emails)
        {
            for (int i = 0; i < requestsPerEmail; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    if (await requestFunc(email))
                        success++;
                    else
                        fail++;
                }));
            }
        }

        await Task.WhenAll(tasks);
        sw.Stop();
        double rps = (success + fail) / sw.Elapsed.TotalSeconds;
        return (success, fail, rps);
    }

    static string[] GenerateEmails(int count)
    {
        var emails = new string[count];
        for (int i = 0; i < count; i++)
            emails[i] = $"{RandomString(5, 10)}@{domains[rnd.Next(domains.Length)]}";
        return emails;
    }

    static string RandomString(int minLength, int maxLength)
    {
        int length = Random.Shared.Next(minLength, maxLength + 1);

        // letters + digits
        const string letters = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string chars = letters + digits;

        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
            sb.Append(chars[Random.Shared.Next(chars.Length)]);

        return sb.ToString();
    }

    static async Task<bool> SendRequest(string url, bool isPost)
    {
        try
        {
            var response = isPost ? await client.PostAsync(url, null) : await client.GetAsync(url);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
