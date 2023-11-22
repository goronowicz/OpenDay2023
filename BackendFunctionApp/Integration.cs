// Default URL for triggering event grid function in the local environment.
// http://localhost:7071/runtime/webhooks/EventGrid?functionName={functionname}

using System;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace BackendFunctionApp
{
    public class Integration
    {
        private readonly ILogger<Integration> _logger;

        public Integration(ILogger<Integration> logger)
        {
            _logger = logger;
        }

        [Function(nameof(StartIntegration))]
        public async Task StartIntegration([EventGridTrigger] EventGridEvent cloudEvent,
            [DurableClient] DurableTaskClient client)
        {
            _logger.LogInformation("Event type: {type}, Event subject: {subject}", cloudEvent.EventType, cloudEvent.Subject);

            await client.ScheduleNewOrchestrationInstanceAsync(nameof(IntergationHandler));
        }

        [Function(nameof(IntergationHandler))]
        public async Task IntergationHandler([OrchestrationTrigger] TaskOrchestrationContext context)
        {
            _logger.LogInformation("Starting Orchestration");

            await context.CallActivityAsync<string>(nameof(CreateUser)) ;
            await context.CallActivityAsync<string>(nameof(CreateOrder));
            await context.CallActivityAsync<string>(nameof(UpdatePayment));
        }

        [Function(nameof(CreateUser))]
        public async Task CreateUser([ActivityTrigger] FunctionContext context)
        {
            _logger.LogInformation("Creating User");
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
    }
}
