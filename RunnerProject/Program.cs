using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Async_await_Task;

namespace RunnerProject
{
    public class Program
    {
        public static  void Main(string[] args)
        {
            MainAsync().Wait();
        }

        public static async Task MainAsync()
        {
            Console.WriteLine("Enter path:");
            string path = Console.ReadLine();

            Console.WriteLine("Enter url:");
            string url = Console.ReadLine();

            Console.WriteLine("Enter amount of parsing levels:");
            
            Int32.TryParse(Console.ReadLine(), out int level);

            var saver = new HtmlParser(url, path, level);
            await saver.StartSavingAsync();
        }
    }
}
