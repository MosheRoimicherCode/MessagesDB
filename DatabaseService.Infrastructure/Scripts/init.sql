CREATE TABLE IF NOT EXISTS users (
    id BIGSERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    phone_number TEXT NOT NULL UNIQUE,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE IF NOT EXISTS messages (
    id BIGSERIAL PRIMARY KEY,
    user_id BIGINT NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    session_id TEXT NOT NULL,
    project_name TEXT NOT NULL,
    text TEXT NOT NULL DEFAULT '',
    telegram_chat_id BIGINT NOT NULL DEFAULT 0,
    telegram_message_id BIGINT NOT NULL DEFAULT 0,
    direction SMALLINT NOT NULL DEFAULT 1,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Keep this script safe to rerun against databases created by older versions.
ALTER TABLE messages
    ADD COLUMN IF NOT EXISTS telegram_chat_id BIGINT NOT NULL DEFAULT 0;

ALTER TABLE messages
    ADD COLUMN IF NOT EXISTS telegram_message_id BIGINT NOT NULL DEFAULT 0;

ALTER TABLE messages
    ADD COLUMN IF NOT EXISTS direction SMALLINT NOT NULL DEFAULT 1;

ALTER TABLE messages
    ALTER COLUMN text SET DEFAULT '';

CREATE TABLE IF NOT EXISTS message_files (
    id BIGSERIAL PRIMARY KEY,
    message_id BIGINT NOT NULL REFERENCES messages(id) ON DELETE CASCADE,
    telegram_file_id TEXT NOT NULL,
    telegram_file_unique_id TEXT NOT NULL DEFAULT '',
    file_name TEXT NOT NULL DEFAULT '',
    mime_type TEXT NOT NULL DEFAULT '',
    file_size BIGINT NOT NULL DEFAULT 0,
    file_kind TEXT NOT NULL,
    thumbnail_telegram_file_id TEXT,
    thumbnail_telegram_file_unique_id TEXT,
    thumbnail_width INTEGER,
    thumbnail_height INTEGER,
    thumbnail_file_size BIGINT,
    created_at_utc TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT ck_message_files_file_size_nonnegative CHECK (file_size >= 0),
    CONSTRAINT ck_message_files_thumbnail_width_nonnegative
        CHECK (thumbnail_width IS NULL OR thumbnail_width >= 0),
    CONSTRAINT ck_message_files_thumbnail_height_nonnegative
        CHECK (thumbnail_height IS NULL OR thumbnail_height >= 0),
    CONSTRAINT ck_message_files_thumbnail_size_nonnegative
        CHECK (thumbnail_file_size IS NULL OR thumbnail_file_size >= 0)
);

CREATE INDEX IF NOT EXISTS idx_messages_session_id
ON messages(session_id);

CREATE INDEX IF NOT EXISTS idx_messages_user_id
ON messages(user_id);

CREATE INDEX IF NOT EXISTS idx_messages_user_project_created_at
ON messages(user_id, project_name, created_at_utc);

CREATE INDEX IF NOT EXISTS idx_messages_telegram_message_id
ON messages(telegram_message_id);

CREATE INDEX IF NOT EXISTS idx_messages_telegram_reference
ON messages(telegram_chat_id, telegram_message_id);

CREATE INDEX IF NOT EXISTS idx_message_files_message_id
ON message_files(message_id);

CREATE INDEX IF NOT EXISTS idx_message_files_telegram_file_id
ON message_files(telegram_file_id);
