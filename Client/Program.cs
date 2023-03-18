using BlazorGinRummy;
using BlazorGinRummy.GinRummyGame;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BlazorGinRummy
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            //builder.Services.AddSingleton<GinRummyGameService>();
            builder.Services.AddSingleton<GinRummyGameService>();

            //var host = builder.Build();

            await builder.Build().RunAsync();

            //var ginRummyGameService = host.Services.GetRequiredService<GinRummyGameService>();

            //await host.RunAsync();
        }
    }
}