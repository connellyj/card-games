FROM mono:6.8.0

WORKDIR /root

RUN apt-get update
RUN apt-get install -y git

RUN git clone https://github.com/connellyj/card-games.git
RUN nuget update -self
RUN nuget restore card-games/CardGameServer

CMD /bin/bash
