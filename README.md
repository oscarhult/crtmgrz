> [!TIP]
> ```
> docker run \
>   --detach \
>   --name crtmgrz \
>   --restart unless-stopped \
>   --publish 8080:8080/tcp \
>   --volume crtmgrz:/app/data \
>   ghcr.io/oscarhult/crtmgrz
> ```

![demo](./demo.gif)
