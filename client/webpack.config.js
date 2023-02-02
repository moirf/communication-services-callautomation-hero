const CommunicationIdentityClient =
  require("@azure/communication-administration").CommunicationIdentityClient;
const HtmlWebPackPlugin = require("html-webpack-plugin");
const connectionString = process.env.CONNECTION_STRING

const PORT = process.env.port || 8080;

module.exports = {
  devtool: "inline-source-map",
  mode: "production",
  entry: "./src/index.js",
  module: {
    rules: [
      {
        test: /\.(js|jsx)$/,
        exclude: /node_modules/,
        use: {
          loader: "babel-loader",
        },
      },
      {
        test: /\.html$/,
        use: [
          {
            loader: "html-loader",
          },
        ],
      },
      {
        test: /\.css$/,
        use: ["style-loader", "css-loader"],
      },
    ],
  },
  plugins: [
    new HtmlWebPackPlugin({
      template: "./public/index.html"
    }),
  ],
  devServer: {
    open: true,
    port: PORT,
    before: function (app) {
      app.post("/tokens/provisionUser", async (req, res) => {
        try {
          if (connectionString?.indexOf("endpoint=") === -1) {
            throw new Error("Update `env configuration` with connection string");
          }

          const communicationIdentityClient = new CommunicationIdentityClient(connectionString);
          let communicationUserId =
            await communicationIdentityClient.createUser();
          const tokenResponse = await communicationIdentityClient.issueToken(
            communicationUserId,
            ["voip"]
          );
          res.json(tokenResponse);
        } catch (error) {
          console.error(error);
        }
      });
    },
  },
};
