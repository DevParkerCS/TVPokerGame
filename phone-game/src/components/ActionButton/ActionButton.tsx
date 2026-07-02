import type { ButtonHTMLAttributes } from "react";
import styles from "./ActionButton.module.scss";

type ActionButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "secondary" | "danger" | "ghost";
  fullWidth?: boolean;
};

export const ActionButton = ({
  variant = "primary",
  fullWidth = false,
  className = "",
  children,
  ...props
}: ActionButtonProps) => {
  const classes = [
    styles.button,
    styles[variant],
    fullWidth ? styles.fullWidth : "",
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <button className={classes} {...props}>
      {children}
    </button>
  );
};
