#!/usr/bin/env python3


# User data dumping script for dumping data from an SS14 postgres database.
# Intended to service GDPR data requests or what have you.

import argparse
import os
import psycopg2
from uuid import UUID

LATEST_DB_MIGRATION = "20230604204603_AccountAdminAdditions"

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("output", help="Directory to output data dumps into.")
    parser.add_argument("user", help="User name/ID to dump data into.")
    parser.add_argument("--ignore-schema-mismatch", action="store_true")
    parser.add_argument("--connection-string", required=True, help="Database connection string to use. See https://www.postgresql.org/docs/current/libpq-connect.html#LIBPQ-CONNSTRING")

    args = parser.parse_args()

    arg_output: str = args.output

    if not os.path.exists(arg_output):
        print("Creating output directory (doesn't exist yet)")
        os.mkdir(arg_output)

    conn = psycopg2.connect(args.connection_string)
    cur = conn.cursor()

    check_schema_version(cur, args.ignore_schema_mismatch)
    user_id = normalize_user_id(cur, args.user)

    dump_AspNetUsers(        cur, user_id, arg_output)
    dump_AccountLogs(        cur, user_id, arg_output)
    dump_AspNetUserRoles(    cur, user_id, arg_output)
    dump_PastAccountNames(   cur, user_id, arg_output)
    dump_Patrons(            cur, user_id, arg_output)
    dump_UserOAuthClients(   cur, user_id, arg_output)
    dump_IS4_PersistedGrants(cur, user_id, arg_output)

def check_schema_version(cur: "psycopg2.cursor", ignore_mismatch: bool):
    cur.execute('SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "__EFMigrationsHistory" DESC LIMIT 1')
    schema_version = cur.fetchone()
    if schema_version == None:
        print("Unable to read database schema version.")
        exit(1)

    if schema_version[0] != LATEST_DB_MIGRATION: 
        print(f"Unsupport schema version of DB: '{schema_version[0]}'. Supported: {LATEST_DB_MIGRATION}")
        if ignore_mismatch:
            return
        exit(1)


def normalize_user_id(cur: "psycopg2.cursor", name_or_uid: str) -> str:
    try:
        return str(UUID(name_or_uid))
    except ValueError:
        # Must be a name, get UUID from DB.
        pass

    cur.execute("SELECT \"Id\" FROM \"AspNetUsers\" WHERE \"UserName\" = %s", (name_or_uid,))
    row = cur.fetchone()
    if row == None:
        print(f"Unable to find user '{name_or_uid}' in DB.")
        exit(1)

    print(f"Found user ID: {row[0]}")
    return row[0]


def dump_admin(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping admin...")

    # #>> '{}' is to turn it into a string.

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data) - 'admin_rank_id'), '[]') #>> '{}'
FROM (
    SELECT
        *,
        (SELECT to_json(rank) FROM (
            SELECT * FROM admin_rank WHERE admin_rank.admin_rank_id = admin.admin_rank_id
        ) rank)
        as admin_rank,
        (SELECT COALESCE(json_agg(to_jsonb(flagg) - 'admin_id'), '[]') FROM (
            SELECT * FROM admin_flag WHERE admin_id = %s
        ) flagg)
        as admin_flags
    FROM
        admin
    WHERE
        \"Id\" = %s
) as data
""", (user_id, user_id))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "admin.json"), "w", encoding="utf-8") as f:
        f.write(json_data)

def dump_AspNetUsers(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping AspNetUsers...")

    # #>> '{}' is to turn it into a string.

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        "Id", "Email", "UserName", "CreatedTime", "EmailConfirmed", "NormalizedEmail", "TwoFactorEnabled", "NormalizedUserName", "AdminLocked", "AdminNotes"
    FROM
        "AspNetUsers" a
    WHERE
        "Id" = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "AspNetUsers.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_AccountLogs(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping AccountLogs...")

    # #>> '{}' is to turn it into a string.

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        "AccountLogs"
    WHERE
        "SpaceUserId" = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "AccountLogs.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_AspNetUserRoles(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping AspNetUserRoles...")

    # #>> '{}' is to turn it into a string.

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        "AspNetUserRoles"
    WHERE
        "UserId" = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "AspNetUserRoles.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_PastAccountNames(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping PastAccountNames...")

    # #>> '{}' is to turn it into a string.

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        "PastAccountNames"
    WHERE
        "SpaceUserId" = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "PastAccountNames.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_Patrons(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping Patrons...")

    # #>> '{}' is to turn it into a string.

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        "Patrons"
    WHERE
        "SpaceUserId" = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "Patrons.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_UserOAuthClients(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping UserOAuthClients...")

    # #>> '{}' is to turn it into a string.

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        *,
        (SELECT to_jsonb(client) FROM (
            SELECT
                *,
                (SELECT COALESCE(json_agg(to_jsonb(client_scopeq)), '[]') FROM (
                    SELECT * FROM "IS4"."ClientScopes" cs WHERE cs."ClientId" = c."Id"
                ) client_scopeq)
                as "Scopes",
                (SELECT COALESCE(json_agg(to_jsonb(client_redirectq)), '[]') FROM (
                    SELECT * FROM "IS4"."ClientRedirectUris" cru WHERE cru."ClientId" = c."Id"
                ) client_redirectq)
                as "RedirectUris",
                (SELECT COALESCE(json_agg(to_jsonb(client_grantq)), '[]') FROM (
                    SELECT * FROM "IS4"."ClientGrantTypes" cgt WHERE cgt."ClientId" = c."Id"
                ) client_grantq)
                as "GrantTypes"
            FROM
                "IS4"."Clients" c
            WHERE c."Id" = a."ClientId"
        ) client)
        as "Client"
    FROM
        "UserOAuthClients" a
    WHERE
        "SpaceUserId" = %s
) as data
""", (user_id,))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "UserOAuthClients.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


def dump_IS4_PersistedGrants(cur: "psycopg2.cursor", user_id: str, outdir: str):
    print("Dumping IS4.PersistedGrants...")

    # #>> '{}' is to turn it into a string.

    cur.execute("""
SELECT
    COALESCE(json_agg(to_jsonb(data)), '[]') #>> '{}'
FROM (
    SELECT
        *
    FROM
        "IS4"."PersistedGrants"
    WHERE
        "SubjectId" = %s
) as data
""", (str(user_id),))

    json_data = cur.fetchall()[0][0]

    with open(os.path.join(outdir, "IS4.PersistedGrants.json"), "w", encoding="utf-8") as f:
        f.write(json_data)


main()
