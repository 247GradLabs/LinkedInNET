
API definition
---------------------------------

What's implemented and what's not...

Check out the README file to understand how you can contribute to these tasks.

LinkedIn api v2 documentation: https://docs.microsoft.com/en-us/linkedin/compliance/

### Basis




### Profiles
  



### Companies




### Groups



#### Group Memberships for a User



####  Retrieving a Group's Discussion Posts



####  Creating a Group Discussion Post



####  Interacting with a Discussion Post



####  Interacting with Comments



####  Suggested Groups for a LinkedIn Member



### Jobs



#### Search



#### Posting



### Share and Social Stream



### Communications


Code generation
---------------------------------

- [x] Setup .tt file and generation class 
- [x] Generate API groups 
- [x] Generate API group
- [x] Generate basic return types 
- [x] Generate basic methods
- [x] Support 1 level of field selectors `/v1/people/id=12345:(first-name,last-name)`
- [x] Support all levels of field selectors
- [x] Collections of ResultType  
- [x] Fix generation of sub-fields (ex: location:(name))  
- [ ] Support entity selectors `/v1/people::(~,id=123456,id=456789):(first-name)` ([ref](https://developer.linkedin.com/documents/field-selectors)) 
    - [ ] API Definition syntax
    - [ ] Collection of selectors
    - [ ] Generate API method

API other items
---------------------------------

- [x] Profile: Some members have profiles in multiple languages. To specify the language you prefer, pass an Accept-Language HTTP header.
- [x] Pagination: start=0, count=500
- [ ] `/v1/companies?is-company-admin=true`

For developers
---------------------------------

- [x] Success HTTP codes: 200, 201
- [x] Error HTTP codes: 400, 401, 403, 404, 500
- [x] async/await pattern Windows Phone 8ish and RT
- [ ] Async pattern by callbacks for Silverlight and Windows Phone 7ish
- [x] Nuget package
- [ ] Full exception documentation to ensure your apps won't crash on unexpected exception types
- [ ] WinRT build


