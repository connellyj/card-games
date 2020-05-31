FROM mono:6.8.0

WORKDIR /root

RUN apt-get update
RUN apt-get install -y git

RUN git clone https://github.com/connellyj/card-games.git
RUN cd card-games/CardGameServer/
RUN nuget update -self

CMD /bin/bash
