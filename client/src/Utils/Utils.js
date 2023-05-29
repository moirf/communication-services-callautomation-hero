import {
    isCommunicationUserIdentifier,
    isPhoneNumberIdentifier,
    isMicrosoftTeamsUserIdentifier,
    isUnknownIdentifier
} from '@azure/communication-common';
const config = require("../../config.json");

export const utils = {
    getAppServiceUrl: () => {
        return window.location.origin;
    },
    provisionNewUser: async (userId) => {
        let response = await fetch(config.serverURL +'/tokens/provisionUser', {
            method: 'POST',
            body: { userId },
            headers: {
                'Accept': 'application/json, text/plain, */*',
                'Content-Type': 'application/json'
            },
        });

        if (response.ok) {
            return response.json();
        }

        throw new Error('Invalid token response');
    },
    getIdentifierText: (identifier) => {
        if (isCommunicationUserIdentifier(identifier)) {
            return identifier.communicationUserId;
        } else if (isPhoneNumberIdentifier(identifier)) {
            return identifier.phoneNumber;
        } else if (isMicrosoftTeamsUserIdentifier(identifier)) {
            return identifier.microsoftTeamsUserId;
        } else if (isUnknownIdentifier(identifier) && identifier.id === '8:echo123'){
            return 'Echo Bot';
        } else {
            return 'Unknown Identifier';
        }
    },
    getSizeInBytes(str) {
        return new Blob([str]).size;
    },
    getRemoteParticipantObjFromIdentifier(call, identifier) {
        switch(identifier.kind) {
            case 'communicationUser': {
                return call.remoteParticipants.find(rm => {
                    return rm.identifier.communicationUserId === identifier.communicationUserId
                });
            }
            case 'microsoftTeamsUser': {
                return call.remoteParticipants.find(rm => {
                    return rm.identifier.microsoftTeamsUserId === identifier.microsoftTeamsUserId
                });
            }
            case 'phoneNumber': {
                return call.remoteParticipants.find(rm => {
                    return rm.identifier.phoneNumber === identifier.phoneNumber
                });
            }
            case 'unknown': {
                return call.remoteParticipants.find(rm => {
                    return rm.identifier.id === identifier.id
                });
            }
        }
    },
    startRecording: async (id) => {
        try {
          const response = await fetch(config.serverURL + '/startRecording?serverCallId=' + id);
          if (response.ok) {
            const recordingid = await response.json();
            return { recordingId: recordingid, message: '' };
          }
          const output = await response.json();
          const errorMessage = output.message || 'Recording could not be started';
          return { recordingId: '', message: errorMessage };
        } catch (e) {
          return { recordingId: '', message: 'Recording could not be started' };
        }
      },
      stopRecording: async (recordingId) => {
        try {
          const response = await fetch(config.serverURL +
            '/stopRecording?recordingId=' + recordingId
          );
          if (response.ok) {
            return { message: '' };
          }
          return { message: 'Recording could not be stopped' };
        } catch (e) {
          return { message: 'Recording could not be stopped' };
        }
      },
}
