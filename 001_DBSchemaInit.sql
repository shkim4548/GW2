-- 001_init.sql
-- Initial schema for GameServer Admin Tool

CREATE TABLE admin (
    admin_id       SERIAL PRIMARY KEY,
    login_id       VARCHAR(50) UNIQUE NOT NULL,
    password_hash  VARCHAR(255) NOT NULL,
    role           VARCHAR(20) NOT NULL,
    created_at     TIMESTAMP NOT NULL DEFAULT NOW(),
    last_login_at  TIMESTAMP,
    is_active      BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE account (
    account_id     SERIAL PRIMARY KEY,
    login_id       VARCHAR(50) UNIQUE NOT NULL,
    password_hash  VARCHAR(255) NOT NULL,
    created_at     TIMESTAMP NOT NULL DEFAULT NOW(),
    last_login_at  TIMESTAMP,
    is_banned      BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE "user" (
    user_id        SERIAL PRIMARY KEY,
    account_id     INT NOT NULL REFERENCES account(account_id),
    nickname       VARCHAR(50) NOT NULL,
    level          INT NOT NULL DEFAULT 1,
    created_at     TIMESTAMP NOT NULL DEFAULT NOW(),
    last_login_at  TIMESTAMP,
    status         VARCHAR(20) NOT NULL
);

CREATE TABLE post (
    post_id        SERIAL PRIMARY KEY,
    post_type      VARCHAR(20) NOT NULL,
    title          VARCHAR(200) NOT NULL,
    content        TEXT NOT NULL,
    author_type    VARCHAR(20) NOT NULL,
    author_id      INT NOT NULL,
    created_at     TIMESTAMP NOT NULL DEFAULT NOW(),
    updated_at     TIMESTAMP,
    is_deleted     BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE admin_log (
    log_id         SERIAL PRIMARY KEY,
    admin_id       INT NOT NULL REFERENCES admin(admin_id),
    action         VARCHAR(100) NOT NULL,
    target_type    VARCHAR(50),
    target_id      INT,
    created_at     TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE user_log (
    log_id         SERIAL PRIMARY KEY,
    user_id        INT NOT NULL REFERENCES "user"(user_id),
    action         VARCHAR(100) NOT NULL,
    detail         TEXT,
    created_at     TIMESTAMP NOT NULL DEFAULT NOW()
);
