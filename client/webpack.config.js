const CommunicationIdentityClient =
  require("@azure/communication-identity").CommunicationIdentityClient;
const HtmlWebPackPlugin = require("html-webpack-plugin");
const BundleAnalyzerPlugin =
  require("webpack-bundle-analyzer").BundleAnalyzerPlugin;
const config = require("./config.json");

const PORT = process.env.port || 8080;
const CONNECTION_STRING =
  process.env.CONNECTION_STRING || "<put connection string for local run>";
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
          let token = await client.createUserAndToken(["voip"]);
          res.json(token);
        } catch (error) {
          console.error(error);
        }
      });
    },
  },
};
