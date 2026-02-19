# Build from repository `src` directory context.
# Example:
# docker build -f web/Fig.Web/Dockerfile -t fig.web ./src

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore "web/Fig.Web/Fig.Web.csproj"
RUN dotnet publish "web/Fig.Web/Fig.Web.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

COPY --from=build /app/publish/wwwroot .
COPY --from=build /src/web/Fig.Web/nginx.conf /etc/nginx/nginx.conf
COPY --from=build /src/web/Fig.Web/ssl.conf.template /etc/templates/ssl.conf.template
COPY --from=build /src/web/Fig.Web/ssl_ca.conf.template /etc/templates/ssl_ca.conf.template
COPY --from=build /src/web/Fig.Web/entrypoint.sh /entrypoint.sh

RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
