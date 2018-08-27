# VSTSFunctionApp
A Function App that talks to the VSTS API

If you are publishing this to Azure Functions, these are the app settings you need:

    "VstsClientId": "<clientId>", // update this with your Application ID in Azure AD
    
    // change to the URLs of your VSTS account - these must use HTTPS
    "VstsCollectionUrl": "https://account.visualstudio.com",
    "VstsAlmSearchUrl": "https://account.almsearch.visualstudio.com",
    
    "VstsUsername": "<AAD username in the form user@tenant.onmicrosoft.com or user@domain.com if you use custom domains>",
    "VstsFullUsername": "Firstname Lastname <user@tenant.onmicrosoft.com>" - you need this syntax for ALM search to work properly
    "VstsPassword": "<AAD password>",
    "VstsProject": "<projectName>",
    
    // This is the resource ID for the VSTS application - don't change this.
    "VstsResourceId": "499b84ac-1321-427f-aa17-267ca6975798",
    
    // The VSTS API version that you want to use - always check https://docs.microsoft.com/en-us/rest/api/vsts/
    "VstsApiVersion": "4.1"
    
If you are developing locally, put the above into local.settings.json.

Happy Kanban-ing!