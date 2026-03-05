FROM ubuntu:24.04

RUN apt-get update && \
    apt-get install -y --no-install-recommends espeak-ng && \
    rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["espeak-ng"]
