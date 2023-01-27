# img.birb.cc

img.birb.cc is a ShareX compatible C# image host, privately hosted by me.

no, you cannot have an API key.

## how to host yourself

- clone the repo.

- run `dotnet run`

- a default admin api key will be generated. Use this to add new accounts.

## valid endpoints

```
POST /api/upload

POST /api/usr/new                  // admin only

POST /api/usr/settings

POST /api/usr

POST /api/users

GET /api/dashmsg

GET /api/stats

POST /api/img

DELETE /api/delete/{hash}

DELETE /api/nuke
```
