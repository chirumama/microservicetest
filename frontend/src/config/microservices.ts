// src/config/microservices.ts

export interface EndpointConfig {
  method: "GET" | "POST" | "PUT" | "DELETE";
  path: string;          // display path e.g. /api/passport/verify
  description?: string;

  defaultBody?: string;  // JSON string for POST/PUT
}

export interface MicroserviceConfig {
  id: number;
  name: string;
  gatewayBaseUrl: string;  // e.g. http://localhost:9080
  healthPath: string;
  endpoints: EndpointConfig[];
}

export const MICROSERVICES: MicroserviceConfig[] = [
  {
    id: 1,
    name: "Pan Service",
    gatewayBaseUrl: "http://localhost:9080",
    healthPath: "/api/v1/pan/health",
    endpoints: [
      {
        method: "POST",
        path: "/api/v1/pan/verify",
        description: "Verify PAN card details",
        defaultBody: `{\n  "id_number": "ABCDE1234F"\n}`,
      },
      {
        method: "GET",
        path: "/api/v1/pan/health",
        description: "Service health check",
      },
      {
        method: "GET",
        path: "/api/v1/pan/health/database",
        description: "Database health check",
      },
    ],
  },
  {
    id: 2,
    name: "Passport Service",
    gatewayBaseUrl: "http://localhost:9080",
    healthPath: "/api/passport/health",
    endpoints: [
      {
        method: "POST",
        path: "/api/passport/verify",
        description: "Verify passport details",
        defaultBody: `{\n  "file_number": "BO1065733511221",\n  "date_of_birth": "2000-12-29",\n  "consent": "Y"\n}`,
      },
      {
        method: "GET",
        path: "/api/passport/health",
        description: "Service health check",
      },
      {
        method: "GET",
        path: "/api/passport/health/db",
        description: "Database health check",
      },
    ],
  },
  {
    id: 3,
    name: "GST Service",
    gatewayBaseUrl: "http://localhost:9080",
    healthPath: "/api/gst/health",
    endpoints: [
      {
        method: "POST",
        path: "/api/gst/verify",
        description: "Verify GST number",
        defaultBody: `{\n  "gstin": "22AAAAA0000A1Z5"\n}`,
      },
      {
        method: "GET",
        path: "/api/gst/health",
        description: "Service health check",
      },
    ],
  },
  {
    id: 4,
    name: "IP Lookup Service",
    gatewayBaseUrl: "http://localhost:9080",
    healthPath: "/v1/iplookup/8.8.8.8",   // no dedicated health, use a real lookup as probe
    endpoints: [
      {
        method: "GET",
        path: "/v1/iplookup/{ip}",          // ← lowercase, matches APISIX route
        description: "Lookup IP — replace {ip} in the URL above before testing",
      },
    ],
  },
];