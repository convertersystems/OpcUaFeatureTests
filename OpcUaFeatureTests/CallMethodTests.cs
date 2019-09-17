using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace OpcUaFeatureTests
{
    [TestClass]
    public class CallMethodTests
    {
        /// <summary>
        /// Calls a method of the UACPPServer. This method multiplies two Doubles and returns the result.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task Multiply()
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

                Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                Console.WriteLine($"UserIdentityToken: '{channel.UserIdentity}'.");

                Console.WriteLine("\nCall Multiply method with two arguments.");
                var a = 6.0;
                var b = 7.0;

                // build a CallRequest. See 'OPC UA Spec Part 4' paragraph 5.11.2
                var request = new CallRequest
                {
                    MethodsToCall = new[] {
                    new CallMethodRequest
                    {
                        // calling a method usually requires two nodeids.
                        // the ObjectId specifies the object instance. 
                        ObjectId = NodeId.Parse("ns=2;s=Demo.Method"),
                        // the MethodId specifies the method.
                        MethodId = NodeId.Parse("ns=2;s=Demo.Method.Multiply"),
                        // input args are passed as an array of Variants.
                        InputArguments = new [] { new Variant(a), new Variant(b) }
                    }
                }
                };
                // send the request to the server.
                var response = await channel.CallAsync(request);
                // 'Results' will be an array of CallResponse, one for every CallMethodRequest.
                var result = response.Results[0].OutputArguments[0].GetValueOrDefault<double>();

                Console.WriteLine($"  {a}");
                Console.WriteLine($"* {b}");
                Console.WriteLine(@"  ------------------");
                Console.WriteLine($"  {result}");

                Console.WriteLine($"\nClosing session '{channel.SessionId}'.");
                await channel.CloseAsync();
            }
            catch (Exception ex)
            {
                await channel.AbortAsync();
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Calls a method of the UACPPServer. The method arguments are a custom structure.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task VectorAdd()
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
                "opc.tcp://localhost:48010", // the endpoint of Unified Automation's UaCPPServer.
                additionalTypes: new[] { typeof(Vector) });  // tell the decoder about any custom structures

            try
            {
                // try opening a session and reading a few nodes.
                await channel.OpenAsync();

                Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                Console.WriteLine($"UserIdentityToken: '{channel.UserIdentity}'.");

                Console.WriteLine("\nCall VectorAdd method with structure arguments.");
                var v1 = new Vector { X = 1.0, Y = 2.0, Z = 3.0 };
                var v2 = new Vector { X = 1.0, Y = 2.0, Z = 3.0 };

                // build a CallRequest. See 'OPC UA Spec Part 4' paragraph 5.11.2
                var request = new CallRequest
                {
                    MethodsToCall = new[] {
                    new CallMethodRequest
                    {
                        // calling a method usually requires two nodeids.
                        // the ObjectId specifies the object instance. 
                        ObjectId = NodeId.Parse("ns=2;s=Demo.Method"),
                        // the MethodId specifies the method.
                        MethodId = NodeId.Parse("ns=2;s=Demo.Method.VectorAdd"),
                        // input args are passed as an array of Variants.
                        InputArguments = new [] { new ExtensionObject(v1), new ExtensionObject(v2) }.ToVariantArray()
                    }
                }
                };
                // send the request to the server.
                var response = await channel.CallAsync(request);
                // 'Results' will be an array of CallResponse, one for every CallMethodRequest.
                var result = response.Results[0].OutputArguments[0].GetValueOrDefault<Vector>();

                Console.WriteLine($"  {v1}");
                Console.WriteLine($"+ {v2}");
                Console.WriteLine(@"  ------------------");
                Console.WriteLine($"  {result}");

                Console.WriteLine($"\nClosing session '{channel.SessionId}'.");
                await channel.CloseAsync();
            }
            catch (Exception ex)
            {
                await channel.AbortAsync();
                Console.WriteLine(ex.Message);
            }
        }

        [DataTypeId("nsu=http://www.unifiedautomation.com/DemoServer/;i=3002")]
        [BinaryEncodingId("nsu=http://www.unifiedautomation.com/DemoServer/;i=5054")]
        public class Vector : Structure
        {
            public double X { get; set; }

            public double Y { get; set; }

            public double Z { get; set; }

            public override void Encode(IEncoder encoder)
            {
                encoder.WriteDouble("X", this.X);
                encoder.WriteDouble("Y", this.Y);
                encoder.WriteDouble("Z", this.Z);
            }

            public override void Decode(IDecoder decoder)
            {
                this.X = decoder.ReadDouble("X");
                this.Y = decoder.ReadDouble("Y");
                this.Z = decoder.ReadDouble("Z");
            }

            public override string ToString() => $"{{ X={this.X}; Y={this.Y}; Z={this.Z}; }}";
        }
    }
}
