# CRIS GameRoom — Client nativ

Aplicația este un client Windows WPF .NET 8.

Server:
- HTTPS: https://criswebhost.ro
- API: https://criswebhost.ro/api/
- Realtime: https://criswebhost.ro

Autentificare:
- POST auth/login
- GET auth/me
- POST auth/logout
- Authorization: Bearer <accessToken>

Camere:
- GET rooms
- POST rooms
- GET rooms/:code
- POST rooms/:code/join
- POST rooms/:code/leave

Realtime:
- transport WebSocket
- autentificare prin auth.token
- room:join
- room:state
- room:leave
- room:chat:list
- room:chat:send
