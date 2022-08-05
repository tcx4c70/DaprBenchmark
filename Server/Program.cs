var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.MapGet("/hello", () => {
    // Console.WriteLine("receive hello");
    return "world";
});
app.MapPost("/echo", (EchoRequest request) => request.content);

await app.RunAsync();

public record EchoRequest(string content);
