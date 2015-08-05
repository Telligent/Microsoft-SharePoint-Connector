## SharePoint Connector Tests

This area will contain diagnostic tools and integration test for the SharePoint Connector.

### ConsoleUPS

The ConsoleUPS diagnostic tool will provide information about the User Profile Service application. The methods used in the ConsoleUPS are similar to the Connector's Profile Sync Plugin. This test can be helpful in troubleshooting issues with the Profile Sync.

The data returned in the console application is formatted using JSON syntax. This can be useful when using text editors capable of auto formatting this file type.

#### Setup

Unzip ConsoleUPS.zip from the run folder and update the ConsoleUPS.exe.config file:

- **UserName** This is the service account used to authenticate with SharePoint's User Profile Service.
- **Password** The password for the service account.
- **Domain** The active directory domain that the service account belongs to.
- **ChangeToken** Please ignore this setting. ConsoleUPS will update this setting internally.

#### Options

In the Windows command prompt change directories to the unzipped console application. Then run using the following options.

- **Change Directories** `c:> cd ConsoleUPS\`
- **Run the executable** `c:\ConsoleUPS>ConsoleUPS.exe` 
- **Output to a file** `c:\ConsoleUPS>ConsoleUPS.exe false > users.json` The 'false' argument will let the application know to end without the user's input.
- **Limit the number of user profiles** `c:\ConsoleUPS>ConsoleUPS.exe false 10 > users.json` The number 10 will let the application know to stop requesting user profiles from SharePoint when it reaches the limit.
- **User Account regex search** `c:\ConsoleUPS>ConsoleUPS.exe false 1 rudy.* > users.json` By adding a regex pattern the application will check the user profile's AccountName field. If there is a match then it will be grouped and returned. Please lookup regex patterns for more information. Simply typing a name without the special characters will also check for matches.
- **Incremental Sync ignore change token** `c:\ConsoleUPS>ConsoleUPS.exe false 1 . true > users.json` The 'true' argument at the end of the statement will tell the application to ignore the previously saved ChangeToken. This will request user profile changes stored in SharePoint's cache. If the ChangeToken is not ignored then the most recent returned token will be used.