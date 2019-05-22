using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace OpcUaFeatureTests
{
    [TestClass]
    public class HistoryTests
    {
        /// <summary>
        /// Tests reading the raw history of node 'Demo.History.DoubleWithHistory'.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ReadHistoryRawValues()
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
                "opc.tcp://localhost:48010",
                SecurityPolicyUris.Basic256Sha256);

            // try opening a session and reading a few nodes.
            await channel.OpenAsync();

            Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
            Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
            Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
            Console.WriteLine($"UserIdentityToken: '{channel.UserIdentity}'.");

            Console.WriteLine("Check if DataLogger active.");
            var req = new ReadRequest
            {
                NodesToRead = new[] {
                    new ReadValueId { NodeId = NodeId.Parse("ns=2;s=Demo.History.DataLoggerActive"), AttributeId = AttributeIds.Value }
                },
            };
            var res = await channel.ReadAsync(req);

            if (StatusCode.IsBad(res.Results[0].StatusCode))
            {
                throw new InvalidOperationException("Error reading 'Demo.History.DataLoggerActive'. ");
            }
            var isActive = res.Results[0].GetValueOrDefault<bool>();

            if (!isActive)
            {
                Console.WriteLine("Activating DataLogger.");

                var req1 = new CallRequest
                {
                    MethodsToCall = new[] {
                        new CallMethodRequest{
                            ObjectId =  NodeId.Parse("ns=2;s=Demo.History"), // parent node
				            MethodId = NodeId.Parse("ns=2;s=Demo.History.StartLogging")
                        },
                    },
                };
                var res1 = await channel.CallAsync(req1);

                if (StatusCode.IsBad(res1.Results[0].StatusCode))
                {
                    throw new InvalidOperationException("Error calling method 'Demo.History.StartLogging'.");
                }
                Console.WriteLine("Note: Datalogger has just been activated, so there will be little or no history data to read.");
                Console.WriteLine("      Try again in 10 minutes.");
            }

            Console.WriteLine("Reading history for last 1 minutes.");

            var start = DateTime.UtcNow.Add(TimeSpan.FromSeconds(-60));
            var end = DateTime.UtcNow;

            byte[] cp = null;

            while (true)
            {
                var req2 = new HistoryReadRequest
                {
                    HistoryReadDetails = new ReadRawModifiedDetails
                    {
                        StartTime = start,
                        EndTime = end,
                        NumValuesPerNode = 100,
                        ReturnBounds = false,
                    },
                    TimestampsToReturn = TimestampsToReturn.Both,
                    ReleaseContinuationPoints = false,
                    NodesToRead = new[] {
                        new HistoryReadValueId{ NodeId = NodeId.Parse("ns=2;s=Demo.History.DoubleWithHistory"), ContinuationPoint = cp },
                    },
                };

                var res2 = await channel.HistoryReadAsync(req2);

                if (StatusCode.IsGood(res2.Results[0].StatusCode))
                {
                    var historyData = res2.Results[0].HistoryData as HistoryData;

                    Console.WriteLine($"Found {historyData.DataValues.Length} value(s) for node '{req2.NodesToRead[0].NodeId}':");

                    foreach (var dv in historyData.DataValues)
                    {
                        Console.WriteLine($"Read {dv.Value}, q: {dv.StatusCode}, ts: {dv.SourceTimestamp}");
                    }

                    cp = res2.Results[0].ContinuationPoint;
                    if (cp == null)
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine($"HistoryRead return statuscode: {res2.Results[0].StatusCode}");
                    break;
                }
            }
            Console.WriteLine($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }

        /// <summary>
        /// Tests reading the aggregated history of node 'Demo.History.DoubleWithHistory'.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task ReadHistoryAggregatedValues()
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
                "opc.tcp://localhost:48010",
                SecurityPolicyUris.Basic256Sha256);

            // try opening a session and reading a few nodes.
            await channel.OpenAsync();

            Console.WriteLine($"Opened session with endpoint '{channel.RemoteEndpoint.EndpointUrl}'.");
            Console.WriteLine($"SecurityPolicy: '{channel.RemoteEndpoint.SecurityPolicyUri}'.");
            Console.WriteLine($"SecurityMode: '{channel.RemoteEndpoint.SecurityMode}'.");
            Console.WriteLine($"UserIdentityToken: '{channel.UserIdentity}'.");

            Console.WriteLine("Check if DataLogger active.");
            var req = new ReadRequest
            {
                NodesToRead = new[] {
                    new ReadValueId { NodeId = NodeId.Parse("ns=2;s=Demo.History.DataLoggerActive"), AttributeId = AttributeIds.Value }
                },
            };
            var res = await channel.ReadAsync(req);

            if (StatusCode.IsBad(res.Results[0].StatusCode))
            {
                throw new InvalidOperationException("Error reading 'Demo.History.DataLoggerActive'. ");
            }
            var isActive = res.Results[0].GetValueOrDefault<bool>();

            if (!isActive)
            {
                Console.WriteLine("Activating DataLogger.");

                var req1 = new CallRequest
                {
                    MethodsToCall = new[] {
                        new CallMethodRequest{
                            ObjectId =  NodeId.Parse("ns=2;s=Demo.History"), // parent node
				            MethodId = NodeId.Parse("ns=2;s=Demo.History.StartLogging")
                        },
                    },
                };
                var res1 = await channel.CallAsync(req1);

                if (StatusCode.IsBad(res1.Results[0].StatusCode))
                {
                    throw new InvalidOperationException("Error calling method 'Demo.History.StartLogging'.");
                }
                Console.WriteLine("Note: Datalogger has just been activated, so there will be little or no history data to read.");
                Console.WriteLine("      Try again in 10 minutes.");
            }

            Console.WriteLine("Reading the aggregated 1 min averages of the last 10 minutes...");

            var start = DateTime.UtcNow.Add(TimeSpan.FromSeconds(-600));
            var end = DateTime.UtcNow;

            byte[] cp = null;

            while (true)
            {
                var req2 = new HistoryReadRequest
                {
                    HistoryReadDetails = new ReadProcessedDetails
                    {
                        StartTime = start,
                        EndTime = end,
                        ProcessingInterval = 60000.0,
                        AggregateType = new[] {
                            NodeId.Parse(ObjectIds.AggregateFunction_Average),
                        },
                    },
                    TimestampsToReturn = TimestampsToReturn.Both,
                    ReleaseContinuationPoints = false,
                    NodesToRead = new[] {
                        new HistoryReadValueId{ NodeId = NodeId.Parse("ns=2;s=Demo.History.DoubleWithHistory"), ContinuationPoint = cp },
                    },
                };

                var res2 = await channel.HistoryReadAsync(req2);

                if (StatusCode.IsGood(res2.Results[0].StatusCode))
                {
                    var historyData = res2.Results[0].HistoryData as HistoryData;

                    Console.WriteLine($"Found {historyData.DataValues.Length} value(s) for node '{req2.NodesToRead[0].NodeId}':");

                    foreach (var dv in historyData.DataValues)
                    {
                        Console.WriteLine($"Read {dv.Value}, q: {dv.StatusCode}, ts: {dv.SourceTimestamp}");
                    }

                    cp = res2.Results[0].ContinuationPoint;
                    if (cp == null)
                    {
                        break;
                    }
                }
                else
                {
                    Console.WriteLine($"HistoryRead return statuscode: {res2.Results[0].StatusCode}");
                    break;
                }
            }
            Console.WriteLine($"Closing session '{channel.SessionId}'.");
            await channel.CloseAsync();
        }
    }
}
