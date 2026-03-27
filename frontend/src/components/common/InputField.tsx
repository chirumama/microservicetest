import React from "react";
import {
  TextField as MUITextField,
  InputAdornment,
  MenuItem,
} from "@mui/material";

interface OptionType {
  label: string;
  value: string;
}

interface InputFieldProps {
  label: string;
  value?: string;
  onChange?: (
    e: React.ChangeEvent<HTMLInputElement> 
  ) => void;
  type?: "text" | "password" | "email";
  placeholder?: string;
  fullWidth?: boolean;
  error?: boolean;
  helperText?: string;
  startIcon?: React.ReactNode;
  endIcon?: React.ReactNode;

  select?: boolean;
  options?: OptionType[];
}

const InputField: React.FC<InputFieldProps> = ({
  label,
  value,
  onChange,
  type = "text",
  placeholder,
  fullWidth = false,
  error = false,
  helperText,
  startIcon,
  endIcon,
  select = false,
  options = [],
}) => {
  return (
    <MUITextField
      label={label}
      value={value}
      onChange={onChange}
      type={select ? undefined : type} // ✅ important
      placeholder={placeholder}
      fullWidth={fullWidth}
      error={error}
      helperText={helperText}
      variant="outlined"
      select={select}
      SelectProps={{
        displayEmpty: true, // ✅ shows placeholder
      }}
      InputProps={{
        startAdornment: startIcon ? (
          <InputAdornment position="start">
            {startIcon}
          </InputAdornment>
        ) : undefined,
        endAdornment: endIcon ? (
          <InputAdornment position="end">
            {endIcon}
          </InputAdornment>
        ) : undefined,
      }}
    >
      {select &&
        options.map((opt) => (
          <MenuItem key={opt.value} value={opt.value}>
            {opt.label}
          </MenuItem>
        ))}
    </MUITextField>
  );
};

export default InputField;