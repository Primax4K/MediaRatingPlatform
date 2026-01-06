CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

DO $$
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'media_type') THEN
            CREATE TYPE media_type AS ENUM ('movie','series','game');
        END IF;
    END$$;

CREATE TABLE IF NOT EXISTS app_user (
                                        id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                                        username TEXT NOT NULL UNIQUE,
                                        password_hash TEXT NOT NULL,
                                        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS genre (
                                     id UUID PRIMARY KEY DEFAULT public.gen_random_uuid(),
                                     name TEXT NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS media (
                                     id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                                     title TEXT NOT NULL,
                                     description TEXT NOT NULL,
                                     type media_type NOT NULL,
                                     release_year INT NOT NULL CHECK (release_year BETWEEN 1870 AND 3000),
                                     age_restriction SMALLINT NOT NULL CHECK (age_restriction BETWEEN 0 AND 21),

                                     average_rating NUMERIC(3,2) NOT NULL DEFAULT 0,
                                     ratings_count  INT NOT NULL DEFAULT 0,

                                     created_by UUID NOT NULL REFERENCES app_user(id) ON DELETE RESTRICT,
                                     created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                                     updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_media_title_trgm ON media USING gin (title gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_media_filters ON media(type, release_year, age_restriction);

CREATE TABLE IF NOT EXISTS media_genre (
                                           media_id UUID NOT NULL REFERENCES media(id) ON DELETE CASCADE,
                                           genre_id UUID NOT NULL REFERENCES genre(id) ON DELETE CASCADE,
                                           PRIMARY KEY (media_id, genre_id)
);

CREATE TABLE IF NOT EXISTS rating (
                                      id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                                      media_id UUID NOT NULL REFERENCES media(id) ON DELETE CASCADE,
                                      userid UUID NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
                                      stars SMALLINT NOT NULL CHECK (stars BETWEEN 1 AND 5),
                                      comment TEXT,
                                      comment_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
                                      created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                                      updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                                      UNIQUE (media_id, userid)
);

CREATE INDEX IF NOT EXISTS idx_rating_media ON rating(media_id);
CREATE INDEX IF NOT EXISTS idx_rating_user ON rating(userid);
CREATE INDEX IF NOT EXISTS idx_rating_confirmed ON rating(comment_confirmed);

CREATE TABLE IF NOT EXISTS rating_like (
                                           rating_id UUID NOT NULL REFERENCES rating(id) ON DELETE CASCADE,
                                           userid UUID NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
                                           created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                                           PRIMARY KEY (rating_id, userid)
);

CREATE TABLE IF NOT EXISTS favorite (
                                        userid UUID NOT NULL REFERENCES app_user(id) ON DELETE CASCADE,
                                        media_id UUID NOT NULL REFERENCES media(id) ON DELETE CASCADE,
                                        created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                                        PRIMARY KEY (userid, media_id)
);

CREATE OR REPLACE VIEW mediascore AS
SELECT
    m.id AS media_id,
    AVG(r.stars)::NUMERIC(3,2) AS avgstars,
    COUNT(r.*) AS ratingscount
FROM media m
         LEFT JOIN rating r ON r.media_id = m.id
GROUP BY m.id;

CREATE OR REPLACE VIEW leaderboarduserratings AS
SELECT
    u.id AS userid,
    u.username,
    COUNT(r.*) AS totalratings,
    COALESCE(AVG(NULLIF(r.stars,0)),0)::NUMERIC(3,2) AS avggivenstars
FROM app_user u
         LEFT JOIN rating r ON r.userid = u.id
GROUP BY u.id, u.username
ORDER BY totalratings DESC, avggivenstars DESC;

CREATE OR REPLACE FUNCTION setupdated_at() RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at := NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_media_updated ON media;
CREATE TRIGGER trg_media_updated
    BEFORE UPDATE ON media
    FOR EACH ROW EXECUTE FUNCTION setupdated_at();

DROP TRIGGER IF EXISTS trg_rating_updated ON rating;
CREATE TRIGGER trg_rating_updated
    BEFORE UPDATE ON rating
    FOR EACH ROW EXECUTE FUNCTION setupdated_at();

CREATE OR REPLACE FUNCTION refresh_media_score(p_media_id UUID) RETURNS VOID AS $$
BEGIN
    UPDATE media m
    SET
        average_rating = COALESCE((
                                      SELECT AVG(r.stars)::NUMERIC(3,2)
                                      FROM rating r
                                      WHERE r.media_id = p_media_id
                                  ), 0),
        ratings_count = COALESCE((
                                     SELECT COUNT(*)::INT
                                     FROM rating r
                                     WHERE r.media_id = p_media_id
                                 ), 0)
    WHERE m.id = p_media_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION trg_refresh_media_score() RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        PERFORM refresh_media_score(NEW.media_id);
        RETURN NEW;

    ELSIF TG_OP = 'DELETE' THEN
        PERFORM refresh_media_score(OLD.media_id);
        RETURN OLD;

    ELSIF TG_OP = 'UPDATE' THEN
        IF NEW.media_id IS DISTINCT FROM OLD.media_id THEN
            PERFORM refresh_media_score(OLD.media_id);
            PERFORM refresh_media_score(NEW.media_id);
        ELSE
            PERFORM refresh_media_score(NEW.media_id);
        END IF;
        RETURN NEW;
    END IF;

    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_rating_refresh_media_score ON rating;
CREATE TRIGGER trg_rating_refresh_media_score
    AFTER INSERT OR UPDATE OR DELETE ON rating
    FOR EACH ROW EXECUTE FUNCTION trg_refresh_media_score();