CREATE TABLE IF NOT EXISTS public.campaign_downloads (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    campaign VARCHAR(80) NOT NULL,
    anonymous_token_hash VARCHAR(128) NOT NULL,
    source VARCHAR(40) NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    CONSTRAINT ux_campaign_downloads_campaign_token UNIQUE (campaign, anonymous_token_hash)
);

CREATE INDEX IF NOT EXISTS ix_campaign_downloads_campaign
    ON public.campaign_downloads (campaign);
