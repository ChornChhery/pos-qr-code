-- Runs once, when the PostgreSQL data volume is first created.
-- One database per service. No shared tables.
CREATE DATABASE identity_db;
CREATE DATABASE catalog_db;
CREATE DATABASE order_db;
CREATE DATABASE payment_db;
