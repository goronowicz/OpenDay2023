// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using System;
using System.Net;
using System.Text.Encodings.Web;
using Azure;
using Azure.Communication.Email;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackendFunctionApp
{
    public class Integration
    {
        private readonly ILogger<Integration> _logger;
        private readonly IOptions<IntegrationSettings> options;
        private string? instanceId;

        public Integration(ILogger<Integration> logger, IOptions<IntegrationSettings> options)
        {
            _logger = logger;
            this.options = options;
        }

        [Function(nameof(StartIntegration))]
        public async Task StartIntegration([EventGridTrigger] EventGridEvent cloudEvent,
            [DurableClient] DurableTaskClient client)
        {
            _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.EventType,
                cloudEvent.Subject);

            var data = cloudEvent.Data.ToObjectFromJson<OrderData>();

            await client.ScheduleNewOrchestrationInstanceAsync(nameof(IntergationHandler), data);
        }

        [Function(nameof(IntergationHandler))]
        public async Task IntergationHandler([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            _logger.LogInformation("Starting Orchestration");
            await context.CreateTimer(context.CurrentUtcDateTime.AddSeconds(20), CancellationToken.None);

            var orderData = context.GetInput<OrderData>();
            await context.CallActivityAsync<string>(nameof(CreateUser));

            await context.CallActivityAsync(nameof(RequestApproval), new RequestApprovalData
            {
                email = orderData.email,
                instanceId = context.InstanceId

            });

            await context.WaitForExternalEvent<Task>("RequestApproval");

            await context.CallActivityAsync<string>(nameof(CreateOrder));
            await context.CallActivityAsync<string>(nameof(UpdatePayment));
            _logger.LogInformation("All done");
        }

        [Function(nameof(CreateUser))]
        public async Task CreateUser([ActivityTrigger] FunctionContext context)
        {
            _logger.LogInformation("Creating User");
            if (new Random().NextSingle() % 2 == 0)
                throw new Exception("Unable to create User");
        }

        [Function(nameof(RequestApproval))]
        public async Task RequestApproval([ActivityTrigger] RequestApprovalData context)
        {
            _logger.LogInformation("Request Approval");

            var emailClient = new EmailClient(options.Value.EmailConnectionString);

            var body = $"https://backendfunctionappopenday2023.azurewebsites.net/api/handleApproval?instanceId={context.instanceId}";

            EmailSendOperation emailSendOperation = await emailClient.SendAsync(
                WaitUntil.Completed,
                "DoNotReply@44100b75-7232-4d42-897c-e22ae262ff69.azurecomm.net",
                context.email,
                subject: "Order Confirmation",
                htmlContent:body,
                plainTextContent: body);

            var status = emailSendOperation.GetRawResponse().Status;
        }

        [Function(nameof(HandleApproval))]
        public async Task<HttpResponseData> HandleApproval([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "handleApproval")] HttpRequestData req,
            [DurableClient] DurableTaskClient client)
        {
            _logger.LogInformation("Request Approval");
            instanceId = req.Query["instanceId"];
            
            await client.RaiseEventAsync(instanceId, "RequestApproval");
            return req.CreateResponse(HttpStatusCode.OK);
        }

        [Function(nameof(CreateOrder))]
        public async Task CreateOrder([ActivityTrigger] FunctionContext context)
        {
            _logger.LogInformation("Creating Order");
        }

        [Function(nameof(UpdatePayment))]
        public async Task UpdatePayment([ActivityTrigger] FunctionContext context)
        {
            _logger.LogInformation("Updating Payment");
        }

        public class OrderData
        {
            public string email { get; set; }
        }
    }

    public class IntegrationSettings
    {
        public string EmailConnectionString { get; set; }
    }

    public class RequestApprovalData 
    {
        public string email { get; set; }
        public string instanceId { get; set; }
    }
}
