using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using ManyConsole;



namespace haypagina.CLI
{
    public class Ivona : ConsoleCommand
    {
        public IvonaEndpointInfo Endpoint;
        public string IvonaAccessKey = "12345";
        string ivonaSecretKey = "67890";
        public bool UseFiddlerProxy = false;
        public DateTime UtcTime;

        public Ivona()
        {
            this.IsCommand("ivona");
            this.HasOption<IvonaEndpoint>("e=", String.Format("Endpoint id ({0}).", String.Join(", ", Enum.GetValues(typeof(IvonaEndpoint)).Cast<IvonaEndpoint>())), v => Endpoint = IvonaEndpointInfo.Get(v));
            this.HasOption("ak=", "Access key", v => IvonaAccessKey = v);
            this.HasOption("sk=", "Secret key", v => ivonaSecretKey = v);
            this.HasOption("f", "Use fiddler proxy (http://127.0.0.1:8888/).", v => UseFiddlerProxy = true);
        }

        public override int? OverrideAfterHandlingArgumentsBeforeRun(string[] remainingArguments)
        {
            this.Endpoint = this.Endpoint ?? IvonaEndpointInfo.Get(IvonaEndpoint.EuWest1);

            UtcTime = (IvonaAccessKey == "12345")
                ? new DateTime(2013, 9, 13, 09, 20, 54, DateTimeKind.Utc)
                : DateTime.UtcNow;

            return base.OverrideAfterHandlingArgumentsBeforeRun(remainingArguments);
        }

        public override  int Run(string[] remainingArguments)
        {
            var querystring = "";
            var bodyJson = "{\"Input\":{\"Data\":\"Hello world\"}}";  //JsonConvert.SerializeObject(body);

            var firstPartOfDate = UtcTime.ToString("yyyyMMdd");
            var fullDate = firstPartOfDate + "T" + UtcTime.ToString("HHmmss") + "Z";

            var url = "https://" + Endpoint.Host + "/CreateSpeech";

            var xAmzContentSha256 = LowercaseHex(Sha256Hash(bodyJson));

            //  Should be sorted in character code order
            var headersToBySigned = new []
            {
                new Tuple<string, string>("content-type", "application/json"),
                new Tuple<string, string>("host", Endpoint.Host),
                new Tuple<string, string>("x-amz-content-sha256", xAmzContentSha256),
                new Tuple<string, string>("x-amz-date", fullDate),
            };

            var canonicalHeaders = string.Join("", headersToBySigned.Select(h => String.Format("{0}:{1}\n", h.Item1, h.Item2)));

            var canonicalRequest = String.Join("\n", 
                "POST", 
                "/CreateSpeech", 
                querystring, 
                canonicalHeaders,
                String.Join(";", headersToBySigned.Select(i => i.Item1)),
                xAmzContentSha256);

            Console.WriteLine("Canonical request:");
            Console.WriteLine(canonicalRequest);
            Console.WriteLine();

            var stringToSign = string.Join("\n",
                "AWS4-HMAC-SHA256",
                fullDate,
                String.Format("{0}/{1}/tts/aws4_request", firstPartOfDate, Endpoint.Region),
                LowercaseHex(Sha256Hash(canonicalRequest)));

            Console.WriteLine("String to sign:");
            Console.WriteLine(stringToSign);
            Console.WriteLine();

            var signature =
                LowercaseHex(HmacSHA256(stringToSign, getSignatureKey(ivonaSecretKey, firstPartOfDate, Endpoint.Region, "tts")));

            Console.WriteLine("Signature:");
            Console.WriteLine(signature);

            var authorizationHeaderValue =
                string.Format(
                    "AWS4-HMAC-SHA256 Credential={0}/{1}/{2}/tts/aws4_request, SignedHeaders={3}, Signature={4}",
                    IvonaAccessKey,
                    firstPartOfDate, 
                    Endpoint.Region, 
                    string.Join(";", headersToBySigned.Select(h => h.Item1)),
                    signature);

            Console.WriteLine("authorization header:");
            Console.WriteLine(authorizationHeaderValue);
            Console.WriteLine();

            var request = WebRequest.CreateHttp(url) as HttpWebRequest;

            if (UseFiddlerProxy)
            {
                request.Proxy = new WebProxy("http://127.0.0.1:8888/");  
            }

            request.Method = "POST";

            request.Headers.Add("Authorization", authorizationHeaderValue);
            
            foreach (var header in headersToBySigned)
            {
                if (header.Item1 == "content-type")
                {
                    request.ContentType = header.Item2;
                }
                else if (header.Item1 == "host")
                {
                    request.Host = header.Item2;
                }
                else
                {
                    request.Headers.Add(header.Item1, header.Item2);
                }
            }

            using (var requestStream = request.GetRequestStream())
            using (var requestWriter = new StreamWriter(requestStream, Encoding.UTF8))
            {
                requestWriter.Write(bodyJson);
            }

            try
            {
                using (var response = request.GetResponse() as HttpWebResponse)
                {
                    Console.WriteLine(response.StatusCode);
                }
            }
            catch (WebException e)
            {
                var failureResponse = e.Response as HttpWebResponse;
                Console.WriteLine(failureResponse.StatusDescription);

                using (var responseStream = failureResponse.GetResponseStream())
                using (var streamReader = new StreamReader(responseStream))
                {
                    Console.WriteLine(streamReader.ReadToEnd());
                }

                throw;
            }

            return 0;
        }

        static byte[] getSignatureKey(String secretKey, String yyyyMMdd, String regionName, String serviceName)
        {
            byte[] kSecret = Encoding.UTF8.GetBytes(("AWS4" + secretKey).ToCharArray());
            byte[] kDate = HmacSHA256(yyyyMMdd, kSecret);
            byte[] kRegion = HmacSHA256(regionName, kDate);
            byte[] kService = HmacSHA256(serviceName, kRegion);
            byte[] kSigning = HmacSHA256("aws4_request", kService);

            return kSigning;
        }

        public static byte[] Sha256Hash(string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            return new SHA256Managed().ComputeHash(bytes);
        }

        static byte[] HmacSHA256(String data, byte[] key)
        {
            String algorithm = "HmacSHA256";
            KeyedHashAlgorithm kha = KeyedHashAlgorithm.Create(algorithm);
            kha.Key = key;

            return kha.ComputeHash(Encoding.UTF8.GetBytes(data));
        }

        public static string LowercaseHex(byte[] data, string prefix = "")
        {
            // From http://stackoverflow.com/a/22158486/32203

            char[] lookup = new char[]
            {
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'
            };
            int i = 0, p = prefix.Length, l = data.Length;
            char[] c = new char[l * 2 + p];
            byte d;
            for (; i < p; ++i) c[i] = prefix[i];
            i = -1;
            --l;
            --p;
            while (i < l)
            {
                d = data[++i];
                c[++p] = lookup[d >> 4];
                c[++p] = lookup[d & 0xF];
            }
            return new string(c, 0, c.Length);
        }
    }
}
