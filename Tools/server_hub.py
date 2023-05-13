#!/usr/bin/env python3

#
# Helper script to interface with the Hub API.
#

import argparse
import requests
import json
from typing import TypedDict, Any

HUB_URL = "https://central.spacestation14.io/hub/"

class ServerEntry(TypedDict):
    address: str
    statusData: dict[str, Any]


def main():
    parser = argparse.ArgumentParser()
    subparsers = parser.add_subparsers(title="subcommands", dest="command")
    subparsers.add_parser("list", description="List servers on the hub")

    parser_status = subparsers.add_parser("status")
    parser_status.add_argument("address", help="Address of the server to fetch status for")

    parser_info = subparsers.add_parser("info")
    parser_info.add_argument("address", help="Address of the server to fetch info for")

    args = parser.parse_args()
    if args.command == "list":
        run_list(args)
    elif args.command == "status":
        run_status(args)
    elif args.command == "info":
        run_info(args)
    elif not args.command:
        parser.print_help()


def run_list(args: argparse.Namespace):
    resp = requests.get(f"{HUB_URL}api/servers")
    resp.raise_for_status()

    resp_data: list[ServerEntry] = resp.json()

    fmt = "{:<50} {:<10} {}"

    resp_data.sort(key= lambda x: x["statusData"]["players"], reverse=True)

    print(fmt.format("ADDRESS", "PLAYERS", "NAME"))
    for server in resp_data:
        print(fmt.format(server["address"], server["statusData"]["players"], server["statusData"]["name"]))


def run_status(args: argparse.Namespace):
    resp = requests.get(f"{HUB_URL}api/servers")
    resp.raise_for_status()

    resp_data: list[ServerEntry] = resp.json()

    for server in resp_data:
        if server["address"] == args.address:
            break
    else:
        print(f"Unable to find server: '{args.address}'")
        exit(1)

    print(json.dumps(server["statusData"]))


def run_info(args: argparse.Namespace):
    resp = requests.get(f"{HUB_URL}api/servers/info", params={"url": args.address})
    resp.raise_for_status()

    print(resp.text)


main()
