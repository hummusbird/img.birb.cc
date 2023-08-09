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

## web server

make sure you set up a web server to forward :5000 to :443, and check file uploads.

example NGINX config:

```
server {
        listen 443 ssl http2;
        listen [::]:443 ssl http2;

        server_name YOUR_DOMAIN_HERE;

        location / {
                add_header 'Access-Control-Allow-Origin' '*' always;

                proxy_pass              http://localhost:5000;
                proxy_http_version      1.1;
                proxy_set_header        Upgrade $http_upgrade;
                proxy_set_header        Connection keep-alive;
                proxy_set_header        Host $host;
                proxy_cache_bypass      $http_upgrade;
                proxy_set_header        X-Forwarded-For $proxy_add_x_forwarded_for;
                proxy_set_header        X-Forwarded-Proto $scheme;
        }

  ssl_certificate PATH_TO_CERT;
  ssl_certificate_key PATH_TO_KEY;
}
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
