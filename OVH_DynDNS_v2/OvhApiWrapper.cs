using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OVH_DynDNS_v2
{
    public class OvhApiWrapper
    {
        private string ApplicationKey { get; }
        private string ApplicationSecret { get; }
        private string ConsumerKey { get; }
        private string DomainName { get; }

        public OvhApiWrapper(string applicationKey, string applicationSecret, string consumerKey, string domainName)
        {
            ApplicationKey = applicationKey;
            ApplicationSecret = applicationSecret;
            ConsumerKey = consumerKey;
            DomainName = domainName;
        }

        public async Task<long[]> GetRecordsList(string fieldType)
        {
            string query = $"https://eu.api.ovh.com/1.0/domain/zone/{DomainName}/record?fieldType={fieldType}";

            string timestamp = await GetOvhTimestamp();
            using ManagedHttpClient managedHttpClient = new ManagedHttpClient(ApplicationKey, ApplicationSecret, ConsumerKey, timestamp);
            managedHttpClient.BuildSignature("GET", query, string.Empty, timestamp);

            string result = await managedHttpClient.GetStringAsync(query);
            return JsonConvert.DeserializeObject<long[]>(result);
        }

        public async Task<Record> GetRecordDetails(long recordId)
        {
            string query = $"https://eu.api.ovh.com/1.0/domain/zone/{DomainName}/record/{recordId}";

            string timestamp = await GetOvhTimestamp();
            using ManagedHttpClient managedHttpClient = new ManagedHttpClient(ApplicationKey, ApplicationSecret, ConsumerKey, timestamp);
            managedHttpClient.BuildSignature("GET", query, string.Empty, timestamp);

            string result = await managedHttpClient.GetStringAsync(query);
            return JsonConvert.DeserializeObject<Record>(result);
        }

        public async Task PutRecordDetails(long dnsRecordId, Record record)
        {
            string query = $"https://eu.api.ovh.com/1.0/domain/zone/{DomainName}/record/{dnsRecordId}";

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver {NamingStrategy = new CamelCaseNamingStrategy()}
            };

            string jsonRecord = JsonConvert.SerializeObject(record, jsonSerializerSettings);
            StringContent stringContent = new StringContent(jsonRecord, Encoding.UTF8, "application/json");

            string timestamp = await GetOvhTimestamp();
            using ManagedHttpClient managedHttpClient = new ManagedHttpClient(ApplicationKey, ApplicationSecret, ConsumerKey, timestamp);
            managedHttpClient.BuildSignature("PUT", query, await stringContent.ReadAsStringAsync(), timestamp);

            await managedHttpClient.PutAsync(query, stringContent);
        }

        public async Task PostRefreshZone()
        {
            string query = $"https://eu.api.ovh.com/1.0/domain/zone/{DomainName}/refresh";

            string timestamp = await GetOvhTimestamp();
            using ManagedHttpClient managedHttpClient = new ManagedHttpClient(ApplicationKey, ApplicationSecret, ConsumerKey, timestamp);
            managedHttpClient.BuildSignature("POST", query, string.Empty, timestamp);

            await managedHttpClient.PostAsync(query, null);
        }

        private static async Task<string> GetOvhTimestamp()
        {
            using HttpClient httpClient = new HttpClient();
            return await httpClient.GetStringAsync("https://eu.api.ovh.com/1.0/auth/time");
        }
    }

    public class Record
    {
        public string Target { get; set; }
    }

    internal class ManagedHttpClient : HttpClient
    {
        private string ApplicationKey { get; }
        private string ApplicationSecret { get; }
        private string ConsumerKey { get; }
        private string Timestamp { get; }

        public ManagedHttpClient(string applicationKey, string applicationSecret, string consumerKey, string timestamp)
        {
            ApplicationKey = applicationKey;
            ApplicationSecret = applicationSecret;
            ConsumerKey = consumerKey;
            Timestamp = timestamp;

            DefaultRequestHeaders.Add("X-Ovh-Application", ApplicationKey);
            DefaultRequestHeaders.Add("X-Ovh-Consumer", ConsumerKey);
            DefaultRequestHeaders.Add("X-Ovh-Timestamp", Timestamp);

            Timeout = TimeSpan.FromSeconds(15);
        }

        public void BuildSignature(string method, string query, string body, string timestamp)
        {
            string preHash = $"{ApplicationSecret}+{ConsumerKey}+{method}+{query}+{body}+{timestamp}";

            using SHA1Managed sha1Managed = new SHA1Managed();

            byte[] hash = sha1Managed.ComputeHash(Encoding.UTF8.GetBytes(preHash));
            StringBuilder stringBuilder = new StringBuilder(hash.Length * 2);

            foreach (byte b in hash)
            {
                stringBuilder.Append(b.ToString("x2"));
            }

            DefaultRequestHeaders.Add("X-Ovh-Signature", $"$1${stringBuilder}");
        }
    }
}