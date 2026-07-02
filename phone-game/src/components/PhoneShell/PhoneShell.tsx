import styles from "./PhoneShell.module.scss";

type PhoneShellProps = {
  eyebrow?: string;
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
  variant?: "join" | "game";
};

export const PhoneShell = ({
  eyebrow = "TV Poker",
  title,
  subtitle,
  children,
  footer,
  variant = "join",
}: PhoneShellProps) => {
  return (
    <main className={`${styles.shellPage} ${styles[variant]}`}>
      <section className={styles.shellCard}>
        <div className={styles.heroGlow} />
        <header className={styles.shellHeader}>
          <div>
            <p className={styles.eyebrow}>{eyebrow}</p>
            <h1>{title}</h1>
            {subtitle && <p className={styles.subtitle}>{subtitle}</p>}
          </div>
          <div className={styles.chipStack} aria-hidden="true">
            <span />
            <span />
            <span />
          </div>
        </header>
        <div className={styles.shellBody}>{children}</div>
        {footer && <footer className={styles.shellFooter}>{footer}</footer>}
      </section>
    </main>
  );
};
