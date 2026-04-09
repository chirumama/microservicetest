import React from "react";
import { Box, Tabs, Tab } from "@mui/material";

export interface TabItem {
  label: string;
}

export interface TabBarProps {
  tabs: TabItem[];
  value: number;
  onChange: (index: number) => void;
  centered?: boolean;
  variant?: "standard" | "scrollable";
  disabled?: boolean;
}

const TabBar: React.FC<TabBarProps> = ({
  tabs,
  value,
  onChange,
  centered = false,
  disabled = false,
}) => {
  return (
    <Box sx={{ borderBottom: 1, borderColor: "divider" }}>
      <Tabs
        value={value}
        onChange={(_, newValue) => onChange(newValue)}
        centered={centered}
         variant="fullWidth"
      >
        {tabs.map((tab, index) => (
          <Tab key={index} label={tab.label} disabled={disabled} />
        ))}
      </Tabs>
    </Box>
  );
};

export default TabBar;