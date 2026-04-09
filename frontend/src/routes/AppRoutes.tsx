import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Login from "../pages/auth/Login";
import Dashboard from "../pages/dashboard/Dashboard";
import CreateApplication from "../pages/applications/CreateApplication";
import ManageApplications from "../pages/applications/ManageApplications";
import ApplicationDetails from "../pages/applications/ApplicationDetails";
import SuperAdminDashboard from "../pages/dashboard/SuperAdminDashboard";
import { useAuth } from "../context/AuthContext";


function ProtectedRoute({ children, allowedRoles }: { children: React.ReactElement; allowedRoles?: string[] }) {
  const { user, isLoading } = useAuth();
  if (isLoading) return <div className="d-flex vh-100 justify-content-center align-items-center text-muted">Loading...</div>;
  if (!user) return <Navigate to="/" replace />;
  if (allowedRoles && !allowedRoles.includes(user.role)) return <Navigate to="/dashboard" replace />;
  return children;
}

export default function AppRoutes(){
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Login />} />
        <Route path="/dashboard" element={<ProtectedRoute><Dashboard /></ProtectedRoute>} />
        <Route path="/superadmin-dashboard" element={<ProtectedRoute allowedRoles={["SuperAdmin"]}><SuperAdminDashboard /></ProtectedRoute>} />
        <Route path="/create" element={<ProtectedRoute><CreateApplication show={true} onClose={() => {}} /></ProtectedRoute>} />
        <Route path="/manage" element={<ProtectedRoute><ManageApplications /></ProtectedRoute>} />
        <Route path="/manage/:id" element={<ProtectedRoute><ApplicationDetails /></ProtectedRoute>} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
