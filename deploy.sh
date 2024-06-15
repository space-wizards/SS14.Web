#!/usr/bin/env bash

set -e

to_build=$1

if [ "$to_build" == hub ]; then
  docker image save ss14_serverhub | gzip | ssh pebbles "podman load -q && podman image tag docker.io/library/ss14_serverhub localhost/ss14_serverhub" &
  docker image save ss14_serverhub | gzip | ssh nsh "podman load -q && podman image tag docker.io/library/ss14_serverhub localhost/ss14_serverhub" &
fi

if [ "$to_build" == auth ]; then
  docker image save ss14_auth | gzip | ssh pebbles "podman load -q && podman image tag docker.io/library/ss14_auth localhost/ss14_auth" &
  docker image save ss14_auth | gzip | ssh nsh "podman load -q && podman image tag docker.io/library/ss14_auth localhost/ss14_auth" &
fi

if [ "$to_build" == web ]; then
  docker image save ss14_web | gzip | ssh pebbles "podman load -q && podman image tag docker.io/library/ss14_web localhost/ss14_web" &
  docker image save ss14_web | gzip | ssh nsh "podman load -q && podman image tag docker.io/library/ss14_web localhost/ss14_web" &
fi

wait $(jobs -p)
