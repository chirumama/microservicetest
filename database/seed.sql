
-- -----------------------------------------------------------------------------
-- roles
-- -----------------------------------------------------------------------------

CREATE TABLE roles (
    id   INTEGER      PRIMARY KEY,
    name VARCHAR(50)  NOT NULL
);

INSERT INTO roles (id, name) VALUES
    (1, 'User'),
    (2, 'Admin'),
    (3, 'SuperAdmin')
ON CONFLICT (id) DO NOTHING;


-- -----------------------------------------------------------------------------
-- users
-- -----------------------------------------------------------------------------

CREATE TABLE users (
    id           SERIAL        PRIMARY KEY,
    email        VARCHAR(255)  NOT NULL,
    passwordhash TEXT          NOT NULL,
    roleid       INTEGER       NOT NULL,
    isactive     BOOLEAN       DEFAULT TRUE,
    createdat    TIMESTAMPTZ   DEFAULT NOW(),
    updatedat    TIMESTAMPTZ
);

ALTER TABLE users
    ADD CONSTRAINT fk_users_role FOREIGN KEY (roleid) REFERENCES roles(id);

-- superadmin  |  password: Unlock@123Y
INSERT INTO users (email, passwordhash, roleid, isactive, createdat) VALUES (
    'yogeshchauhan0991@gmail.com',
    '$2b$11$ifxbBUaUIMLQwbkqyav3oeuLPghGvTY7ch4Od1MeU6JIjTKq3L/nS',
    3,
    TRUE,
    NOW()
);


-- -----------------------------------------------------------------------------
-- userotps
-- -----------------------------------------------------------------------------

CREATE TABLE userotps (
    id         SERIAL        PRIMARY KEY,
    userid     INTEGER,
    otpcode    VARCHAR(6),
    expirytime TIMESTAMPTZ,
    isused     BOOLEAN       DEFAULT FALSE,
    createdat  TIMESTAMPTZ   DEFAULT NOW()
);


-- -----------------------------------------------------------------------------
-- microservices
-- -----------------------------------------------------------------------------

CREATE TABLE microservices (
    id          SERIAL        PRIMARY KEY,
    name        VARCHAR(200)  NOT NULL,
    description TEXT,
    isactive    BOOLEAN       DEFAULT TRUE,
    createdat   TIMESTAMPTZ   DEFAULT NOW()
);

INSERT INTO microservices (id, name, description, isactive, createdat) VALUES
    (1, 'IpLookup2',            'Second IP Lookup service on port 5225',     TRUE, '2026-04-24 08:41:54.610+00'),
    (2, 'PanVerification',      'PAN card verification service on port 5240', TRUE, '2026-04-27 10:49:19.610+00'),
    (3, 'PassportVerification', 'Passport verification service on port 5241', TRUE, '2026-04-27 10:49:19.610+00')
ON CONFLICT (id) DO NOTHING;


-- -----------------------------------------------------------------------------
-- applications
-- -----------------------------------------------------------------------------

CREATE TABLE applications (
    id          SERIAL        PRIMARY KEY,
    userid      INTEGER       NOT NULL,
    title       VARCHAR(200)  NOT NULL,
    description TEXT,
    status      INTEGER,
    createdat   TIMESTAMPTZ   DEFAULT NOW(),
    updatedat   TIMESTAMPTZ
);

ALTER TABLE applications
    ADD CONSTRAINT fk_applications_user FOREIGN KEY (userid) REFERENCES users(id);


-- -----------------------------------------------------------------------------
-- applicationmicroservices
-- -----------------------------------------------------------------------------

CREATE TABLE applicationmicroservices (
    id               SERIAL       PRIMARY KEY,
    applicationid    INTEGER      NOT NULL,
    microserviceid   INTEGER      NOT NULL,
    isenabled        BOOLEAN      DEFAULT TRUE,
    createdat        TIMESTAMPTZ  DEFAULT NOW(),
    updatedat        TIMESTAMPTZ
);

