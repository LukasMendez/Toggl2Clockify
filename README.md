# Toggl2Clockify

## Introduction

Toggl2Clockify is a worker service that can import time reports from Toggl and export them to Clockify.

The service is supposed to run in the background and execute automatically in a specified time interval defined by the user. The purpose of the service is to make it easier for developers or other users that need to track their time spent on tasks, and who prefer using Toggl Track but work for a company that requires time registration to be done in Clockify. 

Using this tool you will be able to use Toogl Track normally and not have to worry about the hours not being added to Clockify. 

## Get started

In order to start exporting time entries tracked by Toggle to Clockify, you will need to configure your `appsettings.json` 

Also make sure that you have the `history.json` file that contains execution history data required when you want the service to run continuously. 

The following shows the required structure of your `appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Toggl": {
    "ApiKey": "<YOUR-API-KEY>"
  },
  "Clockify": {
    "ApiKey": "<YOUR-API-KEY>",
    "WorkspaceId": "<YOUR-WORKSPACE-ID>",
    "UserId": "<YOUR-USER-ID>"
  },
  "Toggl2Clockify": {
    "ProjectMappings": [
      {
        "SourceProjectIds": [ "<YOUR-FIRST-TOGGL-PROJECT-ID>", "<YOUR-SECOND-TOGGL-PROJECT-ID>" ],
        "DestinationProjectId": "<YOUR-CLOCKIFY-DESTINATION-PROJECT-ID>"
      }
    ]
  },
  "GeneralConfiguration": {
    "DefaultDaysSinceNow": 14,
    "ShouldRoundTime": true,
    "RunIntervalInHours": 24
  }
}

```

#### 

The code is configured to support multiple project mappings, meaning that you can have as many Clockify projects as you want, that all import time entries from as many different Toggl projects as you like. Each configuration per Clockify project must be defined in `Toggl2Clockify:ProjectMappings` in `appsettings.json`, which takes an array of this object type. 

### Toggl API

1) ApiKey

To find your API key for Toggl go to your Control Panel and click on your name in the left corner at the bottom, click on "Profile settings" and then scroll all the way to the bottom where you will see a box called "API Token". Here you'll be able to reveal/generate an API key. 

2. SourceProjectIds

Your source project ID's are the ID's of the projects with tracked time entries, that you want to concatenate and export to Clockify. To find a project ID for a Toggl project go to your Control Panel, click on "Projects", select one of your projects, and then in the URL you will be able to see the ID right next to *../projects/* in the path. The property supports an array of <u>**numbers only**</u>.  

### Clockify

1. ApiKey

To find your API key for Clockify go to your Control Panel and click on your name in the upper right corner, click on "Profile settings" and then scroll all the way to the bottom where you will see a box called "API". Here you'll be able to reveal/generate an API key. 

2. WorkspaceId

To find the workspace ID, log in to Clockify, go to workspace settings, and copy the unique 24-char part that's in the URL.

If your workspace is managed by an organization, you may be unable to access the workspace settings and will have to extract the workspace ID by either inspecting your requests to the server via the browser or manually making a GET request to [https://api.clockify.me/api/v1/workspaces](https://api.clockify.me/api/v1/workspaces) including an **X-Api-Key** header where you set the value to the API key you generated earlier. This will return a JSON with a list of all your workspaces including their name and ID. 

3. UserId

To find the user ID, the easiest way is to make a GET request to the following endpoint https://api.clockify.me/api/v1/user
Remember to include a X-Api-Key header with your API key here as well. 

The userId will be the value of the property: "id" 

###  General Configuration

1) DefaultDaysSinceNow

Whenever the service runs, it retrieves a time entry history for a specific time period. The service will always start by looking in `history.json` and use the last execution date as start date. If nothing is defined, it will need to know how many days it should look back, and use that date as start date. This can be changed to any value desired by the user. 

2. ShouldRoundTime

To avoid funny and too precise numbers (not every project leader wants that) you can set **ShouldRoundTime** to true, which will make sure that for example 9,47 hours is rounded up to 10 hours. 

3. RunIntervalHours

To specify how often you want the service should run you can set the amount of hours, that the service should wait before executing again. The number you provide must be the amount of hours. 

## Publish

To publish the code and start using it, you can follow this guide: https://docs.microsoft.com/en-us/dotnet/core/extensions/windows-service
