import http from 'k6/http';
import { check, sleep } from 'k6';
import ws from 'k6/ws';

export const options = {
  stages: [
    { duration: '30s', target: 20 },
    { duration: '1m', target: 20 },
    { duration: '30s', target: 0 },
  ],
};

export default function () {

  const res = http.get('http://localhost:8080/api/auth/discord/config');
  check(res, { 'config status is 200': (r) => r.status === 200 });


  const url = 'ws://localhost:8080/battleHub';
  const params = { tags: { my_tag: 'battlehub' } };

  const socket = ws.connect(url, params, function (socket) {
    socket.on('open', () => {
      console.log('connected');
      socket.send(JSON.stringify({
        protocol: 'json',
        version: '1',
        requestId: '1'
      }));
    });
    socket.on('close', () => console.log('disconnected'));
    socket.on('error', (e) => console.log('error: ' + e.error()));
  });

  sleep(1);
}
