// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Newtonsoft.Json;
using System.Text.Json.Nodes;
namespace CallAutomationHero.Server
{
    /// <summary>
    /// Handling different callback events
    /// and perform operations
    /// </summary>

    public class IncomingCallHandler
    {
        private readonly CallAutomationClient _callAutomationClient;
        private readonly IConfiguration _configuration;

        public IncomingCallHandler(IConfiguration configuration)
        {
            _configuration = configuration;
            _callAutomationClient = new CallAutomationClient(_configuration["ConnectionString"]);
        }

        public async Task<IResult> HandleIncomingCall(EventGridEvent[] eventGridEvents)
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                Logger.LogInformation("Event " + JsonConvert.SerializeObject(eventGridEvent));

                // Handle system events
                if (eventGridEvent.TryGetSystemEventData(out object eventData))
                {
                    // Handle the subscription validation event.
                    if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                    {
                        var responseData = new SubscriptionValidationResponse
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };
                        return Results.Ok(responseData);
                    }
                }
                else
                {
                    Logger.LogInformation($"AnswerCall - {JsonNode.Parse(eventGridEvent.Data)}");
                    var jsonObject = JsonNode.Parse(eventGridEvent.Data)!.AsObject();
                    return await AnswerCall(jsonObject);
                }
            }
            return Results.Ok();
        }

        public async Task<IResult> HandleCallback(CloudEvent[] cloudEvents, string callerId)
        {
            CallConnection? callConnection = null;

            foreach (var cloudEvent in cloudEvents)
            {
                var @event = CallAutomationEventParser.Parse(cloudEvent);
                Logger.LogInformation($"Event received: {JsonConvert.SerializeObject(@event)}");

                if(callConnection == null)
                {
                    callConnection = _callAutomationClient.GetCallConnection(@event.CallConnectionId);
                }

                if (@event is CallConnected)
                {
                    //Start recognizing Dtmf
                    await RecognizeDtmf.StartRecognizingDtmf(callerId, _configuration, callConnection);
                }
                if (@event is RecognizeCompleted { OperationContext: "MainMenu" })
                {
                    //Perform operation as per DTMF tone recieved
                    var recognizeCompleted = (RecognizeCompleted)@event;
                    CollectTonesResult collectedTones = (CollectTonesResult)recognizeCompleted.RecognizeResult;

                    await PlayAudio.PlayAudioOperation(collectedTones.Tones[0], _configuration,
                       callConnection);
                }
                if (@event is RecognizeFailed { OperationContext: "MainMenu" })
                {
                    // play invalid audio
                    await PlayAudio.PlayAudioToAll(new PlayOptions() { Loop = false }, PlayAudio.PlayAudioMessages.InvalidAudio, 
                        _configuration,  callConnection);
                    _ = await callConnection.HangUpAsync(true);
                }
                if (@event is PlayCompleted { OperationContext: "SimpleIVR" })
                {
                    _ = await callConnection.HangUpAsync(true);
                }
                if (@event is PlayFailed { OperationContext: "SimpleIVR" })
                {
                    _ = await callConnection.HangUpAsync(true);
                }
                if(@event is AddParticipantSucceeded)
                {
                    Logger.LogInformation("Successfully added Agent participant");
                }
                if(@event is AddParticipantFailed)
                {
                    Logger.LogError("Failed to add Agent participant");
                    _ = await callConnection.HangUpAsync(true);
                }
            }
            return Results.Ok();
        }

        public async Task<IResult> AnswerCall(JsonObject jsonObject)
        {
            if (jsonObject != null && _callAutomationClient != null)
            {
                var callerId = jsonObject["from"]!["rawId"]!.ToString();
                var incomingCallContext = jsonObject["incomingCallContext"]!.ToString();
                var callbackUri = new Uri(_configuration["AppBaseUri"] + $"/api/calls/{Guid.NewGuid()}?callerId={callerId}");

                // Answer Call
                var response = await _callAutomationClient.AnswerCallAsync(incomingCallContext, callbackUri);
                Logger.LogInformation($"AnswerCallAsync Response -----> {response.GetRawResponse()}");

                return Results.Ok();
            }
            return Results.Problem("Answer Call failed.");

        }
    }
}
