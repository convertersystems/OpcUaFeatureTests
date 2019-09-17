using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Workstation.ServiceModel.Ua;
using Workstation.ServiceModel.Ua.Channels;

namespace OpcUaFeatureTests
{
    [TestClass]
    public class SubscriptionTests
    {
        /// <summary>
        /// Creates a subscription and monitors current time.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CreateDataSubscription()
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

                // build a CreateSubscriptionRequest. See 'OPC UA Spec Part 4' paragraph 5.13.2
                var req = new CreateSubscriptionRequest
                {
                    RequestedPublishingInterval = 1000.0, // intervals are in milliseconds
                    RequestedMaxKeepAliveCount = 30,
                    RequestedLifetimeCount = 30 * 3,
                    PublishingEnabled = true,
                };
                var res = await channel.CreateSubscriptionAsync(req);

                // the result will return the server's subscription id. You will needs this to 
                // add monitored items.
                var id = res.SubscriptionId;
                Console.WriteLine($"Created subscription '{id}'.");

                // build a CreateMonitoredItemsRequest. See 'OPC UA Spec Part 4' paragraph 5.12.2
                var req2 = new CreateMonitoredItemsRequest
                {
                    SubscriptionId = id,
                    TimestampsToReturn = TimestampsToReturn.Both,
                    ItemsToCreate = new MonitoredItemCreateRequest[]
                    {
                    new MonitoredItemCreateRequest
                    {
                        ItemToMonitor= new ReadValueId{ AttributeId= AttributeIds.Value, NodeId= NodeId.Parse(VariableIds.Server_ServerStatus_CurrentTime)},
                        MonitoringMode= MonitoringMode.Reporting,
                        // specify a unique ClientHandle. The ClientHandle is returned in the PublishResponse
                        RequestedParameters= new MonitoringParameters{ ClientHandle= 42, QueueSize= 2, DiscardOldest= true, SamplingInterval= 1000.0},
                    },
                    },
                };
                var res2 = await channel.CreateMonitoredItemsAsync(req2);

                Console.WriteLine("\nSubscribe to PublishResponse stream.");

                // when the session is open, the client sends a stream of PublishRequests to the server.
                // You can subscribe to all the PublishResponses -or- subscribe to the responses from 
                // a single subscription.
                var token = channel
                    // receive just the subscription we just created
                    .Where(pr => pr.SubscriptionId == id)
                    // subscribe with an 'OnNext' function, and an 'OnError' function
                    .Subscribe(
                        pr =>
                        {
                            // loop thru all the data change notifications and write them out.
                            var dcns = pr.NotificationMessage.NotificationData.OfType<DataChangeNotification>();
                            foreach (var dcn in dcns)
                            {
                                foreach (var min in dcn.MonitoredItems)
                                {
                                    Console.WriteLine($"sub: {pr.SubscriptionId}; handle: {min.ClientHandle}; value: {min.Value}");
                                }
                            }
                        },
                        ex => Console.WriteLine("Exception in publish response handler: {0}", ex.GetBaseException().Message)
                    );

                // publish for 5 seconds and then close.
                await Task.Delay(5000);

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
        /// Creates a subscription and monitors events.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
        [TestMethod]
        public async Task CreateEventSubscription()
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

                // build a CreateSubscriptionRequest. See 'OPC UA Spec Part 4' paragraph 5.13.2
                var req = new CreateSubscriptionRequest
                {
                    RequestedPublishingInterval = 500.0, // intervals are in milliseconds
                    RequestedMaxKeepAliveCount = 30,
                    RequestedLifetimeCount = 30 * 3,
                    PublishingEnabled = true,
                };
                var res = await channel.CreateSubscriptionAsync(req);

                // the result will return the server's subscription id. You will needs this to 
                // add monitored items.
                var id = res.SubscriptionId;
                Console.WriteLine($"Created subscription '{id}'.");

                // build a CreateMonitoredItemsRequest. See 'OPC UA Spec Part 4' paragraph 5.12.2
                var req2 = new CreateMonitoredItemsRequest
                {
                    SubscriptionId = id,
                    TimestampsToReturn = TimestampsToReturn.Both,
                    ItemsToCreate = new MonitoredItemCreateRequest[]
                    {
                    new MonitoredItemCreateRequest
                    {
                        ItemToMonitor= new ReadValueId{ AttributeId= AttributeIds.EventNotifier, NodeId= NodeId.Parse(ObjectIds.Server)},
                        MonitoringMode= MonitoringMode.Reporting,
                        // specify a unique ClientHandle. The ClientHandle is returned in the PublishResponse
                        RequestedParameters= new MonitoringParameters{ ClientHandle= 42, SamplingInterval=-1.0, QueueSize=1000, DiscardOldest=true,
                            // events require an EventFilter with a SelectClause (a list of fields to receive)
                            Filter = new EventFilter{ SelectClauses= EventHelper.GetSelectClauses<BaseEvent>() } },
                    },
                    },
                };
                var res2 = await channel.CreateMonitoredItemsAsync(req2);

                Console.WriteLine("\nSubscribe to PublishResponse stream.");

                // when the session is open, the client sends a stream of PublishRequests to the server.
                // You can subscribe to all the PublishResponses -or- subscribe to the responses from 
                // a single subscription.
                var token = channel
                    // receive responses for the subscription we just created
                    .Where(pr => pr.SubscriptionId == id)
                    // subscribe with an 'OnNext' function, and an 'OnError' function
                    .Subscribe(
                        pr =>
                        {
                        // loop thru all the event notifications and write them out.
                        var enls = pr.NotificationMessage.NotificationData.OfType<EventNotificationList>();
                            foreach (var enl in enls)
                            {
                                foreach (var efl in enl.Events)
                                {
                                    var ev = EventHelper.Deserialize<BaseEvent>(efl.EventFields);
                                    Console.WriteLine($"time: {ev.Time}, src: {ev.SourceName}, msg: {ev.Message}, sev: {ev.Severity}");
                                }
                            }
                        },
                        ex => Console.WriteLine("Exception in publish response handler: {0}", ex.GetBaseException().Message)
                   );

                // publish for 5 seconds and then close.
                for (int i = 0; i < 10; i++)
                {
                    // trigger an event on the Unified Automation server.
                    var writeResult = await channel.WriteAsync(
                        new WriteRequest
                        {
                        // Write true, false, true, false, ...
                        NodesToWrite = new[] {
                            new WriteValue {
                                NodeId = NodeId.Parse("ns=2;s=Demo.Events.Trigger_BaseEvent"),
                                AttributeId = AttributeIds.Value,
                                Value = new DataValue(i%2 == 0)
                            }
                            }
                        }
                    );
                    await Task.Delay(500);
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
    }
}
