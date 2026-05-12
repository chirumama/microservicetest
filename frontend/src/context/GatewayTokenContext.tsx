// src/context/GatewayTokenContext.tsx
import { createContext, useContext, useState, type ReactNode } from "react";

interface GatewayTokenContextType {
  gatewayToken: string | null;
  setGatewayToken: (token: string | null) => void;
}

const GatewayTokenContext = createContext<GatewayTokenContextType | undefined>(undefined);

export function GatewayTokenProvider({ children }: { children: ReactNode }) {
  // Persist across page refreshes
  const [gatewayToken, setGatewayTokenState] = useState<string | null>(
    () => localStorage.getItem("gatewayToken")
  );

  function setGatewayToken(token: string | null) {
    if (token) localStorage.setItem("gatewayToken", token);
    else localStorage.removeItem("gatewayToken");
    setGatewayTokenState(token);
  }

  return (
    <GatewayTokenContext.Provider value={{ gatewayToken, setGatewayToken }}>
      {children}
    </GatewayTokenContext.Provider>
  );
}

export function useGatewayToken() {
  const ctx = useContext(GatewayTokenContext);
  if (!ctx) throw new Error("useGatewayToken must be used within GatewayTokenProvider");
  return ctx;
}