using System;

namespace haypagina.CLI
{
    public class IvonaEndpointInfo
    {
        public string Region;
        public string Host;

        public override string ToString()
        {
            return Region + " - " + Host;
        }

        public static IvonaEndpointInfo Get(IvonaEndpoint endpoint)
        {
            switch (endpoint)
            {
                case IvonaEndpoint.EuWest1:
                    return new IvonaEndpointInfo() {Region = "eu-west-1", Host = "tts.eu-west-1.ivonacloud.com"};

                case IvonaEndpoint.UsEast1:
                    return new IvonaEndpointInfo() {Region = "us-east-1", Host = "tts.us-east-1.ivonacloud.com"};

                case IvonaEndpoint.UsWest2:
                    return new IvonaEndpointInfo() {Region = "us-west-2", Host = "tts.us-west-2.ivonacloud.com"};

                default:
                    throw new Exception("Not found.");
            }
        }
    }
}