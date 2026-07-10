-- Schema for the FCA Handbook assistant. Applied idempotently on start by SchemaBootstrapper.
CREATE EXTENSION IF NOT EXISTS vector;

-- One row per cited provision. The embedding dimension (1536) matches text-embedding-3-small
-- and the local embedding generator. Citations always link back to url on handbook.fca.org.uk.
CREATE TABLE IF NOT EXISTS provisions (
    reference    text PRIMARY KEY,
    sourcebook   text NOT NULL,
    title        text NOT NULL,
    url          text NOT NULL,
    chunk_text   text NOT NULL,
    content_hash text NOT NULL,
    embedding    vector(1536) NOT NULL,
    ingested_at  timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS provisions_embedding_hnsw
    ON provisions USING hnsw (embedding vector_cosine_ops);

-- The per-answer traceability record: what was retrieved, what was cited, and the model,
-- prompt, safety, and cost context. Written for every question, whether answered or refused.
CREATE TABLE IF NOT EXISTS answer_audit (
    id                bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    asked_at          timestamptz NOT NULL,
    question          text NOT NULL,
    retrieved         jsonb NOT NULL,
    cited             jsonb NOT NULL,
    refused           boolean NOT NULL,
    refusal_reason    text,
    model             text NOT NULL,
    prompt_version    text NOT NULL,
    input_safety      text,
    output_safety     text,
    latency_ms        bigint NOT NULL,
    prompt_tokens     integer NOT NULL,
    completion_tokens integer NOT NULL
);
