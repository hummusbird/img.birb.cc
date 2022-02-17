# img.birb.cc

C# image host.

no, you cannot get an API key.

## how to host yourself

- fork the repo. 
- modify Program.cs and change line 181 to your domain.
- install NGINX and dotnet.
- use this config in nginx
```
server {
        listen          443 ssl http2;
        listen          [::]:443 ssl http2;
        server_name     YOURDOMAIN;
        ssl             on;
        ssl_certificate /etc/ssl/certs/YOURCERT;
        ssl_certificate_key     /etc/ssl/private/YOURKEY;

        add_header X-Frame-Options DENY;
        add_header X-Content-Type-Options nosniff;

        root **POINT TO IMG FOLDER**;
        index index.html;

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

- change server_name, root and cert lines to your domain.
- build the release using dotnet, then run the dll
- modify user.json and create urself an admin user
