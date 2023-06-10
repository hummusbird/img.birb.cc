# img.birb.cc

img.birb.cc is a ShareX compatible C# image host, privately hosted by me

no, you cannot have an API key

## how to host yourself

- download the latest release

- run the executable alongside the `wwwroot` folder

- a default admin api key will be generated for you - use this to add new accounts from the admin page

you may also build the program yourself using `build.sh` or `build.bat`

## config file

the first time you run the program, a `config.json` file is generated

this file contains settings and preferences for your image host:

```
DefaultDomain: set this to the default domain of your website 

UserDBPath: path to user json file

FileDBPath: path to file json file

AlbumDBPath: path to album json file

LoggingEnabled: disables / enables logging

AllowedFileTypes: an array of magic headers for allowed filetypes. 
see https://en.wikipedia.org/wiki/List_of_file_signatures
```

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

GET /album/{hash}

POST /album/{hash}/images

POST /album/{hash}/info

POST /api/album/add

DELETE /api/album/delete

DELETE /api/delete/{hash}

DELETE /api/nuke
```