ALTER TABLE applicationmicroservices
    ADD CONSTRAINT fk_appmicro_app   FOREIGN KEY (applicationid)  REFERENCES applications(id);

ALTER TABLE applicationmicroservices
    ADD CONSTRAINT fk_appmicro_micro FOREIGN KEY (microserviceid) REFERENCES microservices(id);

ALTER TABLE applicationmicroservices
    ADD CONSTRAINT uq_appmicro UNIQUE (applicationid, microserviceid);


-- -----------------------------------------------------------------------------
-- applicationenvironments
-- -----------------------------------------------------------------------------

CREATE TABLE applicationenvironments (
    id             SERIAL       PRIMARY KEY,
    applicationid  INTEGER      NOT NULL,
    environment    VARCHAR(50)  NOT NULL,
    isenabled      BOOLEAN      DEFAULT TRUE,
    createdat      TIMESTAMPTZ  DEFAULT NOW(),
    updatedat      TIMESTAMPTZ
);

ALTER TABLE applicationenvironments
    ADD CONSTRAINT fk_appenv_app FOREIGN KEY (applicationid) REFERENCES applications(id);


-- -----------------------------------------------------------------------------
-- apikeys
-- -----------------------------------------------------------------------------

CREATE TABLE apikeys (
    id                     SERIAL       PRIMARY KEY,
    applicationid          INTEGER      NOT NULL,
    environment            VARCHAR(50)  NOT NULL,
    appkey                 TEXT         NOT NULL,
    appsecrethash          TEXT         NOT NULL,
    isactive               BOOLEAN      DEFAULT TRUE,
    isenvironmentenabled   BOOLEAN      DEFAULT TRUE,
    createdat              TIMESTAMPTZ  DEFAULT NOW(),
    updatedat              TIMESTAMPTZ,
    consumerkey            TEXT,
    accesstoken            TEXT
);

ALTER TABLE apikeys
    ADD CONSTRAINT fk_apikeys_app FOREIGN KEY (applicationid) REFERENCES applications(id);


-- -----------------------------------------------------------------------------
-- microserviceroutes
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS microserviceroutes (
    id              SERIAL       PRIMARY KEY,
    microserviceid  INT          NOT NULL REFERENCES microservices(id) ON DELETE CASCADE,
    routeid         TEXT         NOT NULL,
    method          TEXT         NOT NULL,
    path            TEXT         NOT NULL,
    description     TEXT,
    isactive        BOOLEAN      NOT NULL DEFAULT TRUE,
    createdat       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE (microserviceid, routeid)
);

INSERT INTO microserviceroutes (microserviceid, routeid, method, path, description) VALUES
    (1, 'iplookup2-get-ip',        'GET',  '/v1/IpLookup/{ip}',          'Look up geolocation and proxy info for an IP address'),
    (2, 'pan-verification-verify', 'POST', '/api/v1/pan/verify',          'Verify a PAN card number'),
    (3, 'passport-verify',         'POST', '/api/passport/verify',        'Verify passport by file number and date of birth'),
    (3, 'passport-health',         'GET',  '/api/passport/health',        'Passport service health check'),
    (3, 'passport-health-db',      'GET',  '/api/passport/health/db',     'Passport service database health check')
ON CONFLICT (microserviceid, routeid) DO NOTHING;


-- -----------------------------------------------------------------------------
-- applicationroutes
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS applicationroutes (
    id              SERIAL       PRIMARY KEY,
    applicationid   INT          NOT NULL REFERENCES applications(id)  ON DELETE CASCADE,
    microserviceid  INT          NOT NULL REFERENCES microservices(id) ON DELETE CASCADE,
    routeid         TEXT         NOT NULL,
    isenabled       BOOLEAN      NOT NULL DEFAULT TRUE,
    createdat       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updatedat       TIMESTAMPTZ,
    UNIQUE (applicationid, routeid)
);


-- =============================================================================
-- end of seed.sql