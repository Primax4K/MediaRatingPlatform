CREATE TABLE app_user (
                          user_id SERIAL PRIMARY KEY,
                          username VARCHAR(50) UNIQUE NOT NULL,
                          password_hash TEXT NOT NULL,
                          created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE user_profile (
                              profile_id SERIAL PRIMARY KEY,
                              user_id INT NOT NULL UNIQUE REFERENCES app_user(user_id) ON DELETE CASCADE,
                              statistics JSONB DEFAULT '{}',
                              updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TYPE media_type_enum AS ENUM ('movie', 'series', 'game');

CREATE TABLE media_entry (
                             media_id SERIAL PRIMARY KEY,
                             creator_id INT NOT NULL REFERENCES app_user(user_id) ON DELETE CASCADE,
                             title VARCHAR(255) NOT NULL,
                             description TEXT,
                             media_type media_type_enum NOT NULL,
                             release_year INT,
                             age_restriction INT,
                             created_at TIMESTAMP DEFAULT NOW(),
                             updated_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE genre (
                       genre_id SERIAL PRIMARY KEY,
                       name VARCHAR(50) UNIQUE NOT NULL
);

CREATE TABLE media_genre (
                             media_id INT NOT NULL REFERENCES media_entry(media_id) ON DELETE CASCADE,
                             genre_id INT NOT NULL REFERENCES genre(genre_id) ON DELETE CASCADE,
                             PRIMARY KEY (media_id, genre_id)
);

CREATE TABLE rating (
                        rating_id SERIAL PRIMARY KEY,
                        user_id INT NOT NULL REFERENCES app_user(user_id) ON DELETE CASCADE,
                        media_id INT NOT NULL REFERENCES media_entry(media_id) ON DELETE CASCADE,
                        stars INT NOT NULL CHECK (stars BETWEEN 1 AND 5),
                        comment TEXT,
                        comment_confirmed BOOLEAN DEFAULT FALSE,
                        created_at TIMESTAMP DEFAULT NOW(),
                        updated_at TIMESTAMP DEFAULT NOW(),
                        UNIQUE (user_id, media_id)
);

CREATE TABLE rating_like (
                             user_id INT NOT NULL REFERENCES app_user(user_id) ON DELETE CASCADE,
                             rating_id INT NOT NULL REFERENCES rating(rating_id) ON DELETE CASCADE,
                             PRIMARY KEY (user_id, rating_id)
);

CREATE TABLE favorite (
                          user_id INT NOT NULL REFERENCES app_user(user_id) ON DELETE CASCADE,
                          media_id INT NOT NULL REFERENCES media_entry(media_id) ON DELETE CASCADE,
                          PRIMARY KEY (user_id, media_id)
);

CREATE VIEW media_average_score AS
SELECT
    m.media_id,
    AVG(r.stars)::NUMERIC(3,2) AS average_score,
    COUNT(r.rating_id) AS total_ratings
FROM media_entry m
         LEFT JOIN rating r ON m.media_id = r.media_id
GROUP BY m.media_id;