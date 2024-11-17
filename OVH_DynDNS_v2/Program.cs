using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
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

            Config config = new();
            configuration.Bind(config);

            Console.WriteLine("Loaded settings :");
            Console.WriteLine($"   >>   Application Key:      {config.OvhApplicationKey}");
            Console.WriteLine($"   >>   Application Secret:   {config.OvhApplicationSecret}");
            Console.WriteLine($"   >>   Consumer Key:         {config.OvhConsumerKey}");
            Console.WriteLine($"   >>   Domain Name:          {config.OvhDomainName}");

            if (args.Length > 0 && args[0].Contains("get-ck"))
            {
                Client client = new("ovh-eu", config.OvhApplicationKey, config.OvhApplicationSecret);
                CredentialRequest credentialRequest = new(
                    new List<AccessRight>
                    {
                        new("GET", "/*"),
                        new("PUT", "/*"),
                        new("POST", "/*"),
                        new("DELETE", "/*"),
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
                OvhApiWrapper ovhApiWrapper = new(config.OvhApplicationKey, config.OvhApplicationSecret, config.OvhConsumerKey, config.OvhDomainName);

                try
                {
                    // Step 1 : getting public IP

                    string publicIp;

                    using (HttpClient httpClient = new())
                    {
                        publicIp = await httpClient.GetStringAsync("https://api.ipify.org");
                    }

                    Console.WriteLine($"The current server public IP is: {publicIp}");

                    // Step 2 : getting the list of all A domains registered fot the domain

                    long[] records = await ovhApiWrapper.GetRecordsList("A");

                    // Step 3 : looping between each record to check if the target IP corresponds to the public IP obtained in Step 1

                    List<UpdatedRecord> updatedRecords = new();

                    foreach (long recordId in records)
                    {
                        PartialRecord partialRecord = await ovhApiWrapper.GetRecordDetails(recordId);

                        Console.WriteLine($"The registered public IP in OVH DNS is: {partialRecord.Target}");

                        if (partialRecord.Target == publicIp)
                        {
                            Console.WriteLine("The current public IP and OVH target are identical! Rechecking later");
                        }
                        else
                        {
                            Console.WriteLine("The current public IP and OVH target are different! Updating the OVH target now");

                            string previousTarget = partialRecord.Target;
                            partialRecord.Target = publicIp;

                            await ovhApiWrapper.PutRecordDetails(recordId, partialRecord);
                            await ovhApiWrapper.PostRefreshZone();

                            Console.WriteLine("OVH target updated successfully!");

                            updatedRecords.Add(new UpdatedRecord { PartialRecord = partialRecord, PreviousTarget = previousTarget });
                        }
                    }

                    // Step 4 : sending recap by email

                    if (config.MailEnableNotifications && updatedRecords.Count > 0)
                    {
                        Console.WriteLine("Sending notification email...");

                        using (SmtpClient client = new(config.MailSmtpHost, config.MailSmtpPort)
                               {
                                   UseDefaultCredentials = false,
                                   Credentials = new NetworkCredential(config.MailSmtpUsername, config.MailSmtpPassword),
                                   EnableSsl = config.MailEnableSsl,
                                   Timeout = TimeSpan.FromSeconds(20).Milliseconds
                               })
                        {
                            MailMessage mailMessage = new() { From = new MailAddress(config.MailFrom) };
                            mailMessage.To.Add(config.MailTo);

                            mailMessage.Body =
                                "Your public IP has changed! Here is the detail of the updated OVH DNS records:\r\n" +
                                "\r\n";

                            foreach (UpdatedRecord updatedRecord in updatedRecords)
                            {
                                mailMessage.Body +=
                                    $"{(!string.IsNullOrEmpty(updatedRecord.PartialRecord.SubDomain) ? "Sub-domain" : "Domain")} updated: " +
                                    $"{(!string.IsNullOrEmpty(updatedRecord.PartialRecord.SubDomain) ? updatedRecord.PartialRecord.SubDomain + "." : string.Empty)}" +
                                    $"{updatedRecord.PartialRecord.Zone}\r\n" +
                                    $"   >>   Old IP address: {updatedRecord.PreviousTarget}\r\n" +
                                    $"   >>   New IP address: {updatedRecord.PartialRecord.Target}\r\n" +
                                    "\r\n";
                            }

                            mailMessage.Subject = $"OVH Domain Target Updated on {DateTime.Now:dd/MM/yyyy HH:mm:ss}!";
                            client.Send(mailMessage);
                        }

                        Console.WriteLine("Email notification sent!");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.WriteLine("Recovering after error... Waiting 10 seconds before retrying...");
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

        public bool MailEnableNotifications { get; set; }
        public string MailSmtpHost { get; set; }
        public int MailSmtpPort { get; set; }
        public string MailSmtpUsername { get; set; }
        public string MailSmtpPassword { get; set; }
        public bool MailEnableSsl { get; set; }
        public string MailFrom { get; set; }
        public string MailTo { get; set; }
    }

    public class UpdatedRecord
    {
        public PartialRecord PartialRecord { get; set; }
        public string PreviousTarget { get; set; }
    }
}
