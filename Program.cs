using System;
using System.Threading.Tasks;

namespace ApiWindowsService
{
    internal static class Program
    {
        static async Task Main()
        {
            try
            {
                Console.WriteLine("Starting API calls...");
                var client = new ApiClient();
                await client.LoginAsync();
                await client.PushDataAsync();
                Console.WriteLine("API calls completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.ExitCode = 1;
            }
        }
    }
}
