using Azure.Communication;
using Azure.Communication.CallAutomation;

namespace CallAutomationHero.Server
{
    public class PlayAudio
    {
        private readonly IConfiguration _configuration;
        private readonly CallConnection _callConnection;

        public enum PlayAudioType
        {
            MainMenuAudio,
            SalesAudio,
            MarketingAudio,
            CustomerCareAudio,
            AgentAudio,
            InvalidAudio
        }

        public PlayAudio(IConfiguration configuration, CallConnection callconnection)
        {
            _configuration = configuration;
            _callConnection = callconnection;
        }

        public async Task PlayAudioOperation(DtmfTone toneReceived)
        {
            var appBaseUri = _configuration["AppBaseUri"];
            var audioPlayOptions = new PlayOptions() { OperationContext = "SimpleIVR", Loop = false };

            if (toneReceived == DtmfTone.One)
            {
                await PlayAudioToAll(audioPlayOptions, PlayAudioType.SalesAudio);
            }
            else if (toneReceived == DtmfTone.Two)
            {
                await PlayAudioToAll(audioPlayOptions, PlayAudioType.MarketingAudio);
            }
            else if (toneReceived == DtmfTone.Three)
            {
                await PlayAudioToAll(audioPlayOptions, PlayAudioType.CustomerCareAudio);
            }
            else if (toneReceived == DtmfTone.Four)
            {
                audioPlayOptions.OperationContext = "AgentConnect";
                await PlayAudioToAll(audioPlayOptions, PlayAudioType.AgentAudio);

                var addParticipantOptions = new AddParticipantsOptions(new List<CommunicationIdentifier>()
                        {
                        new PhoneNumberIdentifier(_configuration["ParticipantToAdd"])
                        })
                {
                    SourceCallerId = new PhoneNumberIdentifier(_configuration["ACSAlternatePhoneNumber"])
                };

                _ = await _callConnection.AddParticipantsAsync(addParticipantOptions);
            }
            else if (toneReceived == DtmfTone.Five)
            {
                // Hangup for everyone
                _ = await _callConnection.HangUpAsync(true);
            }
            else
            {
                await PlayAudioToAll(audioPlayOptions, PlayAudioType.InvalidAudio);
            }
        }

        public async Task PlayAudioToAll(PlayOptions playAudioOptions, PlayAudioType audioType)
        {
            var appBaseUri = _configuration["AppBaseUri"];
            PlaySource salesAudio = new FileSource(new Uri(appBaseUri + _configuration[audioType.ToString()]));
            _ = await _callConnection.GetCallMedia().PlayToAllAsync(salesAudio, playAudioOptions);
        }
    }
}
