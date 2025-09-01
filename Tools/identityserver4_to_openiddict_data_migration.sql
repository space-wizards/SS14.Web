insert into public."OpenIddictApplications"
select gen_random_uuid() as Id,
       (select "SpaceUserId" from public."UserOAuthClients" uc where c."Id" = uc."ClientId"),
       "LogoUri", null as ApplicationType, c."ClientId",
       (select string_agg(array_to_string(array [
                                              cs."Id"::text,
                                          floor(extract(epoch from cs."Created"))::text,
                                          '_OLD_' || cs."Value",
                                          replace(cs."Description", '*', '')
                                              ], '.'), ',') from "IS4"."ClientSecrets" cs where cs."ClientId" = c."Id") ClientSecret,
       'confidential' as ClientType, gen_random_uuid() as ConcurrencyToken,
       CASE when "RequireConsent" then 'Explicit' else 'Implicit' end as "ConsentType",
       "ClientName" as DisplayName, null as DisplayNames, null as JsonWebKeySet,
       '["ept:authorization","ept:token","ept:introspection","ept:end_session","gt:refresh_token","gt:authorization_code","rst:code","scp:email","scp:profile","scp:roles"]'
           as Permissions,
       null as PostLogoutRedirectUris,
       null as Properties,
       (select json_agg("RedirectUri") from "IS4"."ClientRedirectUris" cr where cr."ClientId" = c."Id")::text "RedirectUris",
    case when "RequirePkce" then '["ft:pkce"]' end as "Requirements",
       json_build_object(
           'space:AllowPlainPkce', "AllowPlainTextPkce"::TEXT,
           'space:SigningAlgorithm', "AllowedIdentityTokenSigningAlgorithms",
           'space:Disabled', ("Enabled" = false)::TEXT
       )::text as "Settings",
    "ClientUri" as WebsiteUrl
from "IS4"."Clients" c;
