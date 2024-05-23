#!/usr/bin/env bash

set -e

to_build=$1

if [ "$to_build" == hub ]; then
  docker build . -f hub.Dockerfile -t ss14_serverhub
fi

if [ "$to_build" == web ]; then
  docker build . -f web.Dockerfile -t ss14_web
fi

if [ "$to_build" == auth ]; then
  docker build . -f auth.Dockerfile -t ss14_auth
fi
