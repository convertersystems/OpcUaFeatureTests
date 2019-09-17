using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace OpcUaFeatureTests
{
    [TestClass]
    public class BrowseReadWriteTests
    {

        /// <summary>
        /// Connects to server and reads the current ServerState. 
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ReadServerState()
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

                // build a ReadRequest. See 'OPC UA Spec Part 4' paragraph 5.10.2
                var readRequest = new ReadRequest
                {
                    // set the NodesToRead to an array of ReadValueIds.
                    NodesToRead = new[] {
                    // construct a ReadValueId from a NodeId and AttributeId.
                    new ReadValueId {
                        // use a saved NodeId or Parse the nodeId from a string.
                        // e.g. "ns=2;s=Demo.Static.Scalar.Double"
                        NodeId = NodeId.Parse(VariableIds.Server_ServerStatus),
                        // variable class nodes have a Value attribute.
                        AttributeId = AttributeIds.Value
                    }
                }
                };
                // send the ReadRequest to the server.
                var readResult = await channel.ReadAsync(readRequest);

                // the result will have a array 'Results' of DataValues, one for every ReadValueId.
                // A DataValue holds the sampled value, timestamps and quality status code.
                var serverStatus = readResult.Results[0].GetValueOrDefault<ServerStatusDataType>();

                Console.WriteLine("\nServer status:");
                Console.WriteLine("  ProductName: {0}", serverStatus.BuildInfo.ProductName);
                Console.WriteLine("  SoftwareVersion: {0}", serverStatus.BuildInfo.SoftwareVersion);
                Console.WriteLine("  ManufacturerName: {0}", serverStatus.BuildInfo.ManufacturerName);
                Console.WriteLine("  State: {0}", serverStatus.State);
                Console.WriteLine("  CurrentTime: {0}", serverStatus.CurrentTime);

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
        /// Connects to server and browses ObjectsFolder. 
        /// From a given start node, Browse returns the References of the node. There are many
        /// options to filter the result regarding reference type and direction.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task BrowseObjectFolder()
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
                new AnonymousIdentity(),
                "opc.tcp://localhost:48010"); // the endpoint of Unified Automation's UaCPPServer.

            try
            {
                // try opening a session and reading a few nodes.
                await channel.OpenAsync();

                Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
                Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
                Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
                Console.WriteLine($"UserIdentityToken: '{channel.UserIdentity}'.");

                // build a BrowseRequest. See 'OPC UA Spec Part 4' section 5.8.2
                var browseRequest = new BrowseRequest
                {
                    NodesToBrowse = new[] {
                    new BrowseDescription {
                        // gather references of this nodeid.
                        NodeId = NodeId.Parse(ObjectIds.ObjectsFolder),
                        // include just 'Forward' references
                        BrowseDirection = BrowseDirection.Forward,
                        // include 'HierarchicalReferences'
                        ReferenceTypeId = NodeId.Parse(ReferenceTypeIds.HierarchicalReferences),
                        // include 'HierarchicalReferences' and all subtypes of 'HierarchicalReferences'
                        IncludeSubtypes = true,
                        // include all classes of node
                        NodeClassMask = (uint)NodeClass.Unspecified,
                        // return reference descriptions with all the fields filled out
                        ResultMask = (uint)BrowseResultMask.All,
                    }
                },
                    RequestedMaxReferencesPerNode = 1000
                };

                // send the request to the server
                var browseResponse = await channel.BrowseAsync(browseRequest).ConfigureAwait(false);

                Console.WriteLine("\n+ Objects, 0:Objects, Object, i=85");

                Assert.IsNotNull(browseResponse.Results[0].References);
                foreach (var rd in browseResponse.Results[0].References)
                {
                    Console.WriteLine("   + {0}, {1}, {2}, {3}", rd.DisplayName, rd.BrowseName, rd.NodeClass, rd.NodeId);
                }

                // it is good practice to be prepared to receive a continuationPoint. 
                // ContinuationPoints are returned when the server has more information
                // than can be delivered in current response.
                // To test this code, you can reduce the above RequestedMaxReferencesPerNode
                // to 1.
                var cp = browseResponse.Results[0].ContinuationPoint;
                while (cp != null)
                {
                    var browseNextRequest = new BrowseNextRequest { ContinuationPoints = new[] { cp }, ReleaseContinuationPoints = false };
                    var browseNextResponse = await channel.BrowseNextAsync(browseNextRequest);
                    Assert.IsNotNull(browseNextResponse.Results[0].References);
                    foreach (var rd in browseNextResponse.Results[0].References)
                    {
                        Console.WriteLine("   + {0}, {1}, {2}", rd.DisplayName, rd.BrowseName, rd.NodeClass);
                    }
                    cp = browseNextResponse.Results[0].ContinuationPoint;
                }

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
        /// Connects to server and reads a slice of an array. 
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ReadIndexRange()
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

                // build a ReadRequest. See 'OPC UA Spec Part 4' paragraph 5.10.2
                var readRequest = new ReadRequest
                {
                    // set the NodesToRead to an array of ReadValueIds.
                    NodesToRead = new[] {
                    // construct a ReadValueId from a NodeId and AttributeId.
                    new ReadValueId {
                        // use a saved NodeId or Parse the nodeId from a string.
                        // e.g. "ns=2;s=Demo.Static.Scalar.Double"
                        NodeId = NodeId.Parse("ns=2;s=Demo.CTT.AllProfiles.Arrays.Double"),
                        // variable class nodes have a Value attribute.
                        AttributeId = AttributeIds.Value,
                        // ask to read a range of the underlying array
                        IndexRange = "1:2"
                    }
                }
                };
                // send the ReadRequest to the server.
                var readResult = await channel.ReadAsync(readRequest);

                // 'Results' will be an array of DataValues, one for every ReadValueId.
                // A DataValue holds the sampled value, timestamps and quality status code.
                var result = readResult.Results[0].GetValueOrDefault<double[]>();

                Console.WriteLine($"Read result: {string.Join(' ', result)}");

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
        /// Connects to server and write a slice of an array. 
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task WriteIndexRange()
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

                // build a WriteRequest. See 'OPC UA Spec Part 4' paragraph 5.10.4
                var writeRequest = new WriteRequest
                {
                    // set the NodesToWrite to an array of WriteValues.
                    NodesToWrite = new[] {
                    // construct a WriteValue from a NodeId, AttributeId and DataValue.
                    new WriteValue {
                        // use a saved NodeId or Parse the nodeId from a string.
                        // e.g. "ns=2;s=Demo.Static.Scalar.Double"
                        NodeId = NodeId.Parse("ns=2;s=Demo.CTT.AllProfiles.Arrays.Double"),
                        // variable class nodes have a Value attribute.
                        AttributeId = AttributeIds.Value,
                        // ask to write an slice of the underlying array
                        IndexRange = "1:2",
                        // the DataValue type has to match the underlying array type exactly.
                        Value = new DataValue(new double[]{41.0, 42.0}),
                    }
                }
                };
                // send the WriteRequest to the server.
                var writeResult = await channel.WriteAsync(writeRequest);

                // 'Results' will be a array of status codes, one for every WriteValue.
                var result = writeResult.Results[0];

                Console.WriteLine($"Write result: {result}");

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
