CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;
CREATE TABLE alert_rules (
    id uuid NOT NULL,
    owner_id uuid NOT NULL,
    base character varying(3) NOT NULL,
    quote character varying(3) NOT NULL,
    threshold_percent numeric(5,2) NOT NULL,
    channel integer NOT NULL,
    enabled boolean NOT NULL,
    CONSTRAINT pk_alert_rules PRIMARY KEY (id)
);

CREATE TABLE alerts (
    id uuid NOT NULL,
    rule_id uuid NOT NULL,
    previous_rate numeric(18,8) NOT NULL,
    current_rate numeric(18,8) NOT NULL,
    observed_change_percent numeric(5,2) NOT NULL,
    fired_at timestamp with time zone NOT NULL,
    CONSTRAINT pk_alerts PRIMARY KEY (id)
);

CREATE TABLE currencies (
    code character varying(3) NOT NULL,
    name character varying(100) NOT NULL,
    numeric_code integer NOT NULL,
    CONSTRAINT pk_currencies PRIMARY KEY (code)
);

CREATE TABLE rate_snapshots (
    base character varying(3) NOT NULL,
    as_of date NOT NULL,
    CONSTRAINT pk_rate_snapshots PRIMARY KEY (base, as_of)
);

CREATE TABLE exchange_rates (
    quote character varying(3) NOT NULL,
    snapshot_base character varying(3) NOT NULL,
    snapshot_as_of date NOT NULL,
    base character varying(3) NOT NULL,
    rate numeric(18,8) NOT NULL,
    as_of date NOT NULL,
    CONSTRAINT pk_exchange_rates PRIMARY KEY (snapshot_base, snapshot_as_of, quote),
    CONSTRAINT fk_exchange_rates_rate_snapshots_snapshot_base_snapshot_as_of FOREIGN KEY (snapshot_base, snapshot_as_of) REFERENCES rate_snapshots (base, as_of) ON DELETE CASCADE
);

CREATE INDEX ix_alert_rules_owner_id ON alert_rules (owner_id);

CREATE INDEX ix_alerts_rule_id ON alerts (rule_id);

INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20260528190909_InitialCreate', '10.0.4');

COMMIT;

