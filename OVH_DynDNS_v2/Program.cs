using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace OVH_DynDNS_v2
{
    class Program
    {
        static void Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json");

            IConfigurationRoot configuration = builder.Build();

            Config config = new Config();
            configuration.Bind(config);

            Console.WriteLine(config.OvhApplicationKey);
            Console.WriteLine(config.OvhApplicationSecret);
            Console.WriteLine(config.OvhConsumerKey);
            Console.WriteLine(config.OvhDomainName);
        }
    }

    public class Config
    {
        public string OvhApplicationKey { get; set; }
        public string OvhApplicationSecret { get; set; }
        public string OvhConsumerKey { get; set; }
        public string OvhDomainName { get; set; }
    }
}