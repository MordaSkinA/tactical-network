-- 1. Squads and Members
CREATE TABLE IF NOT EXISTS squads (
    id VARCHAR(50) PRIMARY KEY,
    label VARCHAR(100),
    side VARCHAR(20),
    leader_name VARCHAR(100),
    tags TEXT[]
);

CREATE TABLE IF NOT EXISTS squad_members (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    squad_id VARCHAR(50) REFERENCES squads(id) ON DELETE CASCADE,
    nickname VARCHAR(100) UNIQUE NOT NULL,
    role VARCHAR(20) NOT NULL,
    bulwark VARCHAR(20),
    build VARCHAR(50)
);

-- 2. Order Macros
CREATE TABLE IF NOT EXISTS order_macros (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    type VARCHAR(20) NOT NULL
);

CREATE TABLE IF NOT EXISTS order_macro_squads (
    macro_id VARCHAR(50) REFERENCES order_macros(id) ON DELETE CASCADE,
    squad_id VARCHAR(50) REFERENCES squads(id) ON DELETE CASCADE,
    PRIMARY KEY (macro_id, squad_id)
);

-- 3. Goal Order Macros
CREATE TABLE IF NOT EXISTS goal_order_macros (
    id VARCHAR(50) PRIMARY KEY,
    issuer_name VARCHAR(100) NOT NULL,
    text TEXT NOT NULL,
    timer_seconds INTEGER,
    phase VARCHAR(20)
);

CREATE TABLE IF NOT EXISTS goal_order_macro_targets (
    macro_id VARCHAR(50) REFERENCES goal_order_macros(id) ON DELETE CASCADE,
    target_type VARCHAR(20) NOT NULL, -- 'build', 'role', 'side', 'squad'
    target_value VARCHAR(100) NOT NULL,
    PRIMARY KEY (macro_id, target_type, target_value)
);

-- 4. Member Presets
CREATE TABLE IF NOT EXISTS member_presets (
    nickname VARCHAR(100) PRIMARY KEY,
    role VARCHAR(20) NOT NULL,
    bulwark VARCHAR(20),
    build VARCHAR(50)
);

-- 5. Discord Webhooks
CREATE TABLE IF NOT EXISTS discord_webhooks (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    webhook_url TEXT NOT NULL
);
