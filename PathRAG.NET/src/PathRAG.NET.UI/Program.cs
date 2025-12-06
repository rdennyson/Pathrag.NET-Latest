using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PathRAG.NET.UI;
using PathRAG.NET.UI.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient for API calls with extended timeout for document processing
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7001/"),
    Timeout = TimeSpan.FromMinutes(10) // Extended timeout for document upload/processing
});

// Register API services
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IGraphService, GraphService>();

// Add AntDesign
builder.Services.AddAntDesign();

await builder.Build().RunAsync();

