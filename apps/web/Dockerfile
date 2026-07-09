# Build stage
FROM node:20-alpine AS build

WORKDIR /build

COPY apps/web/package*.json ./

RUN npm ci

COPY apps/web .

RUN npm run build

# Runtime stage
FROM nginx:alpine

COPY --from=build /build/dist/apps/web /usr/share/nginx/html

COPY apps/web/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
