# img.birb.cc

img.birb.cc is a ShareX compatible C# image host, privately hosted by me

no, you cannot have an API key

## how to host yourself

- clone the repo

- build using `build.sh` for linux, or `build.bat` for windows

- run the executable alongside the `wwwroot` folder

- a default admin api key will be generated for you - use this to add new accounts from the admin page

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
