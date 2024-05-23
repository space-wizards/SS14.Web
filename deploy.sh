#!/usr/bin/env bash

set -e

to_build=$1

if [ "$to_build" == hub ]; then
  docker image save ss14_serverhub | gzip | ssh pebbles podman load &
  docker image save ss14_serverhub | gzip | ssh nsh podman load &
fi

if [ "$to_build" == auth ]; then
  docker image save ss14_auth | gzip | ssh pebbles podman load &
  docker image save ss14_auth | gzip | ssh nsh podman load &
fi

if [ "$to_build" == web ]; then
  docker image save ss14_web | gzip | ssh pebbles podman load &
  docker image save ss14_web | gzip | ssh nsh podman load &
fi

wait $(jobs -p)
