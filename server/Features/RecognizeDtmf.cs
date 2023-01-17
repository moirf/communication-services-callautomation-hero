using Azure.Communication.CallAutomation;
using Azure.Communication;

namespace CallAutomationHero.Server
{
    public class RecognizeDtmf
    {
        IConfiguration _configuration;
        CallConnection callConnection;

        public RecognizeDtmf(IConfiguration configuration, CallConnection callConnection)
        {
            _configuration = configuration;
            this.callConnection = callConnection;
        }

        public async Task StartRecognizingDtmf(string callerId)
        {
            var appBaseUri = _configuration["AppBaseUri"];
            // Start recognize prompt - play audio and recognize 1-digit DTMF input
            var recognizeOptions =
                new CallMediaRecognizeDtmfOptions(CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 1)
                {
                    InterruptPrompt = true,
                    InterToneTimeout = TimeSpan.FromSeconds(10),
                    InitialSilenceTimeout = TimeSpan.FromSeconds(5),
                    Prompt = new FileSource(new Uri(appBaseUri + _configuration[PlayAudio.PlayAudioType.MainMenuAudio.ToString()])),
                    OperationContext = "MainMenu"
                };
            _ = await callConnection.GetCallMedia().StartRecognizingAsync(recognizeOptions);
        }
    }
}
