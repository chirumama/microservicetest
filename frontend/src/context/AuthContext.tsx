import { createContext, useContext, useState, useEffect, type ReactNode } from "react";

export type Role = "SuperAdmin" | "Admin" | "User";

export type UserType = {
  email: string;
  role: Role;
  token: string;
};

type AuthContextType = {
  user: UserType | null;
  login: (token: string, role: string, email: string) => void;
  logout: () => void;
  isLoading: boolean;
};

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

function parseRole(role: string): Role {
  // Backend returns "SuperAdmin", "Admin", "User" as strings
  switch (role) {
    case "SuperAdmin": return "SuperAdmin";
    case "Admin":      return "Admin";
    default:           return "User";
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserType | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const token = localStorage.getItem("token");
    const role  = localStorage.getItem("role");
    const email = localStorage.getItem("email");
    if (token && role && email) {
      setUser({ token, role: parseRole(role), email });
    }
    setIsLoading(false);
  }, []);

  const login = (token: string, role: string, email: string): void => {
    const parsedRole = parseRole(role);
    localStorage.setItem("token", token);
    localStorage.setItem("role",  parsedRole);
    localStorage.setItem("email", email);
    setUser({ token, role: parsedRole, email });
  };

  const logout = (): void => {
    localStorage.removeItem("token");
    localStorage.removeItem("role");
    localStorage.removeItem("email");
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, login, logout, isLoading }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used within AuthProvider");
  return context;
}
