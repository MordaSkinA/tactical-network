-- ЧЕРНОВИК
CREATE TABLE IF NOT EXISTS users (
    id               uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    username         text NOT NULL,
    password_hash    text NOT NULL,
    password_salt    text NOT NULL,
    role             text NOT NULL,
    squad_id         text,
    discord_id       text,
    discord_username text,
    last_login_at    timestamp,
    last_login_ip    text
);
CREATE UNIQUE INDEX IF NOT EXISTS users_username_lower_uq ON users (lower(username));
