#!/usr/bin/env python3
"""
Converts a Neon connection URI into the ADO.NET connection string the API needs.

Neon hands you a URI:

    postgresql://user:password@ep-xxx.us-east-1.aws.neon.tech/neondb?sslmode=require

Npgsql does not parse that form - it wants keyword syntax:

    Host=...;Port=5432;Database=...;Username=...;Password=...;SSL Mode=Require;...

Usage:
    python scripts/neon-connection-string.py
    python scripts/neon-connection-string.py "postgresql://..."

Nothing is sent anywhere; the conversion is entirely local.
"""

import sys
import urllib.parse


def convert(uri: str) -> str:
    parsed = urllib.parse.urlparse(uri.strip())

    if parsed.scheme not in ("postgres", "postgresql"):
        raise ValueError(
            f"Expected a postgres:// or postgresql:// URI, got '{parsed.scheme or 'nothing'}'."
        )

    host = parsed.hostname or ""
    if not host:
        raise ValueError("No host found in the URI - did you paste the whole thing?")

    # Neon offers a pooled endpoint alongside the direct one. EF Core issues prepared
    # statements, which PgBouncer in transaction-pooling mode does not support, so the
    # direct endpoint is the right target.
    if "-pooler" in host:
        host = host.replace("-pooler", "")
        print(
            "NOTE: stripped '-pooler' from the host - EF Core needs the direct endpoint.",
            file=sys.stderr,
        )

    database = (parsed.path or "").lstrip("/")
    if not database:
        raise ValueError("No database name found in the URI.")

    # Credentials arrive percent-encoded in a URI but must be literal in a keyword string.
    username = urllib.parse.unquote(parsed.username or "")
    password = urllib.parse.unquote(parsed.password or "")

    return ";".join([
        f"Host={host}",
        f"Port={parsed.port or 5432}",
        f"Database={database}",
        f"Username={username}",
        f"Password={password}",
        "SSL Mode=Require",
        "Trust Server Certificate=true",
    ])


def main() -> int:
    if len(sys.argv) > 1:
        uri = sys.argv[1]
    else:
        uri = input("Paste your Neon connection URI: ")

    try:
        print(convert(uri))
    except ValueError as error:
        print(f"Error: {error}", file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
