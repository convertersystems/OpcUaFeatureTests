using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace OpcUaFeatureTests
{
    [TestClass]
    public class BasicConnectionTests
    {
        /// <summary>
        /// Connects to server endpoint without security.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task Connect()
        {
            // describe this client application.
            var clientDescription = new ApplicationDescription
            {
                ApplicationName = "Workstation.UaClient.FeatureTests",
                ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:Workstation.UaClient.FeatureTests",
                ApplicationType = ApplicationType.Client
            };

            // create a 'UaTcpSessionChannel', a client-side channel that opens a 'session' with the server.
            var channel = new UaTcpSessionChannel(
                clientDescription,
                null,  // no x509 client certificate
                new AnonymousIdentity(), // the anonymous identity
                "opc.tcp://localhost:48010", // the endpoint of Unified Automation's UaCPPServer.
                SecurityPolicyUris.None);

            try
            {
                // try opening a session and reading a few nodes.
                await channel.OpenAsync();

                // success! client session opened with these settings.
                Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                Console.WriteLine($"UserIdentityToken: '{channel.UserIdentity}'.");

                Console.WriteLine($"Closing session '{channel.SessionId}'.");
                await channel.CloseAsync();
            }
            catch (Exception ex)
            {
                await channel.AbortAsync();
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Connects to server endpoint with security using x509 certificate. The client
        /// selects the most secure endpoint. The client creates a self-signed certificate
        /// if one does not exist.  Your server will likely deny your client connection 
        /// until the certifcate is 'trusted'. How to trust a certificate is server specific.  
        /// With Unified Automation, look for the "UA Admin Dialog" application.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ConnectWithSecurity()
        {
            // describe this client application.
            var clientDescription = new ApplicationDescription
            {
                ApplicationName = "Workstation.UaClient.FeatureTests",
                ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:Workstation.UaClient.FeatureTests",
                ApplicationType = ApplicationType.Client
            };

            // place to store certificates
            var certificateStore = new DirectoryStore("./pki");

            // create a 'UaTcpSessionChannel', a client-side channel that opens a 'session' with the server.
            var channel = new UaTcpSessionChannel(
                clientDescription,
                certificateStore,
                new AnonymousIdentity(), // the anonymous identity
                "opc.tcp://localhost:48010"); // the endpoint of Unified Automation's UaCPPServer.

            try
            {
                // try opening a session and reading a few nodes.
                await channel.OpenAsync();

                // success! client session opened with these settings.
                Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                Console.WriteLine($"UserIdentityToken: '{channel.UserIdentity}'.");

                Console.WriteLine($"Closing session '{channel.SessionId}'.");
                await channel.CloseAsync();

            }
            catch (Exception ex)
            {
                await channel.AbortAsync();
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Connects to server endpoint with user name and password. The password
        /// will be encrypted. The Unified Automation's server accepts user "root" with password "secret"
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ConnectWithUserNameAndPassword()
        {
            // describe this client application.
            var clientDescription = new ApplicationDescription
            {
                ApplicationName = "Workstation.UaClient.FeatureTests",
                ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:Workstation.UaClient.FeatureTests",
                ApplicationType = ApplicationType.Client
            };

            // place to store certificates
            var certificateStore = new DirectoryStore("./pki");

            // create a 'UaTcpSessionChannel', a client-side channel that opens a 'session' with the server.
            var channel = new UaTcpSessionChannel(
                clientDescription,
                certificateStore,
                new UserNameIdentity("root", "secret"),
                "opc.tcp://localhost:48010"); // the endpoint of Unified Automation's UaCPPServer.

            try
            {
                // try opening a session and reading a few nodes.
                await channel.OpenAsync();

                // success! client session opened with these settings.
                Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                Console.WriteLine($"UserIdentityToken: '{channel.UserIdentity}'.");

                Console.WriteLine($"Closing session '{channel.SessionId}'.");
                await channel.CloseAsync();

            }
            catch (Exception ex)
            {
                await channel.AbortAsync();
                Console.WriteLine(ex.Message);
            }
        }
    }
}
