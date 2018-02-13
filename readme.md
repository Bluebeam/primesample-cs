# https://github.com/Bluebeam/primesample-cs.gitSession Roundtripper Sample

## Resources

- [API Documentation](https://studioapi.bluebeam.com/publicapi/swagger/ui/index)

## Workflow

The Session Roundtripper sample application demonstrates the following scenario:

1. Authorizes the app using Three-Legged OAuth/2
2. Upload a file to a Studio Project and then checks it out to a new Studio Session
    * User Chooses a Project from a dropdown
    * User Specifies a Session Name
    * User browses for a File
    * User Clicks Create
3. The backend application now does the following
    * Starts an upload to the Project which gets an AWS Upload URL
    * Uploads the file to AWS
    * Confirms the Upload in the Project
    * Creates a new Session
    * Checks out the file to the Session
4. Users add markups to the file while it is in a Session
5. User clicks 'Finish' button in application which then does the following
    * Sets the Session state to 'Finalizing' to kick everyone out of the Session
    * Kicks off a process to generate a Snapshot of the file with the markups
    * Waits for the snapshot to finish
    * Downloads the snapshot
    * Deletes the Session
    * Starts a checkin for the project file, getting an AWS Upload URL
    * Uploads the file to AWS
    * Confirms the project Checkin
    * Kicks off a job to flatten the file
    * Gets a share link for the project file

## Notes

In Step 5 above, an alternate approach could have been chosen. Instead of generating the snapshot, the file could have been checked in directly from the Session. However, the call to check in from Session only updates the project copy of the file leaving the file remaining in a checked out state. Furthermore, there is no convenient way to poll the status of the checkin. In this scenario the application would either have to poll the chat history looking for a specific chat message that identifies when the update is complete, or the application could poll the file history for an updated revision. Once the update is complete, the checkout could be undone on the file and the Session deleted as in the above workflow.

For the purpose of this sample the snapshot method is more straightforward with the caveat the extra bandwidth is consumed by downloading the file and then re-uploading it.

## Details

### Configuration

Configuration of the client id, secret, and callback urls are handled by using either a config file or using environment variables. To use a config file, place the following as config.json in the root of the project:

Configuration of the client id and secret are setup using the Secrets Manager.  Perform the following commands on the command line in the project folder:

```
dotnet user-secrets set ClientID <client_id>
dotnet user-secrets set ClientSecret <client_secret>

```

The secret is used for encrypting the session cookie.

The callback URL is specified in the BluebeamOAuth.cs (BluebeamAuthenticationDefaults class) file on line 46:
```
public const string CallbackPath = "/callback";
```

### Authentication

This app uses a custom middleware to handle the refresh tokens updating. For every request the middleware will check if the tokens have expired and perform a token refresh if neeeded.  All other Authentication handling is done by the .NET Core Frameworks.  The BluebeamOAuth.cs file contains some customizations to handle the specifics of interacting with Bluebeam's Auth server.


### Database

Persistent storage is necessary in order to save tokens. SQLite is used for this project, since its one of the provided persistence providers and its relatively simple to setup and use. As a production system, an external database would be required.

