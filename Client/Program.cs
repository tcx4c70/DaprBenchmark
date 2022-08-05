using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;

public class Client
{
    private string m_url;
    private string m_appId;
    private int m_numWorkers;
    private int m_blocksPerWorker;
    private int m_reqPerBlock;
    private int m_times;

    public record EchoMsg([property: JsonPropertyName("content")] string content);

    Client()
    {
        m_url             = (Environment.GetEnvironmentVariable("BASE_URL") ?? "http://localhost") + ":" +
                            (Environment.GetEnvironmentVariable("PORT") ?? "7001");
        m_appId           = Environment.GetEnvironmentVariable("SERVER_APP_ID") ?? "server";
        m_numWorkers      = Int32.Parse(Environment.GetEnvironmentVariable("NUM_WORKERS")       ?? "250");
        m_blocksPerWorker = Int32.Parse(Environment.GetEnvironmentVariable("BLOCKS_PER_WORKER") ?? "10");
        m_reqPerBlock     = Int32.Parse(Environment.GetEnvironmentVariable("REQUEST_PER_BLOCK") ?? "500");
        m_times           = Int32.Parse(Environment.GetEnvironmentVariable("RUN_TIMES")         ?? "10");
        Console.WriteLine($"url: {m_url}");
        Console.WriteLine($"appId: {m_appId}");
        Console.WriteLine($"numWorkers: {m_numWorkers}");
        Console.WriteLine($"blocksPerWorker: {m_blocksPerWorker}");
        Console.WriteLine($"reqPerBlock: {m_reqPerBlock}");
        Console.WriteLine($"times: {m_times}");
        Console.WriteLine();
    }

    public async Task RunTest(string name, Func<string, Func<HttpClient, ValueTask>> issueRequestFactory)
    {
        Console.WriteLine($"{name} test:");
        var loadGenerator = new ConcurrentLoadGenerator<HttpClient>(
            m_numWorkers,
            m_blocksPerWorker,
            m_reqPerBlock,
            issueRequest: issueRequestFactory(m_url),
            getStateForWorker: _ => {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Add("dapr-app-id", m_appId);
                return client;
            }
        );
        await loadGenerator.Warmup();
        for (int i = 0; i < m_times; ++i)
        {
            Console.Write($"{i, 4}: ");
            await loadGenerator.Run();
        }
        GC.Collect();
        Console.WriteLine();
    }

    public static async Task Main()
    {
        Client client = new Client();
        await client.RunTest("GET", url => client => new ValueTask(client.GetAsync($"{url}/hello")));
        await client.RunTest("Post(json)", url => client =>
            new ValueTask(client.PostAsync($"{url}/echo", new StringContent(JsonSerializer.Serialize<EchoMsg>(new EchoMsg("Just a test."))))));

        // var client = new HttpClient();
        // client.DefaultRequestHeaders.Add("dapr-app-id", appId);
        // var reply = await client.GetAsync($"{url}/hello");
        // Console.WriteLine(await reply.Content.ReadAsStringAsync());
//
        // var request = new StringContent(JsonSerializer.Serialize<EchoMsg>(new EchoMsg("This is a test.")), Encoding.UTF8, "application/json");
        // var response = await client.PostAsync($"{url}/echo", request);
        // Console.WriteLine(response);
        // Console.WriteLine(await response.Content.ReadAsStringAsync());
    }
}
