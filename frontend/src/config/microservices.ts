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
  // {
  //   id: 2,
  //   name: "Pan Service",
  //   gatewayBaseUrl: "http://3.110.46.238:9000",
  //   healthPath: "/api/v1/pan/health", 
  //   endpoints: [
  //     {
  //       method: "POST",
  //       path: "/api/v1/pan/verify",
  //       description: "Verify PAN card details",
  //       defaultBody: `{\n  "id_number": "ABCDE1234F"\n}`,
  //     },
  //     {
  //       method: "GET",
  //       path: "/api/v1/pan/health",
  //       description: "Service health check",
  //     },
  //     {
  //       method: "GET",
  //       path: "/api/v1/pan/health/database",
  //       description: "Database health check",
  //     },
  //   ],
  // },

  {
    id: 1,
    name: "IP Lookup Service",
    gatewayBaseUrl: "http://3.110.46.238:9000",
    healthPath: "/v1/iplookup/health",   // no dedicated health, use a real lookup as  probe
    endpoints: [
      {
        method: "GET",
        path: "/v1/iplookup/{ip}",          // ← lowercase, matches APISIX route
        description: "Lookup IP — replace {ip} in the URL above before testing",
      },
    ],
  },
];