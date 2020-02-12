using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Ovh.Api;
using Ovh.Api.Models;

namespace OVH_DynDNS_v2
{
    class Program
    {
        static async Task Main(string[] args)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("config.json");

            IConfigurationRoot configuration = builder.Build();

            Config config = new Config();
            configuration.Bind(config);

            Console.WriteLine("Loaded settings :");
            Console.WriteLine($"   >>   Application Key:      {config.OvhApplicationKey}");
            Console.WriteLine($"   >>   Application Secret:   {config.OvhApplicationSecret}");
            Console.WriteLine($"   >>   Consumer Key:         {config.OvhConsumerKey}");
            Console.WriteLine($"   >>   Domain Name:          {config.OvhDomainName}");

            if (args.Length > 0 && args[0].Contains("get-ck"))
            {
                Client client = new Client("ovh-eu", config.OvhApplicationKey, config.OvhApplicationSecret);
                CredentialRequest credentialRequest = new CredentialRequest(
                    new List<AccessRight>
                    {
                        new AccessRight("GET", "/*"),
                        new AccessRight("PUT", "/*"),
                        new AccessRight("POST", "/*"),
                        new AccessRight("DELETE", "/*"),
                    },
                    "https://example.com/" // Change this URL if you don't want to see an unreachable webpage after you validated your consumer key. An unreachable webpage does not mean that the validation has failed.
                );

                CredentialRequestResult credentialRequestResult = await client.RequestConsumerKeyAsync(credentialRequest);
                Console.WriteLine($"Please visit {credentialRequestResult.ValidationUrl} to authenticate and press enter to continue");
                Console.ReadLine();

                client.ConsumerKey = credentialRequestResult.ConsumerKey;
                Console.WriteLine($"Your \"consumerKey\" is {credentialRequestResult.ConsumerKey}");
                Console.ReadLine();
            }
            else
            {
                OvhApiWrapper ovhApiWrapper = new OvhApiWrapper(config.OvhApplicationKey, config.OvhApplicationSecret, config.OvhConsumerKey, config.OvhDomainName);

                while (true)
                {
                    try
                    {
                        // Step 1 : getting public IP

                        string publicIp;

                        using (HttpClient httpClient = new HttpClient())
                        {
                            publicIp = await httpClient.GetStringAsync("https://api.ipify.org");
                        }

                        Console.WriteLine($"The current server public IP is: {publicIp}");

                        // Step 2 : getting the list of all A domains registered fot the domain

                        long[] records = await ovhApiWrapper.GetRecordsList("A");

                        // Step 3 : looping between each record to check if the target IP corresponds to the public IP obtained in Step 1

                        foreach (long recordId in records)
                        {
                            Record record = await ovhApiWrapper.GetRecordDetails(recordId);

                            Console.WriteLine($"The registered public IP in OVH DNS is: {record.Target}");

                            if (record.Target == publicIp)
                            {
                                Console.WriteLine("The current public IP and OVH target are identical! Rechecking later");
                            }
                            else
                            {
                                Console.WriteLine("The current public IP and OVH target are different! Updating the OVH target now");

                                // string previousIp = record.Target;

                                record.Target = publicIp;

                                await ovhApiWrapper.PutRecordDetails(recordId, record);
                                await ovhApiWrapper.PostRefreshZone();

                                Console.WriteLine("OVH target updated successfully!");

                                // Console.WriteLine("Sending notification email...");
                                //
                                // using (SmtpClient client = new SmtpClient(_config.MailSmtpHost, _config.MailSmtpPort)
                                // {
                                //     UseDefaultCredentials = false,
                                //     Credentials = new NetworkCredential(_config.MailSmtpUsername, _config.MailSmtpPassword),
                                //     EnableSsl = _config.MailEnableSsl,
                                //     Timeout = TimeSpan.FromSeconds(30).Milliseconds
                                // })
                                // {
                                //     MailMessage mailMessage = new MailMessage {From = new MailAddress(_config.MailFrom)};
                                //     mailMessage.To.Add(_config.MailTo);
                                //     mailMessage.Body =
                                //         $"Old IP address: {previousIp}\r\n" +
                                //         $"New IP address: {publicIp}";
                                //     mailMessage.Subject = $"OVH Domain Target Updated at {DateTime.Now:dd/MM/yyyy HH:mm:ss}!";
                                //     client.Send(mailMessage);
                                // }
                                //
                                // Console.WriteLine("Email notification sent!");
                            }
                        }

                        await Task.Delay(TimeSpan.FromMinutes(5));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Console.WriteLine("Recovering after error... Waiting 10 seconds before retrying...");
                        await Task.Delay(TimeSpan.FromSeconds(10));
                    }
                }
            }
        }
    }

    public class Config
    {
        public string OvhApplicationKey { get; set; }
        public string OvhApplicationSecret { get; set; }
        public string OvhConsumerKey { get; set; }
        public string OvhDomainName { get; set; }
    }

    public class PartialRecord
    {
        public string Target { get; set; }
    }
}