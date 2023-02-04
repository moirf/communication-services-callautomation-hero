const CommunicationIdentityClient = require("@azure/communication-identity");
const HtmlWebPackPlugin = require("html-webpack-plugin");
const BundleAnalyzerPlugin =
  require("webpack-bundle-analyzer").BundleAnalyzerPlugin;
const config = require("./config.json");

const PORT = process.env.port || 8080;
const CONNECTION_STRING =
  process.env.CONNECTION_STRING ||
  "endpoint=https://acs-app-validations.communication.azure.com/;accesskey=YHuzUTXJtHAuvglEyy0yf97vGEhZJ0rQ3QGSdMioovuohQcTeeUVHx3in0XNFot816J2+eIlxzwdw8VXlsqrwQ==";
module.exports = {
  devtool: "inline-source-map",
  mode: "development",
  entry: "./src/index.js",
  output: {
    filename: "main.bundle.js",
  },
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
      template: "./public/index.html",
    }),
    new BundleAnalyzerPlugin(),
  ],
  devServer: {
    open: true,
    port: PORT,
    before: function (app) {
      app.post("/tokens/provisionUser", async (req, res) => {
        try {
          const client = new CommunicationIdentityClient(CONNECTION_STRING);
          let userId = await client.createUser();
          const tokenResponse = await client.issueToken(userId, ["voip"]);
          res.json(tokenResponse);
        } catch (error) {
          console.error(error);
        }
      });
    },
  },
};
