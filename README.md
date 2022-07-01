# img.birb.cc

C# image host.

no, you cannot have an API key.

## how to host yourself

- clone the repo OR download the published build. 
- install NGINX and dotnet.
- use this config in nginx

```
        location / {
                try_files $uri $uri/ =404;
        }

        location /api/ {

                add_header 'Access-Control-Allow-Origin' '*' always;

                proxy_pass      https://127.0.0.1:5001;
                proxy_http_version      1.1;
                proxy_set_header        Upgrade $http_upgrade;
                proxy_set_header        Connection keep-alive;
                proxy_set_header        Host $host;
                proxy_cache_bypass      $http_upgrade;
                proxy_set_header        X-Forwarded-For $proxy_add_x_forwarded_for;
                proxy_set_header        X-Forwarded-Proto $scheme;
        }
}
```

- run the .dll
- a default admin api key will be generated. Use this to add new accounts.

## valid endpoints

```
POST /api/upload                (upload a file)

POST /api/usr/new               // admin only (create a new user)

POST /api/usr/domain            (change the domain the file link will display)

POST /api/usr                   (get your stats)

POST /api/users                 (get information on all users)

GET /api/stats                  (get stats of the site)

POST /api/img                   (get hashes of all files you have uploaded)

DELETE /api/delete/{hash}       (delete a file you have uploaded)

DELETE /api/nuke                (delete all files you have uploaded)
```
