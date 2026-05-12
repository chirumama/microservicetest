import AppRoutes from "./routes/AppRoutes";
import { AuthProvider } from "./context/AuthContext";
import { AppProvider } from "./context/AppContext";
import { GatewayTokenProvider } from "./context/GatewayTokenContext";
export default function App() {
  return (
    <AuthProvider>
      <AppProvider>
        <GatewayTokenProvider>
<AppRoutes />
        </GatewayTokenProvider>
        
      </AppProvider>
    </AuthProvider>
  );
}