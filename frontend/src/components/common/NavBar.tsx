import { useState } from "react";
import { useNavigate } from "react-router-dom";

import {
  AppBar,
  Toolbar,
  IconButton,
  Avatar,
  Box,
  Menu,
  MenuItem,
  Typography,
  Divider,
} from "@mui/material";

import {
  VpnKey,
  ExitToApp,
} from "@mui/icons-material";

import { useAuth } from "../../context/AuthContext";

interface NavBarProps {
  username?: string;
}

const NavBar: React.FC<NavBarProps> = ({
  username = "User",
}) => {
  const [anchorEl, setAnchorEl] =
    useState<null | HTMLElement>(null);

  const navigate = useNavigate();
  const { logout } = useAuth();

  const open = Boolean(anchorEl);

  const handleAvatarClick = (
    event: React.MouseEvent<HTMLElement>
  ): void => {
    setAnchorEl(event.currentTarget);
  };

  const handleMenuClose = (): void => {
    setAnchorEl(null);
  };

  const handleLogout = (): void => {
    logout();
    navigate("/");
    handleMenuClose();
  };

  const handleChangePassword = (): void => {
    navigate("/forgot-password");
    handleMenuClose();
  };

  // Get initials from username
  const getInitials = (name: string): string => {
    const names = name.split(" ");

    if (names.length >= 2) {
      return `${names[0][0]}${names[1][0]}`.toUpperCase();
    }

    return name[0]?.toUpperCase() || "U";
  };

  return (
    <AppBar
      position="sticky"
      elevation={0}
      sx={{
        backgroundColor: "#ffffff",
        color: "text.primary",
        borderBottom: "1px solid #f1f1f1",
      }}
    >
      <Toolbar
        sx={{
          display: "flex",
          justifyContent: "space-between",
          px: 3,
        }}
      >
        {/* Left Section */}
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
            gap: 1,
          }}
        >
          <Typography
            variant="h6"
            sx={{
              color: "#333",
              fontWeight: 500,
            }}
          >
            Hello, {username} 👋
          </Typography>
        </Box>

        {/* Right Section */}
        <Box
          sx={{
            display: "flex",
            alignItems: "center",
          }}
        >
          {/* Avatar */}
          <IconButton
            sx={{ padding: 0 }}
            onClick={handleAvatarClick}
          >
            <Avatar
              sx={{
                width: 40,
                height: 40,
                bgcolor: "#667eea",
                fontSize: 15,
                fontWeight: 600,
              }}
            >
              {getInitials(username)}
            </Avatar>
          </IconButton>

          {/* Dropdown Menu */}
          <Menu
            anchorEl={anchorEl}
            open={open}
            onClose={handleMenuClose}
            PaperProps={{
              elevation: 3,
              sx: {
                mt: 1.5,
                width: 220,
                borderRadius: "12px",

                "& .MuiMenuItem-root": {
                  fontSize: "0.875rem",
                  py: 1.2,
                  px: 2,
                },
              },
            }}
            transformOrigin={{
              horizontal: "right",
              vertical: "top",
            }}
            anchorOrigin={{
              horizontal: "right",
              vertical: "bottom",
            }}
          >
            {/* Account Header */}
            <MenuItem
              sx={{
                cursor: "default",
                "&:hover": {
                  backgroundColor: "transparent",
                },
                pb: 0.5,
              }}
            >
              <Box>
                <Typography
                  variant="subtitle2"
                  sx={{
                    fontWeight: 600,
                    color: "text.primary",
                  }}
                >
                  My Account
                </Typography>

                <Typography
                  variant="caption"
                  sx={{
                    color: "text.secondary",
                  }}
                >
                  {username}
                </Typography>
              </Box>
            </MenuItem>

            <Divider sx={{ my: 1 }} />

            {/* Change Password */}
            <MenuItem onClick={handleChangePassword}>
              <VpnKey
                sx={{
                  fontSize: 18,
                  mr: 1.5,
                  color: "text.secondary",
                }}
              />

              <Typography variant="body2">
                Change Password
              </Typography>
            </MenuItem>

            {/* Logout */}
            <MenuItem onClick={handleLogout}>
              <ExitToApp
                sx={{
                  fontSize: 18,
                  mr: 1.5,
                  color: "text.secondary",
                }}
              />

              <Typography variant="body2">
                Logout
              </Typography>
            </MenuItem>
          </Menu>
        </Box>
      </Toolbar>
    </AppBar>
  );
};

export default NavBar;