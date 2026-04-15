import { createContext, useContext, useState, useEffect, type ReactNode } from "react";

export type Role = "SuperAdmin" | "Admin" | "User";

export type UserType = {
  userId: number;
  roleId: number;
  email:  string;
  role:   Role;
};

type AuthContextType = {
  user:      UserType | null;
  login:     (userId: number, roleId: number, role: string, email: string) => void;
  logout:    () => void;
  isLoading: boolean;
};

export const AuthContext = createContext<AuthContextType | undefined>(undefined);

function parseRole(role: string): Role {
  switch (role) {
    case "SuperAdmin": return "SuperAdmin";
    case "Admin":      return "Admin";
    default:           return "User";
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser]         = useState<UserType | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    // Restore session from localStorage on page refresh
    const userId = localStorage.getItem("userId");
    const roleId = localStorage.getItem("roleId");
    const role   = localStorage.getItem("role");
    const email  = localStorage.getItem("email");

    if (userId && roleId && role && email) {
      setUser({
        userId: parseInt(userId),
        roleId: parseInt(roleId),
        role:   parseRole(role),
        email,
      });
    }
    setIsLoading(false);
  }, []);

  const login = (userId: number, roleId: number, role: string, email: string): void => {
    const parsedRole = parseRole(role);

    // Store userId and roleId — these are sent as X-User-Id and X-User-Role headers
    // No JWT token stored anywhere
    localStorage.setItem("userId", String(userId));
    localStorage.setItem("roleId", String(roleId));
    localStorage.setItem("role",   parsedRole);
    localStorage.setItem("email",  email);

    setUser({ userId, roleId, role: parsedRole, email });
  };

  const logout = (): void => {
    localStorage.removeItem("userId");
    localStorage.removeItem("roleId");
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