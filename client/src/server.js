const express = require('express')

const CommunicationIdentityClient = require("@azure/communication-administration").CommunicationIdentityClient;
const config = require("./config.json");

const app = express()
const port = process.env.port || 8080;

app.get('/serverstatus', (req, res)=> res.send('server is running'))

app.post("/tokens/provisionUser", async (req, res) => {
  try {
    if (config?.connectionString?.indexOf("endpoint=") === -1) {
      throw new Error("Update `config.json` with connection string");
    }

    const communicationIdentityClient = new CommunicationIdentityClient(
      config.connectionString
    );
    let communicationUserId = await communicationIdentityClient.createUser();
    const tokenResponse = await communicationIdentityClient.issueToken(
      communicationUserId,
      ["voip"]
    );
    res.json(tokenResponse);
  } catch (error) {
    console.error(error);
  }
});

app.listen(port, ()=> console.log(`Server is listening on port ${port}!`))