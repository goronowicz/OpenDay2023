using System.Net;
using Azure;
using Azure.Data.Tables;
using Azure.Messaging.EventGrid;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker.Extensions.Tables;
using ITableEntity = Azure.Data.Tables.ITableEntity;
//using Azure.Messaging;

namespace FrontFunctionApp
{

    public class OrderFunction
    {
        private readonly ILogger _logger;

        public OrderFunction(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<OrderFunction>();
        }

        [Function(nameof(Order))]
        
        public async Task<OrderResponse> Order([HttpTrigger(AuthorizationLevel.Function, "post", Route = "order")] HttpRequestData req, 
           [TableInput("Orders", Connection = "DataStorage")] TableClient orders)
        {
            _logger.LogInformation($"${nameof(Order)} Started");

            var orderRequest = await req.ReadFromJsonAsync<OrderRequest>();

            var orderData = new OrderData()
            {
                email = orderRequest.email,
                PartitionKey = orderRequest.email,
                RowKey = Guid.NewGuid().ToString()
            };
            var status = await orders.AddEntityAsync(orderData);

            return new OrderResponse()
            {
                HttpResponse =
                    req.CreateResponse(HttpStatusCode.OK),
                orderData = new EventGridEvent("Integration", nameof(EventGridEvent), "1.0", orderData, typeof(OrderData))

            };
        }
    }

    public class OrderResponse
    {
        public HttpResponseData HttpResponse { get; set; }

        [EventGridOutput(TopicEndpointUri = "IntegrationUri", TopicKeySetting = "IntegrationKey")]
        public EventGridEvent orderData { get; set; }
    }

    public class OrderRequest
    {
        public string email { get; set; }
    }

    public class OrderData : ITableEntity
    {
        public string email { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
