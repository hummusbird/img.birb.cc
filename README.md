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

POST /api/upload

POST /api/usr                   // admin only

POST /api/usr/domain

GET /api/usr

GET /api/stats

GET /api/img                    // admin only

DELETE /api/delete/{hash}

```