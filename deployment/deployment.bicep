@description('random unique string')
param unique_id string = uniqueString(resourceGroup().id)

@description('Web app name.')
@minLength(2)
param webAppName string = 'CallAutomation-${unique_id}'

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Describes plan\'s pricing tier and instance size. Check details at https://azure.microsoft.com/en-us/pricing/details/app-service/')
@allowed([
  'F1'
  'D1'
  'B1'
  'B2'
  'B3'
  'S1'
  'S2'
])
param sku string = 'F1'

@description('The language stack of the app.')
@allowed([
  '.net'
  'php'
  'node'
  'html'
])
param language string = '.net'

@description('Git Repo URL')
param repoUrl string = 'https://github.com/moirf/communication-services-callautomation-hero'

var appServicePlanName = 'Asp-${unique_id}'
var gitRepoUrl = repoUrl
var configReference = {
  '.net': {
    comments: '.Net app. No additional configuration needed.'
  }
  html: {
    comments: 'HTML app. No additional configuration needed.'
  }
  php: {
    phpVersion: '7.4'
  }
  node: {
    appSettings: [
      {
        name: 'WEBSITE_NODE_DEFAULT_VERSION'
        value: '12.15.0'
      }
    ]
  }
}

resource asp 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: sku
  }
}

resource webApp 'Microsoft.Web/sites@2022-03-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    siteConfig: union(configReference[language],{
      minTlsVersion: '1.2'
      scmMinTlsVersion: '1.2'
      ftpsState: 'FtpsOnly'
    })
    serverFarmId: asp.id
    httpsOnly: true
  }
}

resource gitsource 'Microsoft.Web/sites/sourcecontrols@2022-03-01' = {
  parent: webApp
  name: 'web'
  properties: {
    repoUrl: gitRepoUrl
    branch: 'main'
    isManualIntegration: true
  }
}

param acs_name string = 'acs-resource-${unique_id}'
param acs_location string = 'global'
resource acs_resource 'Microsoft.Communication/communicationServices@2020-08-20' = {
  name: acs_name
  location: acs_location
  tags:{}
  properties:{ dataLocation: 'United States' }
}
