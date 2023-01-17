// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

// import { DtmfTone } from '@azure/communication-calling';
import { Dialpad, FluentThemeProvider } from '@azure/communication-react';
import { Stack } from '@fluentui/react';
import React, { useState } from 'react';

export const CustomDialpadExample = (props) => {
  const [dtmftone, setDtmftone] = useState('');
  const [buttonValue, setButtonValue] = useState('');
  const [buttonIndex, setButtonIndex] = useState('');
  const [textFieldValue, setTextFieldValue] = useState('');

  const onSendDtmfTone = (dtmfTone) => {
      setDtmftone(dtmfTone);
      props.sendDtmf(dtmfTone);
    return Promise.resolve();
  };

  const onClickDialpadButton = (buttonValue, buttonIndex)=> {
    setButtonValue(buttonValue);
    setButtonIndex(buttonIndex.toString());
  };

  const onChange = (input) => {
    setTextFieldValue(input);
  };

  return (
    <FluentThemeProvider>
      <Stack>
        <div style={{ fontSize: '1.5rem', marginBottom: '1rem', fontFamily: 'Segoe UI' }}>DTMF Tone: {dtmftone}</div>
        <div style={{ fontSize: '1.5rem', marginBottom: '1rem', fontFamily: 'Segoe UI' }}>
          Button Clicked: {buttonValue} index at {buttonIndex}
        </div>

        <Dialpad
          onSendDtmfTone={onSendDtmfTone}
          textFieldValue={textFieldValue}
          onClickDialpadButton={onClickDialpadButton}
          onChange={onChange}
        />
       </Stack>
    </FluentThemeProvider> 
  );
};
export default Dialpad;