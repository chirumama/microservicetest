// AppContext is no longer used for local state — apps are fetched from the API.
// Kept for backward compatibility in case it's imported anywhere.
import { createContext, useContext, type ReactNode } from "react";

type AppContextType = Record<string, never>;
export const AppContext = createContext<AppContextType | undefined>(undefined);

export function AppProvider({ children }: { children: ReactNode }) {
  return <AppContext.Provider value={{}}>{children}</AppContext.Provider>;
}

export function useApp() {
  const context = useContext(AppContext);
  if (context === undefined) throw new Error("useApp must be used within an AppProvider");
  return context;
}
