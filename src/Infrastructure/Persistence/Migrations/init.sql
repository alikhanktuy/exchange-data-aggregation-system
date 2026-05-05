CREATE EXTENSION IF NOT EXISTS timescaledb;

CREATE TABLE IF NOT EXISTS ticks (
    ticker      TEXT        NOT NULL,
    price       NUMERIC     NOT NULL,
    volume      NUMERIC     NOT NULL,
    timestamp   TIMESTAMPTZ NOT NULL,
    source      TEXT        NOT NULL
);

SELECT create_hypertable('ticks', 'timestamp', if_not_exists => TRUE);

CREATE INDEX IF NOT EXISTS idx_ticks_ticker_time
    ON ticks (ticker, timestamp DESC);
