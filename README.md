
![](https://raw.githubusercontent.com/SparkleNetworks/LinkedInNET/master/src/LiNET-200.png)

LinkedInNET
===========

Have any question? You may reach the authors on the dedicated chat room: [![Join the chat at https://gitter.im/SparkleNetworks/LinkedInNET](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/SparkleNetworks/LinkedInNET?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

Motivation
------------

Bring the .NET world a nice LinkedIn client library. + support both V1 and V2 APIs.


Before you start - About LinkedIn V1 API recent changes
----------------------------------------------------------------


By using the LinkedIn APIs you agree to the [LinkedIn APIs Terms of Use](https://developer.linkedin.com/documents/linkedin-apis-terms-use).  
This project is released under the LGPL v3 license.  
This is NOT an official client library.

Usage
------------

### 1. Installation

Build the sources... You have to create your own .snk file.

Supported frameworks: 3.5 (sync), 4.0 (sync), 4.5 (sync and task async).

### 2. Create API client with configuration

The `LinkedInApi` class is the entry point for all API calls. You must instantiate it with a configuration object. The minimum configuration is the API key and secret.  [Get a LinkedIn API key](https://www.linkedin.com/secure/developer).

````csharp
// create a configuration object
var config = new LinkedInApiConfiguration("•api•key•••", "•api•secret•key••••••");

// get the APIs client
var api = new LinkedInApi(config);
````

### 3. Create OAuth2 authorize url

The OAuth2 authentication process is fully supported. The `GetAuthorizationUrl` method will generate the OAuth2 url to navigate the user to.

````csharp
var scope = AuthorizationScope.ReadBasicProfile | AuthorizationScope.ReadEmailAddress;
var state = Guid.NewGuid().ToString();
var redirectUrl = "http://mywebsite/LinkedIn/OAuth2";
var url = api.OAuth2.GetAuthorizationUrl(scope, state, redirectUrl);
// https://www.linkedin.com/uas/oauth2/authorization?response_type=code&client_id=...
// now redirect your user there
````

### 4. Get access token

When the user is redirected back to your website, you can get an access code.

````csharp
// http://mywebsite/LinkedIn/OAuth2?code=...&state=...
public async Task<ActionResult> OAuth2(string code, string state, string error, string error_description)
{
    if (!string.IsNullOrEmpty(error) || !string.IsNullOrEmpty(error_description))
    {
        // handle error and error_description
    }
    else
    {
        var redirectUrl = "http://mywebsite/LinkedIn/OAuth2";
        var userToken = await api.OAuth2.GetAccessTokenAsync(code, redirectUrl);
        // keep this token for your API requests
    }

    // ...
}
````

You will find in the source codes a nicer way to build the redirect url.

````csharp
var redirectUrl = this.Request.Compose() + this.Url.Action("OAuth2");
````


### 5. Example call: fetch user profile

````csharp
var user = new UserAuthorization(userToken.AccessToken);
var profile = api.Profiles.GetMyProfile(user);
````

Yes, you have to pass the token for each call. This might seem redundant for some but we prefer stateless objects for multi-threaded contexts. 

### 6. Field selectors

The API uses [field lists] to fetch the desired data. Simple extension methods will allow you to make strongly-typed field selection.

````csharp
var profile = api.Profiles.GetMyProfile(
    user,
    FieldSelector.For<Person>().WithFirstname().WithLastname().WithLocationName());
````

The `.WithAllFields()` method will generate the list of all available fields. It is not recommended to do that.

````csharp
var profile = api.Profiles.GetMyProfile(
    user,
    FieldSelector.For<Person>().WithAllFields());
// https://docs.microsoft.com/en-us/linkedin/shared/integrations/people/profile-api available fields here)
// however it is not recommended to specify all fields
````

You can create your own extension methods when you desire many fields. Check the source code to see how it works.

### 7. Errors

API error results throw `LinkedInApiException`s. You can find extra info in the Data collection.

````csharp
try
{
    var profile = this.api.Profiles.GetMyProfile(user);
}
catch (LinkedInApiException ex) // one exception type to handle
{
    // ex.Message
    // ex.InnerException // WebException
    // ex.Data["ResponseStream"]
    // ex.Data["HttpStatusCode"]
    // ex.Data["Method"]
    // ex.Data["UrlPath"]
    // ex.Data["ResponseText"]
}
////catch (Exception ex) { } //  bad, don't do that

````

Library internal errors throw `LinkedInNetException`s. You should not catch them as they do not represent a normal behavior. This may be usefull when waiting for a fix.

You should not catch `WebException`s as they are wrapped into `LinkedInApiException`s.

### 8. Explore

Code documentation is quite present. Use the auto-completion to discover stuff.

The MVC demo app has a /Explore page that demonstrates most API calls. Have a look at it.

### 9. Raw queries

A method is missing in the library? You may contribute to create it... Or you may use the raw query methods `RawGetJsonQuery`, `RawPostJsonQuery` or `RawQuery`.

This example shows how to fetch a company share.

````csharp
string shareAsJson = this.api.RawGetJsonQuery("/v1/companies/" + CompanyId + "/updates/key=" + Uri.EscapeDataString(ShareId) + "?format=json", user);
````

More details [here](https://github.com/SparkleNetworks/LinkedInNET/blob/dffaea840eac044654c7cee59df50a8db6f101a3/src/NET45.Sparkle.LinkedInNET.DemoMvc5/Controllers/ExploreController.cs#L363).

Contribute
------------

We welcome contributions. Especially if you can **suggest/access/test methods with a partner API key**.

We are generating code based on a [XML file](DefinitionFile.md).  
This XML file is manually filled to represent the API.  
The API coverage should be implemented by modifying the XML file and enhancing code generation.

To generate the API code, build the "ServiceDefinition" project in Debug mode, edit `LinkedInApi.xml`, then use "Run custom tool" on the `Service.tt` file. The XML file will be read and most of the code will be updated automagically. 
  
To alter code generation, search for `CSharpGenerator.cs`. Different methods are responsible of generating different parts of C# code (return types, api groups, selectors).
  
To add/alter API methods and return types, search for `LinkedInApi.xml`. This file [describes the API in a human-readable and machine-readable way](DefinitionFile.md). Don't forget to re-generate the code (Service.tt).


References
------------

https://docs.microsoft.com/en-us/linkedin/compliance/  


.NET versions
------------

Supported .NET Framework versions:

* .NET 4.5 (dependencies: Newtonsoft.Json ≥ 6.0.8, Microsoft.Net.Http ≥ 2.2.29)
* .NET 4.0 (dependencies: Newtonsoft.Json ≥ 6.0.8)
* .NET 3.5 (dependencies: Newtonsoft.Json ≥ 6.0.8)

We are using a lot of code generation so it won't be difficult to target any other framework. 


Status
------------

Because of the API policy changes, most API calls are now reserved to the partners LinkedIn chose. The core team will try to keep up using basic API key.

Check our [internal to-do list](src/ToDoV2.md) to track missing/done things.
