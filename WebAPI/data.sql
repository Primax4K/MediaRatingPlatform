-- users
INSERT INTO app_user (id, username, password_hash) VALUES
                                                       ('11111111-1111-1111-1111-111111111111','alice','$2a$10$alicehash'),
                                                       ('22222222-2222-2222-2222-222222222222','bob','$2a$10$bobhash'),
                                                       ('33333333-3333-3333-3333-333333333333','carol','$2a$10$carolhash');

-- genres
INSERT INTO genre (id, name) VALUES
                                 ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa','Action'),
                                 ('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb','Drama'),
                                 ('cccccccc-cccc-cccc-cccc-cccccccccccc','Comedy'),
                                 ('dddddddd-dddd-dddd-dddd-dddddddddddd','RPG');

-- media
INSERT INTO media (
    id, title, description, type, release_year, age_restriction, created_by
) VALUES
      ('aaaaaaaa-1111-1111-1111-aaaaaaaa1111','Inception','Dreams within dreams','movie',2010,13,'11111111-1111-1111-1111-111111111111'),
      ('bbbbbbbb-2222-2222-2222-bbbbbbbb2222','Breaking Bad','Chemistry teacher turned criminal','series',2008,16,'22222222-2222-2222-2222-222222222222'),
      ('cccccccc-3333-3333-3333-cccccccc3333','Elden Ring','Open world fantasy RPG','game',2022,16,'33333333-3333-3333-3333-333333333333');

-- media_genre
INSERT INTO media_genre (media_id, genre_id) VALUES
                                                 ('aaaaaaaa-1111-1111-1111-aaaaaaaa1111','aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'),
                                                 ('aaaaaaaa-1111-1111-1111-aaaaaaaa1111','bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),
                                                 ('bbbbbbbb-2222-2222-2222-bbbbbbbb2222','bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'),
                                                 ('cccccccc-3333-3333-3333-cccccccc3333','dddddddd-dddd-dddd-dddd-dddddddddddd');

-- ratings (valid UUIDs)
INSERT INTO rating (
    id, media_id, userid, stars, comment, comment_confirmed
) VALUES
      ('41111111-1111-1111-1111-111111111111','aaaaaaaa-1111-1111-1111-aaaaaaaa1111','22222222-2222-2222-2222-222222222222',5,'Amazing movie',TRUE),
      ('42222222-2222-2222-2222-222222222222','bbbbbbbb-2222-2222-2222-bbbbbbbb2222','11111111-1111-1111-1111-111111111111',4,'Very intense',TRUE),
      ('43333333-3333-3333-3333-333333333333','cccccccc-3333-3333-3333-cccccccc3333','11111111-1111-1111-1111-111111111111',5,'Masterpiece',FALSE);

-- rating likes
INSERT INTO rating_like (rating_id, userid) VALUES
                                                ('41111111-1111-1111-1111-111111111111','11111111-1111-1111-1111-111111111111'),
                                                ('42222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333333');

-- favorites
INSERT INTO favorite (userid, media_id) VALUES
                                            ('11111111-1111-1111-1111-111111111111','aaaaaaaa-1111-1111-1111-aaaaaaaa1111'),
                                            ('22222222-2222-2222-2222-222222222222','cccccccc-3333-3333-3333-cccccccc3333');
