CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

DO $$
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'media_type') THEN
            CREATE TYPE media_type AS ENUM ('movie','series','game');
        END IF;
    END$$;

CREATE TABLE IF NOT EXISTS app_user (
                                        id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                                        username       TEXT NOT NULL UNIQUE,
                                        password_hash  TEXT NOT NULL,
                                        created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS genre (
                                     id    UUID PRIMARY KEY DEFAULT public.gen_random_uuid(),
                                     name  TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS media (
                                     id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                                     title            TEXT NOT NULL,
                                     description      TEXT NOT NULL,
                                     type             media_type NOT NULL,
                                     release_year     INT NOT NULL CHECK (release_year BETWEEN 1870 AND 3000),
                                     age_restriction  SMALLINT NOT NULL CHECK (age_restriction BETWEEN 0 AND 21),
                                     created_by       UUID NOT NULL REFERENCES app_user(id) ON DELETE RESTRICT,
                                     created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                                     updated_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
CREATE INDEX IF NOT EXISTS idx_media_title_trgm ON media USING gin (title gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_media_filters ON media(type, release_year, age_restriction);

CREATE TABLE IF NOT EXISTS media_genre (
                                           media_id UUID NOT NULL REFERENCES media(id) ON DELETE CASCADE,
                                           genre_id UUID NOT NULL REFERENCES genre(id) ON DELETE CASCADE,
                                           PRIMARY KEY (media_id, genre_id)
);

CREATE TABLE IF NOT EXISTS rating (
                                      id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                                      media_id           UUID NOT NULL REFERENCES media(id)  ON DELETE CASCADE,
                                      user_id            UUID NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
                                      stars              SMALLINT NOT NULL CHECK (stars BETWEEN 1 AND 5),
                                      comment            TEXT,
                                      comment_confirmed  BOOLEAN NOT NULL DEFAULT FALSE,
                                      created_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                                      updated_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                                      UNIQUE (media_id, user_id)
);
CREATE INDEX IF NOT EXISTS idx_rating_media ON rating(media_id);
CREATE INDEX IF NOT EXISTS idx_rating_user ON rating(user_id);
CREATE INDEX IF NOT EXISTS idx_rating_confirmed ON rating(comment_confirmed);

CREATE TABLE IF NOT EXISTS rating_like (
                                           rating_id UUID NOT NULL REFERENCES rating(id) ON DELETE CASCADE,
                                           user_id   UUID NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
                                           created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                                           PRIMARY KEY (rating_id, user_id)
);

CREATE TABLE IF NOT EXISTS favorite (
                                        user_id   UUID NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
                                        media_id  UUID NOT NULL REFERENCES media(id) ON DELETE CASCADE,
                                        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                                        PRIMARY KEY (user_id, media_id)
);

CREATE OR REPLACE VIEW media_score AS
SELECT
    m.id AS media_id,
    AVG(r.stars)::NUMERIC(3,2) AS avg_stars,
    COUNT(r.*) AS ratings_count
FROM media m
         LEFT JOIN rating r ON r.media_id = m.id
GROUP BY m.id;

CREATE OR REPLACE VIEW leaderboard_user_ratings AS
SELECT
    u.id   AS user_id,
    u.username,
    COUNT(r.*) AS total_ratings,
    COALESCE(AVG(NULLIF(r.stars,0)),0)::NUMERIC(3,2) AS avg_given_stars
FROM app_user u
         LEFT JOIN rating r ON r.user_id = u.id
GROUP BY u.id, u.username
ORDER BY total_ratings DESC, avg_given_stars DESC;

CREATE OR REPLACE FUNCTION set_updated_at() RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at := NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;


DROP TRIGGER IF EXISTS trg_media_updated ON media;
CREATE TRIGGER trg_media_updated BEFORE UPDATE ON media
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();

DROP TRIGGER IF EXISTS trg_rating_updated ON rating;
CREATE TRIGGER trg_rating_updated BEFORE UPDATE ON rating
    FOR EACH ROW EXECUTE FUNCTION set_updated_at();