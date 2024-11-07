CREATE TABLE IF NOT EXISTS event_store (
                                           id SERIAL PRIMARY KEY,
                                           stream_id TEXT NOT NULL,
                                           position BIGINT NOT NULL,
                                           event_type TEXT NOT NULL,
                                           event_data JSONB NOT NULL,
                                           metadata JSONB NOT NULL,
                                           created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                                           UNIQUE(stream_id, position)
);

CREATE OR REPLACE FUNCTION append_event(
    p_event JSONB,
    p_event_type TEXT,
    p_stream_id TEXT,
    p_expected_version BIGINT
) RETURNS VOID AS $$
DECLARE
    latest_version BIGINT;
BEGIN
    SELECT COALESCE(MAX(position), -1) INTO latest_version
    FROM event_store
    WHERE stream_id = p_stream_id;

    IF p_expected_version = 0 AND latest_version != -1 THEN
        RAISE EXCEPTION 'Expected version is 0 but stream already has events';
    ELSIF p_expected_version > 0 AND latest_version + 1 != p_expected_version THEN
        RAISE EXCEPTION 'Expected version % does not match the latest version %',
            p_expected_version, latest_version;
    END IF;

    INSERT INTO event_store (stream_id, position, event_type, event_data, metadata)
    VALUES (p_stream_id, latest_version + 1, p_event_type, p_event, '{}');
END;
$$ LANGUAGE plpgsql;

CREATE INDEX IF NOT EXISTS idx_event_store_stream_id ON event_store (stream_id);
CREATE INDEX IF NOT EXISTS idx_event_store_created_at ON event_store (created_at);

CREATE TABLE IF NOT EXISTS grid (
                                    stream_id UUID PRIMARY KEY,
                                    name TEXT NOT NULL,
                                    width INT NOT NULL,
                                    height INT NOT NULL
);

CREATE TABLE IF NOT EXISTS grid_cells (
                                          grid_id UUID NOT NULL,
                                          x INT NOT NULL,
                                          y INT NOT NULL,
                                          color TEXT NOT NULL,
                                          PRIMARY KEY (grid_id, x, y)
);
