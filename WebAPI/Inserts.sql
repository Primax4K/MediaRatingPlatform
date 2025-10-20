CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS pg_trgm;

DO $$
BEGIN
        IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'media_type') THEN
CREATE TYPE media_type AS ENUM ('movie','series','game');
END IF;
    END$$;

CREATE TABLE IF NOT EXISTS appuser (
                                       id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username TEXT NOT NULL UNIQUE,
    passwordhash TEXT NOT NULL,
    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW()
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
    releaseyear INT NOT NULL CHECK (releaseyear BETWEEN 1870 AND 3000),
    agerestriction SMALLINT NOT NULL CHECK (agerestriction BETWEEN 0 AND 21),
    createdby UUID NOT NULL REFERENCES appuser(id) ON DELETE RESTRICT,
    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updatedat TIMESTAMPTZ NOT NULL DEFAULT NOW()
    );
CREATE INDEX IF NOT EXISTS idx_media_title_trgm ON media USING gin (title gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_media_filters ON media(type, releaseyear, agerestriction);

CREATE TABLE IF NOT EXISTS mediagenre (
                                          mediaid UUID NOT NULL REFERENCES media(id) ON DELETE CASCADE,
    genreid UUID NOT NULL REFERENCES genre(id) ON DELETE CASCADE,
    PRIMARY KEY (mediaid, genreid)
    );

CREATE TABLE IF NOT EXISTS rating (
                                      id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    mediaid UUID NOT NULL REFERENCES media(id) ON DELETE CASCADE,
    userid UUID NOT NULL REFERENCES appuser(id) ON DELETE CASCADE,
    stars SMALLINT NOT NULL CHECK (stars BETWEEN 1 AND 5),
    comment TEXT,
    commentconfirmed BOOLEAN NOT NULL DEFAULT FALSE,
    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updatedat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (mediaid, userid)
    );
CREATE INDEX IF NOT EXISTS idx_rating_media ON rating(mediaid);
CREATE INDEX IF NOT EXISTS idx_rating_user ON rating(userid);
CREATE INDEX IF NOT EXISTS idx_rating_confirmed ON rating(commentconfirmed);

CREATE TABLE IF NOT EXISTS ratinglike (
                                          ratingid UUID NOT NULL REFERENCES rating(id) ON DELETE CASCADE,
    userid UUID NOT NULL REFERENCES appuser(id) ON DELETE CASCADE,
    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (ratingid, userid)
    );

CREATE TABLE IF NOT EXISTS favorite (
                                        userid UUID NOT NULL REFERENCES appuser(id) ON DELETE CASCADE,
    mediaid UUID NOT NULL REFERENCES media(id) ON DELETE CASCADE,
    createdat TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (userid, mediaid)
    );

CREATE OR REPLACE VIEW mediascore AS
SELECT
    m.id AS mediaid,
    AVG(r.stars)::NUMERIC(3,2) AS avgstars,
    COUNT(r.*) AS ratingscount
FROM media m
         LEFT JOIN rating r ON r.mediaid = m.id
GROUP BY m.id;

CREATE OR REPLACE VIEW leaderboarduserratings AS
SELECT
    u.id AS userid,
    u.username,
    COUNT(r.*) AS totalratings,
    COALESCE(AVG(NULLIF(r.stars,0)),0)::NUMERIC(3,2) AS avggivenstars
FROM appuser u
         LEFT JOIN rating r ON r.userid = u.id
GROUP BY u.id, u.username
ORDER BY totalratings DESC, avggivenstars DESC;

CREATE OR REPLACE FUNCTION setupdatedat() RETURNS TRIGGER AS $$
BEGIN
    NEW.updatedat := NOW();
RETURN NEW;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_media_updated ON media;
CREATE TRIGGER trg_media_updated BEFORE UPDATE ON media
    FOR EACH ROW EXECUTE FUNCTION setupdatedat();

DROP TRIGGER IF EXISTS trg_rating_updated ON rating;
CREATE TRIGGER trg_rating_updated BEFORE UPDATE ON rating
    FOR EACH ROW EXECUTE FUNCTION setupdatedat();
